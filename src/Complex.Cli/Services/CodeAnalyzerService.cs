using Complex.Abstractions;
using Complex.Cli.Models;
using Microsoft.Extensions.Logging;

namespace Complex.Cli.Services;

public interface ICodeAnalyzerService
{
    Task<RepositoryAnalysis> AnalyzeRepositoryAsync(string repositoryPath, string repositoryUrl, CancellationToken cancellationToken = default);
}

public class CodeAnalyzerService : ICodeAnalyzerService
{
    private readonly ILogger<CodeAnalyzerService> _logger;
    private readonly IReadOnlyList<ILanguageAnalyzerPlugin> _plugins;

    public CodeAnalyzerService(ILogger<CodeAnalyzerService> logger, IEnumerable<ILanguageAnalyzerPlugin> plugins)
    {
        _logger = logger;
        _plugins = plugins.ToList();
    }

    public async Task<RepositoryAnalysis> AnalyzeRepositoryAsync(string repositoryPath, string repositoryUrl, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing repository at {RepositoryPath}", repositoryPath);

        var analysis = new RepositoryAnalysis
        {
            RepositoryUrl = repositoryUrl,
            RepositoryName = Path.GetFileName(repositoryPath),
            AnalysisDate = DateTime.UtcNow
        };

        // Discover all projects via plugins
        var projects = DiscoverAllProjects(repositoryPath);

        foreach (var descriptor in projects)
        {
            var projectAnalysis = await AnalyzeProjectAsync(repositoryPath, descriptor, cancellationToken);
            if (projectAnalysis != null)
            {
                analysis.Projects.Add(projectAnalysis);
            }
        }

        // Calculate overall metrics
        analysis.TotalComplexityScore = analysis.Projects.Sum(p => p.ComplexityScore);
        analysis.TotalEstimatedTeamSize = analysis.Projects.Sum(p => p.EstimatedTeamSize);

        // Aggregate file types
        foreach (var project in analysis.Projects)
        {
            foreach (var fileType in project.FileTypeDistribution)
            {
                if (analysis.OverallFileTypeDistribution.ContainsKey(fileType.Key))
                {
                    analysis.OverallFileTypeDistribution[fileType.Key] += fileType.Value;
                }
                else
                {
                    analysis.OverallFileTypeDistribution[fileType.Key] = fileType.Value;
                }
            }
        }

        // Aggregate technologies
        analysis.AllTechnologies = analysis.Projects
            .SelectMany(p => p.Technologies)
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        _logger.LogInformation("Repository analysis completed. Found {ProjectCount} projects", analysis.Projects.Count);

        return analysis;
    }

    private List<LanguageProjectDescriptor> DiscoverAllProjects(string repositoryPath)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<LanguageProjectDescriptor>();

        foreach (var plugin in _plugins)
        {
            foreach (var descriptor in plugin.DiscoverProjects(repositoryPath))
            {
                if (seen.Add(descriptor.ProjectPath))
                {
                    results.Add(descriptor);
                }
            }
        }

