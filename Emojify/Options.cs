using CommandLine;

namespace Emojify
{
    /// <summary>
    /// Command Line Parameters
    /// </summary>
    class Options
    {
        /// <summary>
        /// Path of file to obfuscate
        /// </summary>
        [Option('i', "input", Required = true, HelpText = "Input file path.")]
        public string InputFilePath { get; set; }

        /// <summary>
        /// Location to save obfuscated code
        /// </summary>
        [Option('o', "output", Required = true, HelpText = "Output file path.")]
        public string OutputFilePath { get; set; }
    }
}