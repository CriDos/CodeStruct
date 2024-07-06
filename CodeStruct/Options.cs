using CommandLine;

namespace HardDev.CodeStruct;

public class Options
{
    [Option('c', "console", Required = false, HelpText = "Output to console instead of clipboard.")]
    public bool OutputToConsole { get; set; }

    [Option("cl", Required = false, HelpText = "Clean up file content.")]
    public bool CleanupContent { get; set; }

    [Option('s', "structure", Required = false, HelpText = "Generate only directory structure.")]
    public bool GenerateStructureOnly { get; set; }
}
