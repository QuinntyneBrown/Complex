# Complex

A code repository complexity analyzer CLI tool that analyzes git repositories and generates comprehensive reports with complexity scores and team size estimates.

## Features

- **Multi-Language Support**: Analyzes C++, Python, .NET/C#, and Angular/TypeScript projects
- **Complexity Scoring**: Provides complexity scores for each project based on lines of code, file count, and technology stack
- **Team Size Estimation**: Estimates the recommended team size needed to support each project
- **Comprehensive Reports**: Generates detailed markdown reports with project-by-project analysis and overall repository insights
- **Modern Architecture**: Built with System.CommandLine and Microsoft Extensions (DI, Logging, Configuration)

## Installation

### Prerequisites

- .NET 10.0 SDK or later

### Build from Source

```bash
git clone https://github.com/QuinntyneBrown/Complex.git
cd Complex
dotnet build
```

## Usage

### Analyze a Repository

```bash
dotnet run --project src/Complex.Cli/Complex.Cli.csproj -- analyze --url <repository-url> [--output <output-path>]
```

#### Options

- `--url`, `-u` (required): URL of the git repository to analyze
- `--output`, `-o` (optional): Output path for the analysis report (default: `./analysis-report.md`)

#### Examples

```bash
# Analyze a repository with default output
dotnet run --project src/Complex.Cli/Complex.Cli.csproj -- analyze --url https://github.com/dotnet/aspnetcore

# Analyze a repository with custom output path
dotnet run --project src/Complex.Cli/Complex.Cli.csproj -- analyze --url https://github.com/angular/angular --output reports/angular-analysis.md
```

### Help

```bash
dotnet run --project src/Complex.Cli/Complex.Cli.csproj -- --help
dotnet run --project src/Complex.Cli/Complex.Cli.csproj -- analyze --help
```

## Report Structure

The generated report includes:

### Executive Summary
- Total projects analyzed
- Overall complexity score
- Recommended total team size
- Technologies used across all projects

### Project Analysis
For each project:
- Project type and path
- Metrics (files, lines of code, complexity score, team size)
- Technologies used
- File type distribution
- Complexity assessment with recommendations

### Overall Recommendations
- Team organization suggestions
- Technology standardization advice
- Code quality and maintenance recommendations

## Architecture

The tool is built with:

- **System.CommandLine**: Modern command-line parsing
- **Microsoft.Extensions.DependencyInjection**: Dependency injection container
- **Microsoft.Extensions.Logging**: Structured logging
- **Microsoft.Extensions.Hosting**: Application hosting and lifetime management
- **Microsoft.Extensions.Configuration**: Configuration management
- **Git CLI**: Git repository operations via command-line

### Project Structure

```
Complex/
├── src/
│   └── Complex.Cli/
│       ├── Commands/
│       │   └── AnalyzeCommand.cs
│       ├── Models/
│       │   ├── ProjectAnalysis.cs
│       │   └── RepositoryAnalysis.cs
│       ├── Services/
│       │   ├── CodeAnalyzerService.cs
│       │   ├── ReportGeneratorService.cs
│       │   └── RepositoryService.cs
│       ├── Program.cs
│       └── appsettings.json
└── Complex.sln
```

## Complexity Scoring

The complexity score is calculated based on:

- **Lines of Code**: 1 point per 100 lines
- **File Count**: 1 point per 10 files
- **Technology Stack**: 5 points per technology
- **File Type Diversity**: 2 points per file type

### Complexity Levels

- **Low** (< 50): Easy to maintain, suitable for a small team or single developer
- **Medium** (50-149): Requires experienced developers, small to medium team
- **High** (150-299): Requires dedicated team with expertise, proper architecture needed
- **Very High** (≥ 300): Requires large specialized team, consider breaking down into smaller components

## Team Size Estimation

Team size is estimated using:

- Base: 1 developer per 10,000 lines of code
- Adjustments for complexity score
- Adjustments for technology diversity

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License.
