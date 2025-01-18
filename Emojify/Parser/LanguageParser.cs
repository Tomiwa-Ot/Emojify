using Emojify.EmojiData;
using Microsoft.CodeAnalysis;

namespace Emojify.Parser
{
    /// <summary>
    /// Language Parser
    /// </summary>
    public abstract class LanguageParser
    {
        protected readonly string CharacterPool = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_-/+=";
        protected readonly Dictionary<string, string> EmojiCharacterMap = [];

        public LanguageParser()
        {
            MapCharactersToEmoji();
        }

        /// <summary>
        /// Parse c# code
        /// </summary>
        /// <param name="inputFilePath">Input file storage location</param>
        /// <param name="outputFilePath">Output file storage location</param>
        public abstract void Parse(string inputFilePath, string outputFilePath);

        /// <summary>
        /// Write code to a file
        /// </summary>
        /// <param name="content">File content</param>
        /// <param name="outputFilePath">Storage location</param>
        protected void WriteToFile(string content, string outputFilePath)
        {
            // Check if file exists
            if (!File.Exists(outputFilePath))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[-] Creating {outputFilePath}");
                Console.ResetColor();
                using (File.Create(outputFilePath)) { }
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[-] Writing to {outputFilePath}");
            Console.ResetColor();
            File.WriteAllText(outputFilePath, content);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[+] Done");
            Console.ResetColor();
        }

        /// <summary>
        /// Generate a random name 10 to 50 characters long
        /// </summary>
        /// <returns></returns>
        protected string GenerateName()
        {
            return new string(Enumerable.Range(0, new Random().Next(10, 51))
                .Select(_ => (char)new Random().Next('A', 'z' + 1))
                .Where(c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))  // Ensure it's a letter
                .ToArray());
        }

        /// <summary>
        /// Map base64 characters to an emoji
        /// </summary>
        private void MapCharactersToEmoji()
        {
            List<EmojiType> usedEmojis = [];

            foreach (char character in CharacterPool)
            {
                EmojiType emojiType;
                do
                {
                    Random random = new();
                    emojiType = Emoji.Emojis.OrderBy(x => random.Next()).First().Key;
                }
                while(usedEmojis.Contains(emojiType));


                EmojiCharacterMap.Add(character.ToString(), Emoji.Emojis[emojiType]);
                usedEmojis.Add(emojiType);
            }
        }
    }
}