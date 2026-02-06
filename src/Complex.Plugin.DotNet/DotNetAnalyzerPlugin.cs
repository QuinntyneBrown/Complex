using Complex.Abstractions;

namespace Complex.Plugin.DotNet;

public class DotNetAnalyzerPlugin : ILanguageAnalyzerPlugin
{
    public string Name => "DotNet";

    public IReadOnlyList<LanguageProjectDescriptor> DiscoverProjects(string repositoryPath)
    {
        var csprojFiles = Directory.GetFiles(repositoryPath, "*.csproj", SearchOption.AllDirectories);
        return csprojFiles
            .Select(Path.GetDirectoryName)
            .Where(p => p != null)
            .Cast<string>()
            .Distinct()
            .Select(p => new LanguageProjectDescriptor
            {
                ProjectType = "C# / .NET",
                ProjectPath = p
            })
            .ToList();
    }

    public IReadOnlyList<string> GetFileExtensions(string projectType)
    {
        return new[] { "*.cs", "*.csproj", "*.sln", "*.json", "*.xml" };
    }

    public IReadOnlyList<string> GetAdditionalExcludedDirectories()
    {
        return Array.Empty<string>();
    }

    public IReadOnlyList<string> DetectTechnologies(string projectPath, string projectType)
    {
        var technologies = new List<string>();

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

        return technologies.Distinct().ToList();
    }
}
