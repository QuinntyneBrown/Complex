namespace Complex.Cli.Models;

public class ProjectAnalysis
{
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectType { get; set; } = string.Empty;
    public string ProjectPath { get; set; } = string.Empty;
    public int TotalFiles { get; set; }
    public int TotalLines { get; set; }
    public int ComplexityScore { get; set; }
    public int EstimatedTeamSize { get; set; }
    public Dictionary<string, int> FileTypeDistribution { get; set; } = new();
    public List<string> Technologies { get; set; } = new();
    public Dictionary<string, object> Metrics { get; set; } = new();
}
