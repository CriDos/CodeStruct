using CommandLine;

public class Options
{
    [Option('c', "console", Required = false, HelpText = "Output to console instead of clipboard.")]
    public bool OutputToConsole { get; set; }

    [Option("cl", Required = false, HelpText = "Clean up file content.")]
    public bool CleanupContent { get; set; }

    [Option('s', "structure", Required = false, HelpText = "Generate only directory structure.")]
    public bool GenerateStructureOnly { get; set; }

    [Option('d', "directories", Required = false, HelpText = "Generate only directory structure without files.")]
    public bool DirectoriesOnly { get; set; }

    [Option("set-path", Required = false, HelpText = "Set CodeStruct path in system environment variables.")]
    public bool SetPath { get; set; }
}
