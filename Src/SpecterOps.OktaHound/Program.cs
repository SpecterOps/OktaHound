using System.CommandLine;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Okta.Sdk.Client;
using SpecterOps.OktaHound.Database;
using SpecterOps.OktaHound.Model.Okta;
using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound;

class Program
{
    private const string LoggerCategoryName = "OktaHound";
    private const int ExitSuccess = 0;
    private const int ExitExportOutputMissing = 20;
    private const int ExitExportDatabaseMissing = 21;
    private const int ExitInvalidDomainWithoutToken = 22;
    private const int ExitInvalidTokenWithoutDomain = 23;
    private const int ExitFetchGraphFailed = 24;
    private const int ExitIoError = 30;
    private const int ExitAggregateError = 31;
    private const int ExitCanceled = 32;
    private const int ExitTimeout = 33;
    private const int ExitDbUpdateError = 34;
    private const int ExitUnhandledError = 35;

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

        Option<CollectionTarget> collectionTargetOption = new("--targets", "--entities", "-e")
        {
            Description = "Entities to collect.",
            Required = false,
            DefaultValueFactory = _ => CollectionTarget.All,
            Arity = ArgumentArity.ExactlyOne
        };

        Option<bool> skipMfaOption = new("--skip-mfa")
        {
            Description = "Skip collecting user authentication factors (MFA).",
            Required = false
        };

        Command legacyCommand = new("legacy", "Collect and export data from an Okta organization")
        {
            outputDirectoryOption,
            oktaDomainOption,
            oktaApiTokenOption,
            skipMfaOption
        };

        Command runCommand = new("run", "Run end-to-end collection, post-processing, and export")
        {
            outputDirectoryOption,
            collectionTargetOption,
            oktaDomainOption,
            oktaApiTokenOption
        };

        Command collectCommand = new("collect", "Collect data from an Okta organization into the database")
        {
            outputDirectoryOption,
            collectionTargetOption,
            oktaDomainOption,
            oktaApiTokenOption,
        };

        Command processCommand = new("process", "Run post-processing tasks on collected entities")
        {
            outputDirectoryOption
        };

        Command exportCommand = new("export", "Export data from the local database to JSON")
        {
            outputDirectoryOption
        };

        // No need to dispose the cancellation token source, as it is bound to the application lifetime
        CancellationTokenSource tokenSource = new();

        legacyCommand.SetAction(parseResult =>
        {
            // Fetch the command line options
            DirectoryInfo outputDirectory = parseResult.GetRequiredValue(outputDirectoryOption);
            LogLevel verbosity = parseResult.GetRequiredValue(verboseOption);
            string? oktaDomain = parseResult.GetValue(oktaDomainOption);
            string? oktaApiToken = parseResult.GetValue(oktaApiTokenOption);
            bool skipMfa = parseResult.GetValue(skipMfaOption);

            // Initialize the console logger and CTRL-C handler
            ILogger logger = InitializeConsole(verbosity, tokenSource);

            // Launch the main logic
            return FetchAndSaveOktaGraph(outputDirectory, logger, oktaDomain, oktaApiToken, skipMfa, tokenSource.Token);
        });

        runCommand.SetAction(parseResult =>
        {
            // Fetch the command line options
            DirectoryInfo outputDirectory = parseResult.GetRequiredValue(outputDirectoryOption);
            CollectionTarget collectionTarget = parseResult.GetRequiredValue(collectionTargetOption);
            LogLevel verbosity = parseResult.GetRequiredValue(verboseOption);
            string? oktaDomain = parseResult.GetValue(oktaDomainOption);
            string? oktaApiToken = parseResult.GetValue(oktaApiTokenOption);

            // Initialize the console logger and CTRL-C handler
            ILogger logger = InitializeConsole(verbosity, tokenSource);

            // Launch end-to-end logic
            return RunAndCatchExceptions(
                cancellationToken => CollectAndExport(collectionTarget, outputDirectory, logger, oktaDomain, oktaApiToken, cancellationToken),
                logger,
                tokenSource.Token);
        });

