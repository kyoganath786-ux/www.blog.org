using System.Diagnostics;
using System.Text;

namespace NickAI.Core.Services;

/// <summary>Result of running an external command.</summary>
public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError, TimeSpan Duration, bool TimedOut)
{
    public bool Succeeded => ExitCode == 0 && !TimedOut;

    public string CombinedOutput => string.Join(
        Environment.NewLine,
        new[] { StandardOutput, StandardError }.Where(s => !string.IsNullOrWhiteSpace(s)));
}

/// <summary>Runs external development commands.</summary>
public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        TimeSpan? timeout = null,
        CancellationToken ct = default);
}

/// <summary>
/// Executes a command line, always capturing stdout/stderr and the exit code.
/// Every command has a timeout and can be cancelled.
/// </summary>
public sealed class ProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromMinutes(5);

        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
        };

        using var process = new Process { StartInfo = startInfo };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            lock (stdout) stdout.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            lock (stderr) stderr.AppendLine(e.Data);
        };

        var stopwatch = Stopwatch.StartNew();
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutCts = new CancellationTokenSource(effectiveTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        var timedOut = false;
        try
        {
            await process.WaitForExitAsync(linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            timedOut = !ct.IsCancellationRequested;
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception)
            {
                // Process already exited.
            }

            if (ct.IsCancellationRequested) throw;
        }

        stopwatch.Stop();

        // Give the async readers a moment to flush remaining output.
        try
        {
            process.WaitForExit(1000);
        }
        catch (Exception)
        {
            // Ignore.
        }

        string outText, errText;
        lock (stdout) outText = stdout.ToString();
        lock (stderr) errText = stderr.ToString();

        return new ProcessResult(process.ExitCode, outText, errText, stopwatch.Elapsed, timedOut);
    }
}
