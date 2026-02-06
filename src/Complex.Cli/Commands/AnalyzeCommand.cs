using Complex.Cli.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.CommandLine;

namespace Complex.Cli.Commands;

public class AnalyzeCommand : Command
{
    private readonly IServiceProvider _serviceProvider;

    public AnalyzeCommand(IServiceProvider serviceProvider) 
        : base("analyze", "Analyze a git repository and generate a complexity report")
    {
        _serviceProvider = serviceProvider;

        var urlOption = new Option<string>(
            aliases: new[] { "--url", "-u" },
            description: "URL of the git repository to analyze")
        {
            IsRequired = true
        };

        var outputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            description: "Output path for the analysis report (default: ./analysis-report.md)",
            getDefaultValue: () => "./analysis-report.md");

        AddOption(urlOption);
        AddOption(outputOption);

        this.SetHandler(async (context) =>
        {
            var url = context.ParseResult.GetValueForOption(urlOption);
            var output = context.ParseResult.GetValueForOption(outputOption);
            var cancellationToken = context.GetCancellationToken();

            context.ExitCode = await ExecuteAsync(url!, output!, cancellationToken);
        });
    }

    private async Task<int> ExecuteAsync(
        string url,
        string output,
        CancellationToken cancellationToken = default)
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<AnalyzeCommand>>();
        var repositoryService = _serviceProvider.GetRequiredService<IRepositoryService>();
        var analyzerService = _serviceProvider.GetRequiredService<ICodeAnalyzerService>();
        var reportService = _serviceProvider.GetRequiredService<IReportGeneratorService>();

        try
        {
            logger.LogInformation("Starting repository analysis for {Url}", url);
            Console.WriteLine($"Analyzing repository: {url}");
            Console.WriteLine();

            // Clone repository
            Console.WriteLine("Cloning repository...");
            var repoPath = await repositoryService.CloneRepositoryAsync(url, cancellationToken);
            Console.WriteLine($"Repository cloned to: {repoPath}");
            Console.WriteLine();

            // Analyze repository
            Console.WriteLine("Analyzing codebase...");
            var analysis = await analyzerService.AnalyzeRepositoryAsync(repoPath, url, cancellationToken);
            Console.WriteLine($"Found {analysis.Projects.Count} project(s)");
            Console.WriteLine();

            // Generate report
            Console.WriteLine("Generating report...");
            var report = await reportService.GenerateMarkdownReportAsync(analysis, cancellationToken);
            await reportService.SaveReportAsync(report, output, cancellationToken);
            Console.WriteLine($"Report saved to: {Path.GetFullPath(output)}");
            Console.WriteLine();

            // Display summary
            Console.WriteLine("=== Analysis Summary ===");
            Console.WriteLine($"Total Projects: {analysis.Projects.Count}");
            Console.WriteLine($"Overall Complexity Score: {analysis.TotalComplexityScore}");
            Console.WriteLine($"Recommended Team Size: {analysis.TotalEstimatedTeamSize} developer(s)");
            Console.WriteLine($"Technologies: {string.Join(", ", analysis.AllTechnologies.Take(5))}{(analysis.AllTechnologies.Count > 5 ? "..." : "")}");
            Console.WriteLine();

            // Cleanup
            repositoryService.CleanupRepository(repoPath);

            logger.LogInformation("Repository analysis completed successfully");
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to analyze repository");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
