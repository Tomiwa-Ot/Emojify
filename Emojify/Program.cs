using CommandLine;
using Emojify;
using Emojify.Parser;

Parser.Default.ParseArguments<Options>(args)
    .WithParsed(options => {
        PrintBanner();
        // Check if the file exists
        if (!File.Exists(options.InputFilePath))
        {

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[!] Error: The file '{options.InputFilePath}' does not exist.");
            Console.ResetColor();
            Environment.Exit(1);
        }

        new CS().Parse(options.InputFilePath, options.OutputFilePath);
    })
    .WithNotParsed(HandleParseErrors);


void HandleParseErrors(IEnumerable<Error> errors)
{
    PrintBanner();
    foreach (var error in errors)
    {

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[!] {error.Tag}");
        Console.ResetColor();
    }
}


void PrintBanner()
{
    Console.WriteLine(@"
    🔋⌚🐆💀                                     
    ⌛       🌵     🌵      🌈 🍡        🥚 👖  👢🎹🎹 🍬   🌽 
    🔋       🍟🔨 🔑🍟    🚀    🍭       🗼 🎍  💄       🍬🌽  
    ⌛🌷💀   🌵  🚬 🌵    ⚽    🎱       🥚 👖  👢🎹🎹    🚩   
    🔋       🍟     🍟    🎱    ⚽       🗼 🎍  💄        🎌   
    ⌛       🌵     🌵    🍭    🔪 👝    🥚 👖  👢        🏁   
    🔋⌚🐆💀 🍟     🍟      🔪🏈   🎽💼🗼   🎍  💄        🚩   
    ");
}