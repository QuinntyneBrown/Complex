using Complex.Abstractions;

namespace Complex.Plugin.Python;

public class PythonAnalyzerPlugin : ILanguageAnalyzerPlugin
{
    public string Name => "Python";

    public IReadOnlyList<LanguageProjectDescriptor> DiscoverProjects(string repositoryPath)
    {
        var pythonProjects = Directory.GetFiles(repositoryPath, "setup.py", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(repositoryPath, "pyproject.toml", SearchOption.AllDirectories))
            .Concat(Directory.GetFiles(repositoryPath, "requirements.txt", SearchOption.AllDirectories));

        return pythonProjects
            .Select(Path.GetDirectoryName)
            .Where(p => p != null)
            .Cast<string>()
            .Distinct()
            .Select(p => new LanguageProjectDescriptor
            {
                ProjectType = "Python",
                ProjectPath = p
            })
            .ToList();
    }

    public IReadOnlyList<string> GetFileExtensions(string projectType)
    {
        return new[] { "*.py", "*.pyx", "*.pyd", "*.txt", "*.toml", "*.cfg" };
    }

    public IReadOnlyList<string> GetAdditionalExcludedDirectories()
    {
        return Array.Empty<string>();
    }

    public IReadOnlyList<string> DetectTechnologies(string projectPath, string projectType)
    {
        return Array.Empty<string>();
    }
}