        collectCommand.SetAction(parseResult =>
        {
            // Fetch the command line options
            DirectoryInfo outputDirectory = parseResult.GetRequiredValue(outputDirectoryOption);
            CollectionTarget collectionTarget = parseResult.GetRequiredValue(collectionTargetOption);
            LogLevel verbosity = parseResult.GetRequiredValue(verboseOption);
            string? oktaDomain = parseResult.GetValue(oktaDomainOption);
            string? oktaApiToken = parseResult.GetValue(oktaApiTokenOption);

            // Initialize the console logger and CTRL-C handler
            ILogger logger = InitializeConsole(verbosity, tokenSource);

            // Launch the main logic
            return RunAndCatchExceptions(
                cancellationToken => Collect(collectionTarget, outputDirectory, logger, oktaDomain, oktaApiToken, cancellationToken),
                logger,
                tokenSource.Token);
        });

        processCommand.SetAction(parseResult =>
        {
            // Fetch the command line options
            DirectoryInfo outputDirectory = parseResult.GetRequiredValue(outputDirectoryOption);
            LogLevel verbosity = parseResult.GetRequiredValue(verboseOption);

            // Initialize the console logger and CTRL-C handler
            ILogger logger = InitializeConsole(verbosity, tokenSource);

            // Launch post-processing logic
            return RunAndCatchExceptions(
            cancellationToken => PostProcessing(outputDirectory, logger, cancellationToken),
            logger,
            tokenSource.Token);
        });

        exportCommand.SetAction(parseResult =>
        {
            // Fetch the command line options
            DirectoryInfo outputDirectory = parseResult.GetRequiredValue(outputDirectoryOption);
            LogLevel verbosity = parseResult.GetRequiredValue(verboseOption);

            // Initialize the console logger and CTRL-C handler
            ILogger logger = InitializeConsole(verbosity, tokenSource);

            // Launch the export logic
            return RunAndCatchExceptions(
                cancellationToken => Export(outputDirectory, logger, cancellationToken),
                logger,
                tokenSource.Token);
        });

        RootCommand rootCommand = new("SpecterOps OktaHound - Okta Data Collector for BloodHound OpenGraph")
        {
            legacyCommand,
            runCommand,
            collectCommand,
            processCommand,
            exportCommand,
            verboseOption
        };

