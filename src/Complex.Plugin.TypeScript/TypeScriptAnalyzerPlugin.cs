using Complex.Abstractions;

namespace Complex.Plugin.TypeScript;

public class TypeScriptAnalyzerPlugin : ILanguageAnalyzerPlugin
{
    public string Name => "TypeScript";

    public IReadOnlyList<LanguageProjectDescriptor> DiscoverProjects(string repositoryPath)
    {
        var projects = new List<LanguageProjectDescriptor>();

        var angularFiles = Directory.GetFiles(repositoryPath, "angular.json", SearchOption.AllDirectories);
        foreach (var file in angularFiles)
        {
            var dir = Path.GetDirectoryName(file);
            if (dir != null)
            {
                projects.Add(new LanguageProjectDescriptor
                {
                    ProjectType = "Angular / TypeScript",
                    ProjectPath = dir
                });
            }
        }

        var packageFiles = Directory.GetFiles(repositoryPath, "package.json", SearchOption.AllDirectories);
        foreach (var file in packageFiles)
        {
            var dir = Path.GetDirectoryName(file);
            if (dir != null && !projects.Any(p => p.ProjectPath == dir))
            {
                projects.Add(new LanguageProjectDescriptor
                {
                    ProjectType = "TypeScript / JavaScript",
                    ProjectPath = dir
                });
            }
        }

        return projects;
    }

    public IReadOnlyList<string> GetFileExtensions(string projectType)
    {
        return projectType switch
        {
            "Angular / TypeScript" => new[] { "*.ts", "*.js", "*.html", "*.css", "*.scss", "*.json" },
            "TypeScript / JavaScript" => new[] { "*.ts", "*.js", "*.jsx", "*.tsx", "*.json" },
            _ => new[] { "*.ts", "*.js", "*.json" }
        };
    }

    public IReadOnlyList<string> GetAdditionalExcludedDirectories()
    {
        return Array.Empty<string>();
    }

    public IReadOnlyList<string> DetectTechnologies(string projectPath, string projectType)
    {
        var technologies = new List<string>();

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

        return technologies.Distinct().ToList();
    }
}
