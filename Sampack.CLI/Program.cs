using ReflectiveArguments;
using Sampack.Compression;
using Sampack.Core;
using Sampack.SampleGuesser.GuessLastSample;

namespace Sampack.CLI;

internal class Program
{
    static ISampleGuesser[] guessers = new ISampleGuesser[]
    {
        new GuessLastSample(),
        new CheatingGuesser()
    };

    static int Main(string[] args)
    {
        return new Command("Sampack", "A lossless audio compression utility.")
            .AddCommand(Compress, "Compress a file")
            .HandleCommandLine(args);
    }

    static void Compress(string inputFilePath, string outputFilePath, string guesser = "guess-last-sample", int blockSize = 256)
    {
        var compressor = new AudioCompressor();

        using var inputStream = File.OpenRead(inputFilePath);
        using var outputStream = File.Create(outputFilePath);

        var guesserInstance = guessers.Single(x => x.Name == guesser);

        var quality = compressor.Compress(inputStream, outputStream, guesserInstance, blockSize);

        Console.WriteLine($"{inputFilePath} -> {outputFilePath}, guesser quality: {quality:F5}, " +
            $"input size: {inputStream.Length:N00}, output size: {outputStream.Length:N00} " +
            $"(reduced to: {(outputStream.Length / (double)inputStream.Length) * 100.0:F2}%)");
    }
}
