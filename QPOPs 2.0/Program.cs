// See https://aka.ms/new-console-template for more information
using QPOPs2;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;

#if DEBUG
//args = new[] { "-s", @"\\gal71836\hq\Manufacturing\AME\VME\sys_root", "-i", @"C:\Users\mtadara1\Desktop\New folder\915.xml", "-o", @"C:\Users\mtadara1\Desktop\New folder\915_3.jt" };
//args = new[] { "--help" };
//args = new[] { "--version" };
//args = new[] { "-p", "false", "-r", "false", "-i", "i.xml", "-s", "sys_root" };
#endif

var options = ArgumentParserHelpers.ParseArgs(args, Console.Error);

if (options == null) return;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var appName = "QPOPs";
var version = "v" + (Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0");

var inputCount = options.Input.Count();

var errors = new ConcurrentBag<(string inputPath, string outputPath, string errorMessage)>();
int completeCount = 0;

var filesLabel = $"file{(inputCount > 1 ? "s" : string.Empty)}";

Console.WriteLine($"Starting to process {inputCount} {filesLabel}.");

Parallel.ForEach(Enumerable.Zip(options.Input, options.Output), inputOutput =>
{
    var (input, output) = inputOutput;

    try
    {
        using var fileStream = new FileStream(input, FileMode.Open, FileAccess.Read);

        var jtNode = XML2JT.Convert(fileStream, new XML2JTConfiguration
        {
            RootName = Path.GetFileNameWithoutExtension(input),
            SysRootPath = options.SysRoot,
            IncludeProduct = options.IncludeProduct ?? false,
            IncludeResource = options.IncludeResource ?? false,
            ResourceSysRootJTFilesAreAssemblies = options.ResourceSysRootJTFilesAreAssemblies ?? false,
            IncludeBranchesWithoutCAD = options.IncludeBranchesWithoutCAD,

            AdditionalAttributes = new Dictionary<string, string>
        {
            { appName, version }
        }
        });

        var jtNodeBytes = jtNode.ToBytes();

        File.WriteAllBytes(output, jtNodeBytes);

        Console.Write($"\r{Interlocked.Increment(ref completeCount)} of {inputCount} {filesLabel} processed.");
    }

    catch(Exception e)
    {
        errors.Add((input, output, e.ToString()));
    }
});

Console.Error.WriteLine($"\r{completeCount} of {inputCount} {filesLabel} processed successfully.");

if(!errors.IsEmpty)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine($"ERROR{(errors.Count > 1 ? "S" : string.Empty)}:");

    foreach(var (inputPath, outputPath, errorMessage) in errors)
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine($" Input: {inputPath}");
        Console.Error.WriteLine($"Output: {outputPath}");
        Console.Error.WriteLine($" Error: {errorMessage}");
    }
}