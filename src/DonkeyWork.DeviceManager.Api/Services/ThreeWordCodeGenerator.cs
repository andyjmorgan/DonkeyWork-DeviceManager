namespace DonkeyWork.DeviceManager.Api.Services;

using System.Security.Cryptography;

/// <summary>
/// Generates cryptographically secure three-word codes for device registration.
/// </summary>
public class ThreeWordCodeGenerator
{
    private static readonly Lazy<string[]> WordList = new(() => LoadWordList());

    private static string[] LoadWordList()
    {
        var wordlistPath = Path.Combine(AppContext.BaseDirectory, "wordlist-2048.txt");
        return File.ReadAllLines(wordlistPath);
    }

    /// <summary>
    /// Generates a three-word code (e.g., "happy-mountain-river").
    /// </summary>
    public string Generate()
    {
        var words = new string[3];
        var wordList = WordList.Value;

        for (int i = 0; i < 3; i++)
        {
            var bytes = new byte[4];
            RandomNumberGenerator.Fill(bytes);
            var index = BitConverter.ToUInt32(bytes, 0) % wordList.Length;
            words[i] = wordList[index];
        }

        return string.Join("-", words);
    }
}