        ParseResult parseResult = rootCommand.Parse(args);
        return parseResult.Invoke();
    }

    private static async Task<int> CollectAndExport(
        CollectionTarget collectionTarget,
        DirectoryInfo outputDirectory,
        ILogger logger,
        string? domain = null,
        string? apiToken = null,
        CancellationToken cancellationToken = default)
    {
        int collectResult = await Collect(collectionTarget, outputDirectory, logger, domain, apiToken, cancellationToken).ConfigureAwait(false);

        if (collectResult != ExitSuccess)
        {
            return collectResult;
        }

        int processResult = await PostProcessing(outputDirectory, logger, cancellationToken).ConfigureAwait(false);

        if (processResult != ExitSuccess)
        {
            return processResult;
        }

        return await Export(outputDirectory, logger, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<int> Collect(
        CollectionTarget collectionTarget,
        DirectoryInfo outputDirectory,
        ILogger logger,
        string? domain = null,
        string? apiToken = null,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = new AppDbContext(outputDirectory.FullName);

        // Check if we are collecting all entities or only a subset.
        // If only a subset, delete the relevant tables to ensure a clean slate for the new data, but keep the rest of the database intact.
        bool preexistingDatabase = File.Exists(dbContext.DatabasePath);
        bool collectAll = collectionTarget == CollectionTarget.All;
        bool deleteDatabase = collectAll && preexistingDatabase;
        bool clearIndividualTables = !collectAll && preexistingDatabase;

        if (deleteDatabase)
        {
            logger.LogInformation("A pre-existing database was found at {DatabasePath} and will be deleted to ensure a clean slate for the new data.", dbContext.DatabasePath);
            await dbContext.Database.EnsureDeletedAsync(cancellationToken).ConfigureAwait(false);
        }
        else if (!preexistingDatabase)
        {
            logger.LogInformation("Creating database at {DatabasePath}...", dbContext.DatabasePath);
        }
        else if (clearIndividualTables)
        {
            logger.LogInformation("A pre-existing database was found at {DatabasePath}. Only the relevant entities will be deleted to ensure a clean slate for the new data.", dbContext.DatabasePath);
        }

        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false);

        if (pendingMigrations.Any())
        {
            logger.LogInformation("Applying {MigrationCount} pending database migrations before data collection...", pendingMigrations.Count());
        }

        await dbContext.Database.MigrateAsync(cancellationToken);

        // Load Okta configuration and create the Okta client
        Configuration? oktaConfigFromCommandLine = null;

        if (!string.IsNullOrEmpty(apiToken))
        {
            oktaConfigFromCommandLine = new(domain, apiToken);
        }

        OktaClient oktaClient = new(outputDirectory.FullName, logger, oktaConfigFromCommandLine);

        await oktaClient.Collect(collectionTarget, clearIndividualTables, cancellationToken).ConfigureAwait(false);

        return ExitSuccess;
    }

    private static async Task<int> PostProcessing(
        DirectoryInfo outputDirectory,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!outputDirectory.Exists)
        {
            logger.LogCritical("Output directory {OutputDirectory} does not exist. Collection must happen first. Exiting.", outputDirectory.FullName);
            return ExitExportOutputMissing;
        }

        await Task.CompletedTask.ConfigureAwait(false);
        logger.LogInformation("Post-processing completed successfully.");
        return ExitSuccess;
    }

    private static async Task<int> Export(
        DirectoryInfo outputDirectory,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!outputDirectory.Exists)
        {
            logger.LogCritical("Output directory {OutputDirectory} does not exist. Collection must happen first. Exiting.", outputDirectory.FullName);
            return ExitExportOutputMissing;
        }

        string databasePath = Path.Combine(outputDirectory.FullName, AppDbContext.DatabaseFileName);
        if (!File.Exists(databasePath))
        {
            logger.LogCritical("Database file {DatabasePath} does not exist. Collection must happen first. Exiting.", databasePath);
            return ExitExportDatabaseMissing;
        }

        await using (var dbContext = new AppDbContext(outputDirectory.FullName))
        {
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false);

            if (pendingMigrations.Any())
            {
                logger.LogInformation("Applying {MigrationCount} pending database migrations before export...", pendingMigrations.Count());
            }

            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }

        OktaClient oktaClient = new(outputDirectory.FullName, logger);

        await using var stream = new FileStream(Path.Combine(outputDirectory.FullName, "okta-users-test.json"), FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        await using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        await oktaClient.ExportOktaGraph(writer, cancellationToken).ConfigureAwait(false);

        return ExitSuccess;
    }

    private static async Task<int> RunAndCatchExceptions(
        Func<CancellationToken, Task<int>> operation,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await operation(cancellationToken).ConfigureAwait(false);
        }
        catch (IOException e)
        {
            logger.LogCritical(e, "IO error: {Message} Exiting.", e.Message);
            return ExitIoError;
        }
        catch (AggregateException e)
        {
            foreach (var innerEx in e.InnerExceptions)
            {
                logger.LogCritical(innerEx, "Unexpected error: {Message} Exiting.", innerEx.Message);
            }

            return ExitAggregateError;
        }
        catch (TaskCanceledException)
        {
            // CTRL-C pressed or timeout
            logger.LogCritical("Data collection aborted. Exiting.");
            return ExitCanceled;
        }
        catch (TimeoutException e)
        {
            // HTTP connection timeout
            logger.LogCritical("Operation timed out: {Message} Exiting.", e.Message);
            return ExitTimeout;
        }
        catch (DbUpdateException e)
        {
            logger.LogCritical(e, "Database update error: {Message} Exiting.", e.Message);
            return ExitDbUpdateError;
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Unexpected error: {Message} Exiting.", e.Message);
            return ExitUnhandledError;
        }
    }

    private static async Task<int> FetchAndSaveOktaGraph(
        DirectoryInfo outputDirectory,
        ILogger logger,
        string? domain = null,
        string? apiToken = null,
        bool skipMfa = false,
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
                return ExitInvalidDomainWithoutToken;
            }

            if (!string.IsNullOrEmpty(domain) && string.IsNullOrEmpty(apiToken))
            {
                logger.LogCritical("The API token must be provided together with the Okta domain. Exiting.");
                return ExitInvalidTokenWithoutDomain;
            }

            // Load Okta configuration and create the Okta client
            Configuration? oktaConfigFromCommandLine = null;

            if (!string.IsNullOrEmpty(apiToken))
            {
                oktaConfigFromCommandLine = new(domain, apiToken);
            }

            OktaClient oktaClient = new(outputDirectory.FullName, logger, oktaConfigFromCommandLine);

            // Fetch the Okta OpenGraph data
            (OktaGraph? oktaGraph, OpenGraph adGraph, OpenGraph hybridEdges) =
                await oktaClient.FetchOktaGraph(skipMfa, cancellationToken).ConfigureAwait(false);

            if (oktaGraph == null)
            {
                logger.LogCritical("Could not fetch Okta OpenGraph data. Exiting.");
                return ExitFetchGraphFailed;
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

            int adNodeCount = adGraph.NodeCount;
            int adEdgeCount = adGraph.EdgeCount;

            if (adNodeCount > 0)
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
            }

            // Exit successfully
            logger.LogInformation("Export completed successfully.");
            return ExitSuccess;
        }
        catch (IOException e)
        {
            logger.LogCritical(e, "Error exporting OpenGraph data into JSON: {Message} Exiting.", e.Message);
            return ExitIoError;
        }
        catch (AggregateException e)
        {
            foreach (var innerEx in e.InnerExceptions)
            {
                logger.LogCritical(innerEx, "Unexpected error: {Message} Exiting.", innerEx.Message);
            }

            return ExitAggregateError;
        }
        catch (TaskCanceledException)
        {
            // CTRL-C pressed or timeout
            logger.LogCritical("Data collection aborted. Exiting.");
            return ExitCanceled;
        }
        catch (TimeoutException e)
        {
            // HTTP connection timeout
            logger.LogCritical("Operation timed out: {Message} Exiting.", e.Message);
            return ExitTimeout;
        }
        catch (DbUpdateException e)
        {
            logger.LogCritical(e, "Database update error: {Message} Exiting.", e.Message);
            return ExitDbUpdateError;
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Unexpected error: {Message} Exiting.", e.Message);
            return ExitUnhandledError;
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
    /// Initializes the console logger and sets up a handler for CTRL-C to trigger cancellation.
    /// </summary>
    /// <param name="verbosity">The minimum log level for the logger.</param>
    /// <param name="tokenSource">The cancellation token source to trigger on CTRL-C.</param>
    /// <returns>An ILogger instance configured for console output.</returns>
    private static ILogger InitializeConsole(LogLevel verbosity, CancellationTokenSource tokenSource)
    {
        // Log messages to the console
        ILogger logger = CreateConsoleLogger(verbosity);

        // React to Ctrl+C
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            logger.LogDebug("Cancellation requested via CTRL-C.");
            eventArgs.Cancel = true;
            tokenSource.Cancel();
        };

        return logger;
    }
}
