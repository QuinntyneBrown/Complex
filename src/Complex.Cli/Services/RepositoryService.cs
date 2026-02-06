using LibGit2Sharp;
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

    public Task<string> CloneRepositoryAsync(string repositoryUrl, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"complex-analysis-{Guid.NewGuid()}");
            _logger.LogInformation("Cloning repository {RepositoryUrl} to {TempPath}", repositoryUrl, tempPath);

            try
            {
                Repository.Clone(repositoryUrl, tempPath);
                _logger.LogInformation("Repository cloned successfully to {TempPath}", tempPath);
                return tempPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clone repository {RepositoryUrl}", repositoryUrl);
                throw;
            }
        }, cancellationToken);
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
