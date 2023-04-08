using CommandLine;
using CommandLine.Text;
using System.Diagnostics.CodeAnalysis;
using System.Management;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;

namespace QPOPs2
{
    public static partial class ArgumentParserHelpers
    {
        private static readonly Parser defaultParser = Parser.Default;
        private static readonly ParserSettings defaultSettings = defaultParser.Settings;
        private static TextWriter helpWriter = defaultSettings.HelpWriter;

        private static readonly int maxDisplayWidth = Console.WindowWidth;

        private static readonly string executableName = Path.GetFileNameWithoutExtension(Environment.ProcessPath) ?? "";

        private const int indentation = 2;

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Options))]
        public static Options? ParseArgs(string[] args, TextWriter? output = null)
        {
            if (output != null) helpWriter = output;

            using var parser = new Parser(configuration =>
            {
                configuration.HelpWriter = null;
                configuration.CaseSensitive = false;
            });

            var parserResult = parser.ParseArguments<Options>(args);

            DisplayErrors(parserResult, args);

            var options = parserResult.Value;

            if (options != null)
            {
                var validationErrors = ValidateOptions(options);

                if (validationErrors.Count > 0)
                {
                    args = args.Prepend("--help").ToArray();

                    DisplayErrors(parser.ParseArguments<Options>(args), args, validationErrors);
                    return null;
                }
            }

            return options;
        }

        private static List<string> ValidateOptions(Options? options)
        {
            if (options == null) return new();

            var errorMessages = new List<string>();

            if (!Directory.Exists(options.SysRoot))
                errorMessages.Add("Provided sys_root folder does not exist.");

            if (!(options.IncludeProduct ?? false) && !(options.IncludeResource ?? false))
                errorMessages.Add("Product and resource are excluded from output. Select at least one.");

            var inputs = options.Input.ToArray();
            if (new HashSet<string>(inputs.Select(input => input.Trim().ToLower())).Count != inputs.Length)
                errorMessages.Add("Some input paths refer to the same file.");

            foreach (var data in inputs.Select((input, index) => (input, index)))
            {
                if (!File.Exists(data.input))
                    errorMessages.Add($"Input file number {data.index + 1} does not exist.");
            }

            var outputs = options.Output.ToArray();
            var jtExtension = ".jt";
            var firstOutput = outputs.FirstOrDefault(string.Empty);
            var doesNotHaveJTExtension = Path.GetExtension(firstOutput).ToLower() != jtExtension;

            if (outputs.Length == 1 && doesNotHaveJTExtension && !Directory.Exists(firstOutput))
                errorMessages.Add("Output folder does not exist.");

            else if (outputs.Length > 0 && inputs.Length != outputs.Length)
                errorMessages.Add("Number of input and output paths does not match.");

            if (new HashSet<string>(outputs.Select(output => output.Trim().ToLower())).Count != outputs.Length)
                errorMessages.Add("Some output paths refer to the same file.");


            if (errorMessages.Count > 0) return errorMessages;

            // Adjust options

            options.SysRoot = PathToUNC(options.SysRoot);

            if (outputs.Length == 0)
                options.Output = inputs.Select(input => Path.ChangeExtension(input, jtExtension));

            else if (outputs.Length == 1 && doesNotHaveJTExtension)
                options.Output = inputs.Select(input => Path.Combine(firstOutput, Path.GetFileName(Path.ChangeExtension(input, jtExtension))));

            return errorMessages;
        }

        private static void DisplayErrors<T>(ParserResult<T> parserResult, string[] args, List<string>? validationErrors = null)
        {
            validationErrors ??= new();
            var handleValidationErrors = validationErrors.Count > 0;

            var errors = parserResult.Errors;

            if (errors.Any() || handleValidationErrors)
            {
                if (errors.IsVersion())
                {
                    defaultParser.ParseArguments<Options>(args);
                    return;
                }

                var helpText = HelpText.AutoBuild(parserResult, maxDisplayWidth);
                var sentenceBuilder = helpText.SentenceBuilder;

                var stringBuilder = new StringBuilder();

                var newLine = Environment.NewLine;

                // Version info
                stringBuilder.AppendLine(helpText.Heading);

                // Errors
                var handleErrors = !errors.IsHelp();
                if (handleErrors || handleValidationErrors)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine(sentenceBuilder.ErrorsHeadingText());

                    if (handleErrors)
                        stringBuilder.AppendLine(HelpText.RenderParsingErrorsText(parserResult, sentenceBuilder.FormatError, sentenceBuilder.FormatMutuallyExclusiveSetErrors, indentation));

                    if (handleValidationErrors)
                    {
                        foreach (var validationError in validationErrors)
                        {
                            stringBuilder.AppendLine(new string(' ', indentation) + validationError);
                        }
                    }
                }

                // Usage
                stringBuilder.AppendLine();
                stringBuilder.AppendLine(sentenceBuilder.UsageHeadingText());
                stringBuilder.AppendLine(
                    HelpText.RenderUsageText(parserResult, example => new Example($"{newLine}{example.HelpText}", example.FormatStyles, example.Sample))
                        .Replace(Options.applicationAlias, executableName)
                        .Trim()
                );

                // Options
                stringBuilder.AppendLine(string.Join(newLine, new HelpText
                {
                    AddDashesToOption = true,
                    MaximumDisplayWidth = maxDisplayWidth
                }.AddOptions(parserResult)
                    .ToString()
                    .Split(newLine)
                    .Select(line =>
                    {
                        var trimmedLine = line.Trim();

                        return trimmedLine.Length == 0 ? trimmedLine : (trimmedLine.StartsWith("-") ? newLine : string.Empty) + line;
                    })
                    .Where(line => line.Length > 0)));

                helpWriter.Write(stringBuilder);
            }
        }

        [GeneratedRegex("^([A-Za-z]:).*$", RegexOptions.Compiled)]
        private static partial Regex DriveLetterRegexp();
        private static readonly Regex driveLetterRegexp = DriveLetterRegexp();

        private static string PathToUNC(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.StartsWith(@"\\") || !Path.IsPathRooted(path)) return path;

            path = Path.GetFullPath(path);

            var driveLetterMatch = driveLetterRegexp.Match(path);

            if (driveLetterMatch.Success)
            {
                var drivePath = driveLetterMatch.Groups[1].Value;
                var driveAddress = OperatingSystem.IsWindows() ? GetDriveAddress(drivePath) : null;

                if (driveAddress != null)
                {
                    path = path.Replace(drivePath, driveAddress);
                }
            }

            return path;
        }

        [SupportedOSPlatform("windows")]
        private static string? GetDriveAddress(string drivePath)
        {
            using var managementObject = new ManagementObject();
            managementObject.Path = new ManagementPath(string.Format("Win32_LogicalDisk='{0}'", drivePath));

            try
            {
                return Convert.ToUInt32(managementObject["DriveType"]) == 4 ? Convert.ToString(managementObject["ProviderName"]) : drivePath;
            }

            catch (Exception)
            {
                return null;
            }
        }
    }
}
