using System;
using System.IO;
using System.Reflection;
using HardDev.CoreUtils.Config;

namespace HardDev.CodeStruct.Configs;

public sealed class CodeStructConfig : BaseConfiguration<CodeStructConfig>
{
    public CodeStructConfig() : base(GetConfigPath())
    {
    }

    private static string GetConfigPath()
    {
        string baseDirectory = AppContext.BaseDirectory;
        string executablePath = Environment.ProcessPath;
        string assemblyLocation = Assembly.GetExecutingAssembly().Location;

        string exeDirectory = Path.GetDirectoryName(executablePath) ??
                              Path.GetDirectoryName(assemblyLocation) ??
                              baseDirectory;

        return Path.Combine(exeDirectory, "CodeStructConfig.json");
    }

    public string[] AllowedExtensions { get; set; } =
    [
        "c", "h", "cpp", "hpp", "cs", "csproj", "cshtml", "csx", "csharp", "vb", "java", "kotlin", "py", "php", "js", "ts", "html",
        "css", "go", "ruby", "pl", "r", "groovy", "swift", "asm", "bat", "cmd", "ps1"
    ];

    public string[] IgnoredDirectories { get; set; } =
    [
        "node_modules", ".git", ".svn", ".run", ".idea", "bin", "obj", ".vs", ".vscode", ".metadata", ".recommenders", ".settings",
        ".angular", ".keep", ".venv", ".virtualenv", "_builds", "_notes", "Build", "Debug", "release", "tmp", "temp"
    ];
}
