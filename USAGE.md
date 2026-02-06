# Complex CLI - Usage Examples

This document provides practical examples of using the Complex CLI tool.

## Basic Usage

### Analyze a Repository

```bash
dotnet run --project src/Complex.Cli/Complex.Cli.csproj -- analyze --url <repository-url>
```

### Analyze with Custom Output Path

```bash
dotnet run --project src/Complex.Cli/Complex.Cli.csproj -- analyze --url <repository-url> --output reports/my-analysis.md
```

## Real-World Examples

### Example 1: Analyzing a .NET Repository

```bash
dotnet run --project src/Complex.Cli/Complex.Cli.csproj -- analyze \
  --url https://github.com/dotnet/aspnetcore \
  --output reports/aspnetcore-analysis.md
```

**Expected Output:**
- Analysis of multiple C# projects
- ASP.NET Core technology detection
- Entity Framework Core detection
- Unit testing framework identification

### Example 2: Analyzing a Python Repository

```bash
dotnet run --project src/Complex.Cli/Complex.Cli.csproj -- analyze \
  --url https://github.com/django/django \
  --output reports/django-analysis.md
```

**Expected Output:**
- Analysis of Python modules
- Detection of setup.py and requirements.txt
- Python package structure analysis

### Example 3: Analyzing an Angular Repository

```bash
dotnet run --project src/Complex.Cli/Complex.Cli.csproj -- analyze \
  --url https://github.com/angular/angular \
  --output reports/angular-analysis.md
```

**Expected Output:**
- TypeScript/JavaScript file analysis
- Angular framework detection
- Testing framework identification (Jasmine/Karma)

### Example 4: Analyzing a Multi-Language Repository

```bash
dotnet run --project src/Complex.Cli/Complex.Cli.csproj -- analyze \
  --url https://github.com/microsoft/vscode \
  --output reports/vscode-analysis.md
```

**Expected Output:**
- Analysis of TypeScript projects
- Detection of C++ native modules
- Multiple technology stack identification

## Understanding the Report

### Complexity Levels

- **Low (< 50)**: Simple projects, 1 developer can handle
- **Medium (50-149)**: Moderate complexity, small team of 2-3 developers
- **High (150-299)**: Complex projects, dedicated team of 3-5 developers
- **Very High (≥ 300)**: Very complex, large team of 5+ developers

### Team Size Estimation Formula

```
Base Team Size = Total Lines / 10,000
+ Complexity adjustments (score > 100, 200, 500)
+ Technology diversity adjustments (> 5 technologies)
```

### Complexity Score Calculation

```
Complexity Score = (Lines of Code / 100)
                 + (File Count / 10)
                 + (Technology Count × 5)
                 + (File Type Count × 2)
```

## Tips for Best Results

1. **Large Repositories**: Be patient, cloning and analyzing large repositories may take several minutes
2. **Private Repositories**: Ensure you have SSH keys or Git credentials configured
3. **Output Path**: Use absolute paths or ensure the directory exists
4. **Clean Analysis**: The tool automatically cleans up cloned repositories after analysis

## Sample Report Structure

A typical report includes:

```markdown
# Code Repository Analysis Report

## Executive Summary
- Total projects analyzed
- Overall complexity score
- Recommended team size
- Technologies used

## Project Analysis
For each project:
- Project type and path
- Metrics (files, lines, complexity, team size)
- Technologies used
- File type distribution
- Complexity assessment

## Recommendations
- Team organization advice
- Technology standardization suggestions
- Code quality recommendations
```

## Command-Line Options

### Required Options

- `--url`, `-u`: URL of the git repository to analyze

### Optional Options

- `--output`, `-o`: Output path for the report (default: `./analysis-report.md`)
- `--help`, `-h`: Show help information

## Troubleshooting

### Issue: "Repository not found"
**Solution**: Verify the repository URL is correct and accessible

### Issue: "Permission denied"
**Solution**: Ensure you have access to the repository and Git credentials are configured

### Issue: "Out of memory"
**Solution**: For very large repositories, increase available memory or analyze specific branches

### Issue: "No projects found"
**Solution**: The repository may not contain recognized project files (.csproj, package.json, setup.py, etc.)

## Build and Run from Release

```bash
# Build in Release mode
dotnet build --configuration Release

# Run the built executable
./src/Complex.Cli/bin/Release/net10.0/Complex.Cli analyze --url <repository-url>
```

## Creating a Standalone Executable

```bash
# Publish as a self-contained executable
dotnet publish src/Complex.Cli/Complex.Cli.csproj -c Release -r linux-x64 --self-contained

# The executable will be in:
# src/Complex.Cli/bin/Release/net10.0/linux-x64/publish/Complex.Cli
```

Replace `linux-x64` with your target runtime:
- `win-x64` for Windows
- `osx-x64` for macOS
- `linux-arm64` for Linux ARM64