        return results;
    }

    private async Task<ProjectAnalysis?> AnalyzeProjectAsync(string repositoryPath, LanguageProjectDescriptor descriptor, CancellationToken cancellationToken)
    {
        try
        {
            var projectPath = descriptor.ProjectPath;
            var projectType = descriptor.ProjectType;
            var projectName = Path.GetFileName(projectPath) ?? "Unknown";

            _logger.LogInformation("Analyzing project {ProjectName} of type {ProjectType}", projectName, projectType);

            var analysis = new ProjectAnalysis
            {
                ProjectName = projectName,
                ProjectType = projectType,
                ProjectPath = Path.GetRelativePath(repositoryPath, projectPath)
            };

            // Analyze files
            var files = GetProjectFiles(projectPath, projectType);
            analysis.TotalFiles = files.Count;

            var fileTypeDistribution = new Dictionary<string, int>();
            int totalLines = 0;

            foreach (var file in files)
            {
                var extension = Path.GetExtension(file).ToLowerInvariant();
                if (!fileTypeDistribution.ContainsKey(extension))
                {
                    fileTypeDistribution[extension] = 0;
                }
                fileTypeDistribution[extension]++;

                try
                {
                    var lines = await File.ReadAllLinesAsync(file, cancellationToken);
                    totalLines += lines.Length;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read file {File}", file);
                }
            }

            analysis.TotalLines = totalLines;
            analysis.FileTypeDistribution = fileTypeDistribution;
            analysis.Technologies = DetermineTechnologies(projectPath, projectType);

            // Calculate complexity score (based on lines of code, file count, and technologies)
            analysis.ComplexityScore = CalculateComplexityScore(analysis);

            // Estimate team size based on complexity
            analysis.EstimatedTeamSize = EstimateTeamSize(analysis);

            // Add metrics
            analysis.Metrics["AverageFileSizeInLines"] = analysis.TotalFiles > 0 ? analysis.TotalLines / analysis.TotalFiles : 0;
            analysis.Metrics["TechnologyCount"] = analysis.Technologies.Count;

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze project at {ProjectPath}", descriptor.ProjectPath);
            return null;
        }
    }

    private List<string> GetProjectFiles(string projectPath, string projectType)
    {
        var files = new List<string>();

        // Get extensions from the plugin that handles this project type
        var extensions = GetExtensionsForProjectType(projectType);

        foreach (var extension in extensions)
        {
            try
            {
                files.AddRange(Directory.GetFiles(projectPath, extension, SearchOption.AllDirectories));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get files with extension {Extension}", extension);
            }
        }

        // Core excluded directories
        var excludedDirs = new List<string> { "node_modules", "bin", "obj", "dist", ".git", "__pycache__", "build" };

        // Add plugin-specific excluded directories
        foreach (var plugin in _plugins)
        {
            excludedDirs.AddRange(plugin.GetAdditionalExcludedDirectories());
        }

        files = files.Where(f =>
        {
            var pathParts = f.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return !pathParts.Any(part => excludedDirs.Contains(part, StringComparer.OrdinalIgnoreCase));
        }).ToList();

        return files;
    }

    private IReadOnlyList<string> GetExtensionsForProjectType(string projectType)
    {
        foreach (var plugin in _plugins)
        {
            var extensions = plugin.GetFileExtensions(projectType);
            if (extensions.Count > 0)
            {
                return extensions;
            }
        }

        return new[] { "*.*" };
    }

    private List<string> DetermineTechnologies(string projectPath, string projectType)
    {
        var technologies = new List<string> { projectType };

        // Cross-cutting technology detection (stays in core)
        if (File.Exists(Path.Combine(projectPath, "Dockerfile")))
            technologies.Add("Docker");

        if (File.Exists(Path.Combine(projectPath, ".github", "workflows")))
            technologies.Add("GitHub Actions");

        if (Directory.Exists(Path.Combine(projectPath, ".azure")))
            technologies.Add("Azure");

        // Delegate language-specific technology detection to plugins
        foreach (var plugin in _plugins)
        {
            var pluginTechnologies = plugin.DetectTechnologies(projectPath, projectType);
            technologies.AddRange(pluginTechnologies);
        }

        return technologies.Distinct().ToList();
    }

    private int CalculateComplexityScore(ProjectAnalysis analysis)
    {
        // Base score on lines of code (1 point per 100 lines)
        int score = analysis.TotalLines / 100;

        // Add points for file count (1 point per 10 files)
        score += analysis.TotalFiles / 10;

        // Add points for technology count (5 points per technology)
        score += analysis.Technologies.Count * 5;

        // Add points for file type diversity (2 points per file type)
        score += analysis.FileTypeDistribution.Count * 2;

        return Math.Max(1, score); // Minimum score of 1
    }

    private int EstimateTeamSize(ProjectAnalysis analysis)
    {
        // Basic formula: 1 developer per 10,000 lines of code
        // Adjusted by complexity factors

        int baseTeamSize = analysis.TotalLines / 10000;
        if (baseTeamSize == 0) baseTeamSize = 1;

        // Adjust for complexity score
        if (analysis.ComplexityScore > 100)
            baseTeamSize += 1;
        if (analysis.ComplexityScore > 200)
            baseTeamSize += 1;
        if (analysis.ComplexityScore > 500)
            baseTeamSize += 2;

        // Adjust for technology count (more technologies = more expertise needed)
        if (analysis.Technologies.Count > 5)
            baseTeamSize += 1;

        return Math.Max(1, baseTeamSize); // Minimum team size of 1
    }
}
