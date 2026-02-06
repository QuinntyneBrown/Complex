using Complex.Abstractions;
using Complex.Cli.Commands;
using Complex.Cli.Services;
using Complex.Plugin.Cpp;
using Complex.Plugin.DotNet;
using Complex.Plugin.Python;
using Complex.Plugin.TypeScript;
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

// Add language analyzer plugins
services.AddSingleton<ILanguageAnalyzerPlugin, DotNetAnalyzerPlugin>();
services.AddSingleton<ILanguageAnalyzerPlugin, TypeScriptAnalyzerPlugin>();
services.AddSingleton<ILanguageAnalyzerPlugin, PythonAnalyzerPlugin>();
services.AddSingleton<ILanguageAnalyzerPlugin, CppAnalyzerPlugin>();

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
