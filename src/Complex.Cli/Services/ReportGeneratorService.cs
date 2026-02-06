using Complex.Cli.Models;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Complex.Cli.Services;

public interface IReportGeneratorService
{
    Task<string> GenerateMarkdownReportAsync(RepositoryAnalysis analysis, CancellationToken cancellationToken = default);
    Task SaveReportAsync(string report, string outputPath, CancellationToken cancellationToken = default);
}

public class ReportGeneratorService : IReportGeneratorService
{
    private readonly ILogger<ReportGeneratorService> _logger;

    public ReportGeneratorService(ILogger<ReportGeneratorService> logger)
    {
        _logger = logger;
    }

    public Task<string> GenerateMarkdownReportAsync(RepositoryAnalysis analysis, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating markdown report for repository {RepositoryName}", analysis.RepositoryName);

        var sb = new StringBuilder();

        // Header
        sb.AppendLine("# Code Repository Analysis Report");
        sb.AppendLine();
        sb.AppendLine($"**Repository:** {analysis.RepositoryUrl}");
        sb.AppendLine($"**Analysis Date:** {analysis.AnalysisDate:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();

        // Executive Summary
        sb.AppendLine("## Executive Summary");
        sb.AppendLine();
        sb.AppendLine($"- **Total Projects Analyzed:** {analysis.Projects.Count}");
        sb.AppendLine($"- **Overall Complexity Score:** {analysis.TotalComplexityScore}");
        sb.AppendLine($"- **Recommended Total Team Size:** {analysis.TotalEstimatedTeamSize} developers");
        sb.AppendLine($"- **Technologies Used:** {string.Join(", ", analysis.AllTechnologies)}");
        sb.AppendLine();

        // Overall File Type Distribution
        if (analysis.OverallFileTypeDistribution.Any())
        {
            sb.AppendLine("### Overall File Type Distribution");
            sb.AppendLine();
            sb.AppendLine("| File Type | Count |");
            sb.AppendLine("|-----------|-------|");
            foreach (var fileType in analysis.OverallFileTypeDistribution.OrderByDescending(f => f.Value))
            {
                sb.AppendLine($"| {fileType.Key} | {fileType.Value} |");
            }
            sb.AppendLine();
        }

        // Project Details
        sb.AppendLine("## Project Analysis");
        sb.AppendLine();

        foreach (var project in analysis.Projects.OrderByDescending(p => p.ComplexityScore))
        {
            sb.AppendLine($"### {project.ProjectName}");
            sb.AppendLine();
            sb.AppendLine($"**Type:** {project.ProjectType}");
            sb.AppendLine($"**Path:** `{project.ProjectPath}`");
            sb.AppendLine();

            // Metrics
            sb.AppendLine("#### Metrics");
            sb.AppendLine();
            sb.AppendLine("| Metric | Value |");
            sb.AppendLine("|--------|-------|");
            sb.AppendLine($"| Total Files | {project.TotalFiles} |");
            sb.AppendLine($"| Total Lines of Code | {project.TotalLines:N0} |");
            sb.AppendLine($"| Complexity Score | {project.ComplexityScore} |");
            sb.AppendLine($"| Estimated Team Size | {project.EstimatedTeamSize} developer(s) |");
            
            if (project.Metrics.ContainsKey("AverageFileSizeInLines"))
            {
                sb.AppendLine($"| Average File Size | {project.Metrics["AverageFileSizeInLines"]} lines |");
            }
            sb.AppendLine();

            // Technologies
            if (project.Technologies.Any())
            {
                sb.AppendLine("#### Technologies");
                sb.AppendLine();
                foreach (var tech in project.Technologies)
                {
                    sb.AppendLine($"- {tech}");
                }
                sb.AppendLine();
            }

            // File Type Distribution
            if (project.FileTypeDistribution.Any())
            {
                sb.AppendLine("#### File Type Distribution");
                sb.AppendLine();
                sb.AppendLine("| File Type | Count |");
                sb.AppendLine("|-----------|-------|");
                foreach (var fileType in project.FileTypeDistribution.OrderByDescending(f => f.Value).Take(10))
                {
                    sb.AppendLine($"| {fileType.Key} | {fileType.Value} |");
                }
                sb.AppendLine();
            }

            // Complexity Assessment
            sb.AppendLine("#### Complexity Assessment");
            sb.AppendLine();
            var complexityLevel = GetComplexityLevel(project.ComplexityScore);
            sb.AppendLine($"**Complexity Level:** {complexityLevel}");
            sb.AppendLine();
            sb.AppendLine(GetComplexityDescription(complexityLevel));
            sb.AppendLine();

            sb.AppendLine("---");
            sb.AppendLine();
        }

        // Recommendations
        sb.AppendLine("## Recommendations");
        sb.AppendLine();
        GenerateRecommendations(sb, analysis);

        _logger.LogInformation("Markdown report generated successfully");

        return Task.FromResult(sb.ToString());
    }

    public async Task SaveReportAsync(string report, string outputPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Saving report to {OutputPath}", outputPath);

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(outputPath, report, cancellationToken);

        _logger.LogInformation("Report saved successfully to {OutputPath}", outputPath);
    }

    private string GetComplexityLevel(int score)
    {
        return score switch
        {
            < 50 => "Low",
            < 150 => "Medium",
            < 300 => "High",
            _ => "Very High"
        };
    }

    private string GetComplexityDescription(string level)
    {
        return level switch
        {
            "Low" => "This project has low complexity. It should be relatively easy to maintain and extend. A small team or single developer can handle it.",
            "Medium" => "This project has medium complexity. It requires experienced developers and careful planning for changes. A small to medium-sized team is recommended.",
            "High" => "This project has high complexity. It requires a dedicated team with expertise in the technologies used. Proper architecture and documentation are essential.",
            "Very High" => "This project has very high complexity. It requires a large team with specialized skills. Consider breaking it down into smaller, more manageable components.",
            _ => "Complexity level unknown."
        };
    }

    private void GenerateRecommendations(StringBuilder sb, RepositoryAnalysis analysis)
    {
        var recommendations = new List<string>();

        // Overall complexity recommendations
        if (analysis.TotalComplexityScore > 500)
        {
            recommendations.Add("**High Overall Complexity**: Consider breaking down large projects into smaller, more manageable microservices or modules.");
        }

        // Team size recommendations
        if (analysis.TotalEstimatedTeamSize > 10)
        {
            recommendations.Add($"**Large Team Required**: With an estimated team size of {analysis.TotalEstimatedTeamSize} developers, ensure proper team organization, clear communication channels, and well-defined responsibilities.");
        }

        // Technology diversity recommendations
        if (analysis.AllTechnologies.Count > 10)
        {
            recommendations.Add($"**Technology Diversity**: The codebase uses {analysis.AllTechnologies.Count} different technologies. Consider standardizing on fewer technologies to reduce maintenance overhead and skill requirements.");
        }

        // Project-specific recommendations
        var largeProjects = analysis.Projects.Where(p => p.TotalLines > 50000).ToList();
        if (largeProjects.Any())
        {
            recommendations.Add($"**Large Projects Detected**: {largeProjects.Count} project(s) have more than 50,000 lines of code. Consider refactoring these projects to improve maintainability.");
        }

        // General recommendations
        recommendations.Add("**Code Quality**: Implement automated testing, code reviews, and continuous integration to maintain code quality.");
        recommendations.Add("**Documentation**: Ensure comprehensive documentation for all projects, especially those with high complexity.");
        recommendations.Add("**Technical Debt**: Regularly review and address technical debt to prevent it from accumulating.");

        foreach (var recommendation in recommendations)
        {
            sb.AppendLine($"- {recommendation}");
            sb.AppendLine();
        }
    }
}
