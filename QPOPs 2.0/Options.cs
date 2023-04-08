using CommandLine;
using CommandLine.Text;

namespace QPOPs2
{
    public class Options
    {
        public const string applicationAlias = "#APPLICATION#";

        [Option('s', "sysroot", Required = true, HelpText = "Path to sys_root folder.")]
        public string SysRoot { get; set; } = string.Empty;

        [Option('i', "input", Required = true, HelpText = "Paths to input XML files.")]
        public IEnumerable<string> Input { get; set; } = Enumerable.Empty<string>();

        [Option('o', "output", HelpText = "Paths to output JT files, or path to an output folder.\nIf not provided output folder is the same as input file folder.")]
        public IEnumerable<string> Output { get; set; } = Enumerable.Empty<string>();

        [Option('p', "product", Default = true, HelpText = "Outputs product data to JT file.")]
        public bool? IncludeProduct { get; set; } = null;

        [Option('r', "resource", Default = true, HelpText = "Outputs resource data to JT file.")]
        public bool? IncludeResource { get; set; } = null;

        [Option('c', "include-branches-without-cad", Default = false, HelpText = "Outputs parts of the tree without CAD to JT file.")]
        public bool IncludeBranchesWithoutCAD { get; set; } = false;

        [Option('a', "resource-sysroot-jt-files-are-assemblies", Default = true, HelpText = "Resource JT files under sys_root are assemblies,\nnot under sys_root - parts.")]
        public bool? ResourceSysRootJTFilesAreAssemblies { get; set; } = null;

        const string sampleSysRoot = @"P:\ath\to\sys_root";

        [Usage(ApplicationAlias = applicationAlias)]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Create individually named JT files", new UnParserSettings { SkipDefault = true, PreferShortName = true }, new Options
                {
                    SysRoot = sampleSysRoot,
                    Input = new[] { @"C:\input1.xml", @"C:\input2.xml" },
                    Output = new[] { @"C:\output1.jt", @"C:\output2.jt" }
                });

                yield return new Example("Create JT files named after input files in a single output folder", new UnParserSettings { SkipDefault = true, PreferShortName = false, UseEqualToken = true }, new Options
                {
                    SysRoot = sampleSysRoot,
                    Input = new[] { @"C:\input1.xml", @"C:\input2.xml" },
                    Output = new[] { @"C:\output\folder" }
                });

                yield return new Example("Create JT file with product data only", new UnParserSettings { SkipDefault = true, PreferShortName = false, UseEqualToken = true }, new Options
                {
                    SysRoot = sampleSysRoot,
                    Input = new[] { @"C:\input1.xml" },
                    IncludeResource = false
                });

                yield return new Example("Create JT file with resource data only and branches without CAD included", new UnParserSettings { SkipDefault = true, PreferShortName = true }, new Options
                {
                    SysRoot = sampleSysRoot,
                    Input = new[] { @"C:\input1.xml" },
                    IncludeProduct = false,
                    IncludeBranchesWithoutCAD = true
                });
            }
        }
    }
}
