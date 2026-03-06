using System.CommandLine;
using System.IO.Compression;
using Microsoft.Extensions.Logging;
using Okta.Sdk.Client;
using SpecterOps.OktaHound.Model.Okta;
using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound;

class Program
{
    private const string LoggerCategoryName = "OktaHound";

    static int Main(string[] args)
    {
        // Command line parsing
        Option<DirectoryInfo> outputDirectoryOption = new("--output", "-o")
        {
            Description = "Path to the OpenGraph output directory",
            HelpName = "DIRPATH",
            Required = false,
            DefaultValueFactory = _ => new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "output")),
            Arity = ArgumentArity.ExactlyOne
        };

        Option<LogLevel> verboseOption = new("--verbosity", "-v")
        {
            Description = "Set output verbosity",
            Required = false,
            DefaultValueFactory = _ => LogLevel.Information,
            Arity = ArgumentArity.ExactlyOne,
            Recursive = true
        };

        Option<string> oktaDomainOption = new("--domain", "-d")
        {
            Description = "Okta domain URL (e.g. https://contoso.okta.com). Overrides okta.yaml setting if provided.",
            Required = false,
            Arity = ArgumentArity.ExactlyOne
        };

        Option<string> oktaApiTokenOption = new("--token", "-t")
        {
            Description = "SSWS API token. Overrides okta.yaml setting if provided.",
            Required = false,
            Arity = ArgumentArity.ExactlyOne
        };

        Option<bool> skipMfaOption = new("--skip-mfa")
        {
            Description = "Skip collecting user authentication factors (MFA).",
            Required = false
        };

        Option<bool> zipOutputOption = new("--zip", "-z")
        {
            Description = "Compress each exported JSON output file after it is written.",
            Required = false
        };

        Option<bool> exportAdNodesOption = new("--export-ad-nodes", "-a")
        {
            Description = "Export the Active Directory subgraph output file.",
            Required = false
        };

        Command collectCommand = new("collect", "Collect and export data from an Okta organization")
        {
            outputDirectoryOption,
            oktaDomainOption,
            oktaApiTokenOption,
            skipMfaOption,
            zipOutputOption,
            exportAdNodesOption
        };

        // No need to dispose the cancellation token source, as it is bound to the application lifetime
        CancellationTokenSource tokenSource = new();

        collectCommand.SetAction(parseResult =>
        {
            // Fetch the command line options
            DirectoryInfo outputDirectory = parseResult.GetRequiredValue(outputDirectoryOption);
            LogLevel verbosity = parseResult.GetRequiredValue(verboseOption);
            string? oktaDomain = parseResult.GetValue(oktaDomainOption);
            string? oktaApiToken = parseResult.GetValue(oktaApiTokenOption);
            bool skipMfa = parseResult.GetValue(skipMfaOption);
            bool zipOutput = parseResult.GetValue(zipOutputOption);
            bool exportAdNodes = parseResult.GetValue(exportAdNodesOption);

            // Log messages to the console
            ILogger logger = CreateConsoleLogger(verbosity);

            // React to Ctrl+C
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                logger.LogDebug("Cancellation requested via CTRL-C.");
                eventArgs.Cancel = true;
                tokenSource.Cancel();
            };

            // Launch the main logic
            return FetchAndSaveOktaGraph(
                outputDirectory,
                logger,
                oktaDomain,
                oktaApiToken,
                skipMfa,
                zipOutput,
                exportAdNodes,
                tokenSource.Token);
        });

        RootCommand rootCommand = new("SpecterOps OktaHound - Okta Data Collector for BloodHound OpenGraph")
        {
            collectCommand,
            verboseOption
        };

        ParseResult parseResult = rootCommand.Parse(args);
        return parseResult.Invoke();
    }

    private static async Task<int> FetchAndSaveOktaGraph(
        DirectoryInfo outputDirectory,
        ILogger logger,
        string? domain = null,
        string? apiToken = null,
        bool skipMfa = false,
        bool zipOutput = false,
        bool exportAdNodes = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Ensure the output directory exists
            if (!outputDirectory.Exists)
            {
                logger.LogInformation("Creating output directory at {OutputDirectory}...", outputDirectory.FullName);
                outputDirectory.Create();
            }
            else
            {
                logger.LogDebug("Using existing output directory at {OutputDirectory}.", outputDirectory.FullName);
            }

            // Validate the Okta authentication parameters
            if (string.IsNullOrEmpty(domain) && !string.IsNullOrEmpty(apiToken))
            {
                logger.LogCritical("The Okta domain must be provided together with the API token. Exiting.");
                return 7;
            }

            if (!string.IsNullOrEmpty(domain) && string.IsNullOrEmpty(apiToken))
            {
                logger.LogCritical("The API token must be provided together with the Okta domain. Exiting.");
                return 7;
            }

            // Load Okta configuration and create the Okta client
            Configuration? oktaConfigFromCommandLine = null;

            if (!string.IsNullOrEmpty(apiToken))
            {
                oktaConfigFromCommandLine = new(domain, apiToken);
            }

            OktaClient oktaClient = new(logger, oktaConfigFromCommandLine);

            // Fetch the Okta OpenGraph data
            (OktaGraph? oktaGraph, OpenGraph adGraph, OpenGraph hybridEdges) =
                await oktaClient.FetchOktaGraph(skipMfa, cancellationToken).ConfigureAwait(false);

            if (oktaGraph == null)
            {
                logger.LogCritical("Could not fetch Okta OpenGraph data. Exiting.");
                return 1;
            }

            logger.LogInformation(
                "The Okta graph contains {NodeCount} nodes and {EdgeCount} edges.",
                oktaGraph.NodeCount,
                oktaGraph.EdgeCount
                );

            // Export the Okta graph to JSON
            const string oktaGraphFileName = "okta-graph.json";
            string oktaGraphFilePath = Path.Combine(outputDirectory.FullName, oktaGraphFileName);
            logger.LogInformation("Writing the Okta OpenGraph to {OutputFileName}...", oktaGraphFileName);
            oktaGraph.SaveAsJson(oktaGraphFilePath);

            if (zipOutput)
            {
                ZipJsonOutput(oktaGraphFilePath, logger);
            }

            int adNodeCount = adGraph.NodeCount;
            int adEdgeCount = adGraph.EdgeCount;

            if (adNodeCount > 0 && !exportAdNodes)
            {
                logger.LogDebug("Skipping Active Directory subgraph export because --export-ad-nodes was not provided.");
            }

            if (adNodeCount > 0 && exportAdNodes)
            {
                // Only save the AD subgraph if it is not empty
                logger.LogInformation(
                    "The Active Directory subgraph contains {NodeCount} nodes and {EdgeCount} edges.",
                    adNodeCount,
                    adEdgeCount
                    );

                const string adNodesFileName = "okta-graph-ad.json";
                string adNodesFilePath = Path.Combine(outputDirectory.FullName, adNodesFileName);
                logger.LogInformation("Writing the Active Directory OpenGraph to {OutputFileName}...", adNodesFileName);
                adGraph.SaveAsJson(adNodesFilePath);

                if (zipOutput)
                {
                    ZipJsonOutput(adNodesFilePath, logger);
                }
            }

            int hybridEdgeCount = hybridEdges.EdgeCount;

            if (hybridEdgeCount > 0)
            {
                // Only save the hybrid edge subgraph if it is not empty
                logger.LogInformation(
                    "The hybrid subgraph contains {EdgeCount} edges.",
                    hybridEdgeCount
                    );

                const string hybridEdgesFileName = "okta-graph-hybrid.json";
                string hybridEdgesFilePath = Path.Combine(outputDirectory.FullName, hybridEdgesFileName);
                logger.LogInformation("Writing the hybrid edge OpenGraph to {OutputFileName}...", hybridEdgesFileName);
                hybridEdges.SaveAsJson(hybridEdgesFilePath);

                if (zipOutput)
                {
                    ZipJsonOutput(hybridEdgesFilePath, logger);
                }
            }

            // Exit successfully
            logger.LogInformation("Export completed successfully.");
            return 0;
        }
        catch (IOException e)
        {
            logger.LogCritical(e, "Error exporting OpenGraph data into JSON: {Message} Exiting.", e.Message);
            return 2;
        }
        catch (AggregateException e)
        {
            foreach (var innerEx in e.InnerExceptions)
            {
                logger.LogCritical(innerEx, "Unexpected error: {Message} Exiting.", innerEx.Message);
            }

            return 3;
        }
        catch (TaskCanceledException)
        {
            // CTRL-C pressed or timeout
            logger.LogCritical("Data collection aborted. Exiting.");
            return 4;
        }
        catch (TimeoutException e)
        {
            // HTTP connection timeout
            logger.LogCritical("Operation timed out: {Message} Exiting.", e.Message);
            return 5;
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Unexpected error: {Message} Exiting.", e.Message);
            return 6;
        }
    }

    /// <summary>
    /// Creates a console logger with the specified verbosity level.
    /// </summary>
    /// <param name="verbosity">The minimum log level for the logger.</param>
    /// <returns>An ILogger instance configured for console output.</returns>
    private static ILogger CreateConsoleLogger(LogLevel verbosity)
    {
        ILoggerFactory loggerFactory = LoggerFactory.Create(
            builder =>
            {
                builder.SetMinimumLevel(verbosity).AddSimpleConsole(
                    options =>
                    {
                        options.SingleLine = true;
                        options.IncludeScopes = false;
                        options.TimestampFormat = "[HH:mm:ss] ";
                    });
            });

        return loggerFactory.CreateLogger(LoggerCategoryName);
    }

    /// <summary>
    /// Creates a zip archive that contains a single JSON export file.
    /// </summary>
    /// <param name="jsonFilePath">Path to the JSON file to archive.</param>
    /// <param name="logger">Logger for status messages.</param>
    private static void ZipJsonOutput(string jsonFilePath, ILogger logger)
    {
        string zipFilePath = Path.ChangeExtension(jsonFilePath, ".zip");

        if (File.Exists(zipFilePath))
        {
            File.Delete(zipFilePath);
        }

        using ZipArchive archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create);
        archive.CreateEntryFromFile(jsonFilePath, Path.GetFileName(jsonFilePath), CompressionLevel.Optimal);

        logger.LogInformation("Wrote zip archive {ZipFileName}.", Path.GetFileName(zipFilePath));
    }
}
