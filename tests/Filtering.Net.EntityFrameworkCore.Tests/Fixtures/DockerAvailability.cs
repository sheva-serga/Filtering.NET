using System.Diagnostics;

namespace Filtering.Net.EntityFrameworkCore.Tests.Fixtures;

/// <summary>
/// Probes the host for a running Docker daemon. Used to skip Testcontainers-backed integration
/// tests when the developer machine (or CI runner) doesn't have Docker available.
/// Result is cached for the AppDomain lifetime so we shell out only once.
/// </summary>
internal static class DockerAvailability
{
    private static readonly Lazy<bool> IsAvailableLazy = new(ProbeDocker);

    /// <summary>True when <c>docker info</c> exits cleanly.</summary>
    public static bool IsAvailable => IsAvailableLazy.Value;

    private static bool ProbeDocker()
    {
        try
        {
            var startInfo = new ProcessStartInfo("docker", "info")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using var dockerProcess = Process.Start(startInfo);
            if (dockerProcess is null) return false;
            if (!dockerProcess.WaitForExit(milliseconds: 5_000))
            {
                try { dockerProcess.Kill(entireProcessTree: true); } catch { /* best effort */ }
                return false;
            }
            return dockerProcess.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
