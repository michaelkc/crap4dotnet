using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;

namespace Crap4DotNet.Cli.Commands;

internal enum CoverageProvider
{
    Auto,
    Coverlet,
    Microsoft
}

internal static class TestCoverageRunner
{
    public static List<string> Run(string path, CoverageProvider provider)
    {
        var target = Path.GetFullPath(path);
        var workingDirectory = Directory.Exists(target) ? target : Path.GetDirectoryName(target)!;
        try
        {
            return RunCore(target, workingDirectory, provider);
        }
        catch (Exception ex) when (ex is IOException or Win32Exception or JsonException)
        {
            throw new CrapCliException("TEST_FAILURE", $"Unable to run dotnet test: {ex.Message}", path);
        }
    }

    private static List<string> RunCore(string target, string workingDirectory, CoverageProvider provider)
    {
        var mtp = UsesMicrosoftTestingPlatform(workingDirectory);
        if (provider == CoverageProvider.Auto)
            provider = mtp ? CoverageProvider.Microsoft : CoverageProvider.Coverlet;
        if (!mtp && provider == CoverageProvider.Microsoft)
            throw new CrapCliException("INVALID_CONFIGURATION",
                "The microsoft coverage provider requires Microsoft.Testing.Platform in the target's global.json.");

        var resultsDirectory = Path.Combine(workingDirectory, "TestResults", $"crap-{Guid.NewGuid():N}");
        var startInfo = new ProcessStartInfo("dotnet")
        {
            // The SDK resolves global.json from the working directory, not the target argument.
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("test");
        if (!mtp || !Directory.Exists(target))
        {
            if (mtp)
                startInfo.ArgumentList.Add(target.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                    ? "--project" : "--solution");
            startInfo.ArgumentList.Add(target);
        }
        if (!mtp)
        {
            startInfo.ArgumentList.Add("--collect:XPlat Code Coverage");
        }
        else if (provider == CoverageProvider.Coverlet)
        {
            startInfo.ArgumentList.Add("--coverlet");
            startInfo.ArgumentList.Add("--coverlet-output-format");
            startInfo.ArgumentList.Add("cobertura");
        }
        else
        {
            startInfo.ArgumentList.Add("--coverage");
            startInfo.ArgumentList.Add("--coverage-output-format");
            startInfo.ArgumentList.Add("cobertura");
        }
        startInfo.ArgumentList.Add("--results-directory");
        startInfo.ArgumentList.Add(resultsDirectory);

        using var process = Process.Start(startInfo)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        Task.WaitAll(stdout, stderr);
        if (process.ExitCode != 0)
        {
            Console.Error.Write(stdout.Result);
            Console.Error.Write(stderr.Result);
            throw new CrapCliException("TEST_FAILURE",
                $"dotnet test exited with code {process.ExitCode}. Check test output and the installed coverage extension.");
        }

        var files = Directory.Exists(resultsDirectory)
            ? Directory.GetFiles(resultsDirectory, "*cobertura.xml", SearchOption.AllDirectories).ToList()
            : [];
        if (files.Count == 0)
        {
            var package = !mtp ? "coverlet.collector"
                : provider == CoverageProvider.Coverlet ? "coverlet.MTP" : "Microsoft.Testing.Extensions.CodeCoverage";
            throw new CrapCliException("COVERAGE_FILE_NOT_FOUND",
                $"dotnet test completed but no Cobertura report was generated. Ensure {package} is referenced in your test projects.",
                resultsDirectory);
        }
        return files;
    }

    private static bool UsesMicrosoftTestingPlatform(string directory)
    {
        for (var current = new DirectoryInfo(directory); current is not null; current = current.Parent)
        {
            var globalJson = Path.Combine(current.FullName, "global.json");
            if (!File.Exists(globalJson))
                continue;
            using var document = JsonDocument.Parse(File.ReadAllText(globalJson), new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });
            return document.RootElement.TryGetProperty("test", out var test)
                && test.TryGetProperty("runner", out var runner)
                && string.Equals(runner.GetString(), "Microsoft.Testing.Platform", StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }
}
