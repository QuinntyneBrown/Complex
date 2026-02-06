using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Complex.Cli.Services;

public interface IRepositoryService
{
    Task<string> CloneRepositoryAsync(string repositoryUrl, CancellationToken cancellationToken = default);
    void CleanupRepository(string path);
}

public class RepositoryService : IRepositoryService
{
    private readonly ILogger<RepositoryService> _logger;

    public RepositoryService(ILogger<RepositoryService> logger)
    {
        _logger = logger;
    }

    public async Task<string> CloneRepositoryAsync(string repositoryUrl, CancellationToken cancellationToken = default)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"complex-analysis-{Guid.NewGuid()}");
        _logger.LogInformation("Cloning repository {RepositoryUrl} to {TempPath}", repositoryUrl, tempPath);

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"clone {repositoryUrl} {tempPath}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"git clone failed with exit code {process.ExitCode}: {stderr}");
            }

            _logger.LogInformation("Repository cloned successfully to {TempPath}", tempPath);
            return tempPath;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Failed to clone repository {RepositoryUrl}", repositoryUrl);
            throw;
        }
    }

    public void CleanupRepository(string path)
    {
        if (Directory.Exists(path))
        {
            try
            {
                _logger.LogInformation("Cleaning up repository at {Path}", path);
                Directory.Delete(path, true);
                _logger.LogInformation("Repository cleaned up successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup repository at {Path}", path);
            }
        }
    }
}
