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

    public CodeAnalyzerService(ILogger<CodeAnalyzerService> logger)
    {
        _logger = logger;
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

        // Find all projects in the repository
        var projects = await FindProjectsAsync(repositoryPath, cancellationToken);
        
        foreach (var projectPath in projects)
        {
            var projectAnalysis = await AnalyzeProjectAsync(repositoryPath, projectPath, cancellationToken);
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

    private async Task<List<string>> FindProjectsAsync(string repositoryPath, CancellationToken cancellationToken)
    {
        var projects = new List<string>();

        // Find .NET projects
        var csprojFiles = Directory.GetFiles(repositoryPath, "*.csproj", SearchOption.AllDirectories);
        projects.AddRange(csprojFiles.Select(Path.GetDirectoryName).Where(p => p != null).Cast<string>());

        // Find Python projects
        var pythonProjects = Directory.GetFiles(repositoryPath, "setup.py", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(repositoryPath, "pyproject.toml", SearchOption.AllDirectories))
            .Concat(Directory.GetFiles(repositoryPath, "requirements.txt", SearchOption.AllDirectories));
        projects.AddRange(pythonProjects.Select(Path.GetDirectoryName).Where(p => p != null).Cast<string>().Distinct());

        // Find C++ projects
        var cppProjects = Directory.GetFiles(repositoryPath, "CMakeLists.txt", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(repositoryPath, "Makefile", SearchOption.AllDirectories));
        projects.AddRange(cppProjects.Select(Path.GetDirectoryName).Where(p => p != null).Cast<string>().Distinct());

        // Find Angular/TypeScript projects
        var angularProjects = Directory.GetFiles(repositoryPath, "angular.json", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(repositoryPath, "package.json", SearchOption.AllDirectories));
        projects.AddRange(angularProjects.Select(Path.GetDirectoryName).Where(p => p != null).Cast<string>().Distinct());

        return await Task.FromResult(projects.Distinct().ToList());
    }

    private async Task<ProjectAnalysis?> AnalyzeProjectAsync(string repositoryPath, string projectPath, CancellationToken cancellationToken)
    {
        try
        {
            var projectName = Path.GetFileName(projectPath) ?? "Unknown";
            var projectType = DetermineProjectType(projectPath);

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
            _logger.LogError(ex, "Failed to analyze project at {ProjectPath}", projectPath);
            return null;
        }
    }

    private string DetermineProjectType(string projectPath)
    {
        if (Directory.GetFiles(projectPath, "*.csproj").Any())
            return "C# / .NET";
        if (File.Exists(Path.Combine(projectPath, "setup.py")) || File.Exists(Path.Combine(projectPath, "pyproject.toml")))
            return "Python";
        if (File.Exists(Path.Combine(projectPath, "CMakeLists.txt")) || File.Exists(Path.Combine(projectPath, "Makefile")))
            return "C++";
        if (File.Exists(Path.Combine(projectPath, "angular.json")))
            return "Angular / TypeScript";
        if (File.Exists(Path.Combine(projectPath, "package.json")))
            return "TypeScript / JavaScript";
        
        return "Unknown";
    }

    private List<string> GetProjectFiles(string projectPath, string projectType)
    {
        var files = new List<string>();
        var extensions = projectType switch
        {
            "C# / .NET" => new[] { "*.cs", "*.csproj", "*.sln", "*.json", "*.xml" },
            "Python" => new[] { "*.py", "*.pyx", "*.pyd", "*.txt", "*.toml", "*.cfg" },
            "C++" => new[] { "*.cpp", "*.h", "*.hpp", "*.c", "*.cc", "*.cxx" },
            "Angular / TypeScript" => new[] { "*.ts", "*.js", "*.html", "*.css", "*.scss", "*.json" },
            "TypeScript / JavaScript" => new[] { "*.ts", "*.js", "*.jsx", "*.tsx", "*.json" },
            _ => new[] { "*.*" }
        };

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

        // Exclude common directories
        var excludedDirs = new[] { "node_modules", "bin", "obj", "dist", ".git", "__pycache__", "build" };
        files = files.Where(f => 
        {
            var pathParts = f.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return !pathParts.Any(part => excludedDirs.Contains(part, StringComparer.OrdinalIgnoreCase));
        }).ToList();

        return files;
    }

    private List<string> DetermineTechnologies(string projectPath, string projectType)
    {
        var technologies = new List<string> { projectType };

        // Check for specific technology indicators
        if (File.Exists(Path.Combine(projectPath, "Dockerfile")))
            technologies.Add("Docker");
        
        if (File.Exists(Path.Combine(projectPath, ".github", "workflows")))
            technologies.Add("GitHub Actions");
        
        if (Directory.Exists(Path.Combine(projectPath, ".azure")))
            technologies.Add("Azure");

        if (projectType == "C# / .NET")
        {
            var csprojFiles = Directory.GetFiles(projectPath, "*.csproj", SearchOption.AllDirectories);
            foreach (var csproj in csprojFiles)
            {
                var content = File.ReadAllText(csproj);
                if (content.Contains("Microsoft.AspNetCore"))
                    technologies.Add("ASP.NET Core");
                if (content.Contains("Microsoft.EntityFrameworkCore"))
                    technologies.Add("Entity Framework Core");
                if (content.Contains("xunit") || content.Contains("NUnit") || content.Contains("MSTest"))
                    technologies.Add("Unit Testing");
            }
        }

        if (projectType.Contains("TypeScript") || projectType.Contains("Angular"))
        {
            var packageJsonPath = Path.Combine(projectPath, "package.json");
            if (File.Exists(packageJsonPath))
            {
                var content = File.ReadAllText(packageJsonPath);
                if (content.Contains("@angular/core"))
                    technologies.Add("Angular");
                if (content.Contains("react"))
                    technologies.Add("React");
                if (content.Contains("vue"))
                    technologies.Add("Vue.js");
                if (content.Contains("jest") || content.Contains("jasmine") || content.Contains("karma"))
                    technologies.Add("JavaScript Testing");
            }
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
