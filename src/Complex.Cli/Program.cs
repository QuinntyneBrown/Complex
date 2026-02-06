using Complex.Cli.Commands;
using Complex.Cli.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.CommandLine;

// Build service provider
var services = new ServiceCollection();

// Add logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// Add services
services.AddSingleton<IRepositoryService, RepositoryService>();
services.AddSingleton<ICodeAnalyzerService, CodeAnalyzerService>();
services.AddSingleton<IReportGeneratorService, ReportGeneratorService>();

var serviceProvider = services.BuildServiceProvider();

// Build root command
var rootCommand = new RootCommand("Complex - A code repository complexity analyzer");

// Add analyze command
var analyzeCommand = new AnalyzeCommand(serviceProvider);
rootCommand.AddCommand(analyzeCommand);

// Execute
return await rootCommand.InvokeAsync(args);
