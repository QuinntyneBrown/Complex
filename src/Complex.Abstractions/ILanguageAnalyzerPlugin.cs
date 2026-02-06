namespace Complex.Abstractions;

public interface ILanguageAnalyzerPlugin
{
    string Name { get; }
    IReadOnlyList<LanguageProjectDescriptor> DiscoverProjects(string repositoryPath);
    IReadOnlyList<string> GetFileExtensions(string projectType);
    IReadOnlyList<string> GetAdditionalExcludedDirectories();
    IReadOnlyList<string> DetectTechnologies(string projectPath, string projectType);
}
