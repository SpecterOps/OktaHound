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

        Command collectCommand = new("collect", "Collect and export data from an Okta organization")
        {
            outputDirectoryOption,
            oktaDomainOption,
            oktaApiTokenOption,
            skipMfaOption
        };

        Command testCommand = new("test", "Test command for OktaHound")
        {
            outputDirectoryOption,
            oktaDomainOption,
            oktaApiTokenOption,
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

            // Initialize the console logger and CTRL-C handler
            ILogger logger = InitializeConsole(verbosity, tokenSource);

            // Launch the main logic
            return FetchAndSaveOktaGraph(outputDirectory, logger, oktaDomain, oktaApiToken, skipMfa, tokenSource.Token);
        });

        testCommand.SetAction(parseResult =>
        {
            // Fetch the command line options
            DirectoryInfo outputDirectory = parseResult.GetRequiredValue(outputDirectoryOption);
            LogLevel verbosity = parseResult.GetRequiredValue(verboseOption);
            string? oktaDomain = parseResult.GetValue(oktaDomainOption);
            string? oktaApiToken = parseResult.GetValue(oktaApiTokenOption);

            // Initialize the console logger and CTRL-C handler
            ILogger logger = InitializeConsole(verbosity, tokenSource);

            // Launch the main logic
            return Test(outputDirectory, logger, oktaDomain, oktaApiToken, tokenSource.Token);
        });

        RootCommand rootCommand = new("SpecterOps OktaHound - Okta Data Collector for BloodHound OpenGraph")
        {
            collectCommand,
            testCommand,
            verboseOption
        };

        ParseResult parseResult = rootCommand.Parse(args);
        return parseResult.Invoke();
    }

    private static async Task<int> Test(
        DirectoryInfo outputDirectory,
        ILogger logger,
        string? domain = null,
        string? apiToken = null,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = new AppDbContext(outputDirectory.FullName);
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        // Load Okta configuration and create the Okta client
        Configuration? oktaConfigFromCommandLine = null;

        if (!string.IsNullOrEmpty(apiToken))
        {
            oktaConfigFromCommandLine = new(domain, apiToken);
        }

        OktaClient oktaClient = new(outputDirectory.FullName, logger, oktaConfigFromCommandLine);

        await oktaClient.DeleteOrganizations(cancellationToken);
        await oktaClient.DeleteUserFactors(cancellationToken);
        await oktaClient.DeleteUsers(cancellationToken);
        await oktaClient.DeleteGroups(cancellationToken);
        await oktaClient.DeleteApplications(cancellationToken);
        await oktaClient.DeleteDevices(cancellationToken);
        await oktaClient.DeleteResourceSets(cancellationToken);
        await oktaClient.DeleteRealms(cancellationToken);
        await oktaClient.DeleteBuiltinRoles(cancellationToken);
        await oktaClient.DeleteCustomRoles(cancellationToken);
        await oktaClient.DeleteRoleAssignments(cancellationToken);
        await oktaClient.DeleteApiTokens(cancellationToken);
        await oktaClient.DeleteAgentPools(cancellationToken);
        await oktaClient.DeleteAuthorizationServers(cancellationToken);
        await oktaClient.DeleteIdentityProviders(cancellationToken);
        await oktaClient.DeleteApiServiceIntegrations(cancellationToken);
        await oktaClient.DeletePolicies(cancellationToken);
        await oktaClient.DeleteClientSecrets(cancellationToken);
        await oktaClient.DeleteJWKs(cancellationToken);
        await oktaClient.DeleteApplicationGrants(cancellationToken);
        await oktaClient.DeleteUserGroupMemberships(cancellationToken);

        await oktaClient.CollectOrganization(cancellationToken);
        await oktaClient.CollectUsers(cancellationToken);
        await oktaClient.CollectGroups(cancellationToken);
        await oktaClient.CollectAgentPools(cancellationToken);
        await oktaClient.CollectOktaDevices(cancellationToken);
        await oktaClient.CollectOktaResourceSets(cancellationToken);
        await oktaClient.CollectOktaRealms(cancellationToken);
        await oktaClient.CollectOktaBuiltInRoles(cancellationToken);
        await oktaClient.CollectOktaCustomRoles(cancellationToken);
        await oktaClient.CollectOktaApplications(cancellationToken);
        await oktaClient.CollectOktaApiTokens(cancellationToken);
        await oktaClient.CollectOktaAuthorizationServers(cancellationToken);
        await oktaClient.CollectOktaIdentityProviders(cancellationToken);
        await oktaClient.CollectOktaApiServiceIntegrations(cancellationToken);
        await oktaClient.CollectOktaPolicies(cancellationToken);
        // await oktaClient.CollectOktaPrivilegedUsers(cancellationToken);
        await oktaClient.CollectOktaUserAuthenticationFactors(cancellationToken);
        // await oktaClient.CollectOktaAppUserAssignments(cancellationToken);
        await oktaClient.CollectOktaGroupMemberships(cancellationToken);
        await oktaClient.CollectOktaApplicationGrants(cancellationToken);
        await oktaClient.CollectOktaApplicationSecrets(cancellationToken);
        await oktaClient.CollectOktaApplicationJsonWebKeys(cancellationToken);
        // await oktaClient.CollectOktaAppGroupAssignments(cancellationToken);
        // await oktaClient.CollectOktaAppGroupPushMappings(cancellationToken);
        await oktaClient.CollectOktaApiServiceIntegrationSecrets(cancellationToken);
        // await oktaClient.CollectOktaIdentityProviderUsers(cancellationToken);
        // await oktaClient.CollectOktaCustomRolePermissions(cancellationToken);
        // await oktaClient.CollectOktaPolicyRules(cancellationToken);
        // await oktaClient.CollectOktaPolicyMappings(cancellationToken);
        // await oktaClient.CollectOktaResourceSetMemberships(cancellationToken);

        await using var stream = new FileStream(Path.Combine(outputDirectory.FullName, "okta-users-test.json"), FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        await using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        await oktaClient.ExportOktaGraph(writer, cancellationToken).ConfigureAwait(false);


        return 0;
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

            OktaClient oktaClient = new(null, logger, oktaConfigFromCommandLine);

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
        catch (DbUpdateException e)
        {
            logger.LogCritical(e, "Database update error: {Message} Exiting.", e.Message);
            return 6;
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Unexpected error: {Message} Exiting.", e.Message);
            return 7;
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
