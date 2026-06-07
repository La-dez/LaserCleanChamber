using System.Text.Json;
using TrajectoryComparer.Helpers;

internal static class Program
{
    private static int Main(string[] args)
    {
        string diagnosticsDirectory = args.Length > 0
            ? Path.GetFullPath(args[0])
            : Path.Combine(AppContext.BaseDirectory, "TrajectoryDiagnostics");

        if (!Directory.Exists(diagnosticsDirectory))
        {
            Console.WriteLine($"Diagnostics directory not found: {diagnosticsDirectory}");
            return 1;
        }

        FileInfo[] files = new DirectoryInfo(diagnosticsDirectory)
            .GetFiles("*.json", SearchOption.TopDirectoryOnly)
            .OrderByDescending(f => f.CreationTimeUtc)
            .ThenByDescending(f => f.Name)
            .Take(2)
            .OrderBy(f => f.CreationTimeUtc)
            .ToArray();

        if (files.Length < 2)
        {
            Console.WriteLine($"Need at least 2 diagnostics files in: {diagnosticsDirectory}");
            return 2;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var left = TrajectoryDiagnosticsConsoleHelper.Load(files[0].FullName, options);
        var right = TrajectoryDiagnosticsConsoleHelper.Load(files[1].FullName, options);

        TrajectoryDiagnosticsConsoleHelper.PrintComparisonReport(diagnosticsDirectory, files[0], files[1], left, right);
        Console.ReadKey();
        return 0;
    }
}
