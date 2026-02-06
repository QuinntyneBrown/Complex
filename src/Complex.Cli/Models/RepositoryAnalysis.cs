namespace Complex.Cli.Models;

public class RepositoryAnalysis
{
    public string RepositoryUrl { get; set; } = string.Empty;
    public string RepositoryName { get; set; } = string.Empty;
    public DateTime AnalysisDate { get; set; }
    public List<ProjectAnalysis> Projects { get; set; } = new();
    public int TotalComplexityScore { get; set; }
    public int TotalEstimatedTeamSize { get; set; }
    public Dictionary<string, int> OverallFileTypeDistribution { get; set; } = new();
    public List<string> AllTechnologies { get; set; } = new();
}
