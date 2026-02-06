using Complex.Abstractions;

namespace Complex.Plugin.Cpp;

public class CppAnalyzerPlugin : ILanguageAnalyzerPlugin
{
    public string Name => "C++";

    public IReadOnlyList<LanguageProjectDescriptor> DiscoverProjects(string repositoryPath)
    {
        var cppProjects = Directory.GetFiles(repositoryPath, "CMakeLists.txt", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(repositoryPath, "Makefile", SearchOption.AllDirectories));

        return cppProjects
            .Select(Path.GetDirectoryName)
            .Where(p => p != null)
            .Cast<string>()
            .Distinct()
            .Select(p => new LanguageProjectDescriptor
            {
                ProjectType = "C++",
                ProjectPath = p
            })
            .ToList();
    }

    public IReadOnlyList<string> GetFileExtensions(string projectType)
    {
        return new[] { "*.cpp", "*.h", "*.hpp", "*.c", "*.cc", "*.cxx" };
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
