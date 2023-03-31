using Newtonsoft.Json;
using NWaves.Audio;
using Sampack.Core;
using Sampack.SampleGuesser.GuessLastSample;
using System.IO.Compression;
using System.Text;

namespace Sampack.Compression;
public class AudioCompressor
{
    public float Compress(Stream inputStream, Stream outputStream, ISampleGuesser guesser, int blockSize)
    {
        int nChannels = 2;
        var waveFile = new WaveFile(inputStream);
        var samples = waveFile[Channels.Interleave].Samples;

        var historicSamples = new LimitedQueue<float>(guesser.DesiredHistoricSampleCount * nChannels);
        var guessedSamples = new List<float>();

        // pre-fill the historic samples buffer with zeroes to guarantee length to compressor
        historicSamples.AddRange(Enumerable.Repeat<float>(0, historicSamples.Limit));

        foreach (var sample in samples.Chunk(nChannels))
        {
            if(guesser is CheatingGuesser cheater)
            {
                cheater.ProvideNextSample(sample, nChannels);
            }

            var guessedSample = guesser.GuessNextSample(historicSamples.ToArray(), nChannels)
                .Select(x => Math.Clamp(x, -1.0f, 1.0f));

            guessedSamples.AddRange(guessedSample);
            historicSamples.AddRange(sample);
        }

        var manifest = new Manifest
        {
            BlockSize = blockSize,
            BytesPerSample = 2,
            ChannelCount = nChannels,
            GuesserName = guesser.Name,
            SampleCount = samples.Length / nChannels
        };

        WritePackedToStream(guessedSamples, samples, manifest, outputStream);

        return GetQuality(samples, guessedSamples);
    }

    private static float GetQuality(float[] samples, List<float> guessedSamples)
    {
        // how "wrong" the guesser guessed
        var error = samples
            .Zip(guessedSamples)
            .Select(x => Math.Abs(x.First - x.Second))
            .Sum();

        // how "wrong" the guesser can possibly guess
        var maxError = samples.Length;

        return 1.0f - error / maxError;
    }

    private static void WritePackedToStream(IEnumerable<float> guessedSamples, IEnumerable<float> realSamples, Manifest manifest, Stream stream)
    {
        var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);

        WriteManifest(manifest, archive);

        using var sampleStream = archive.CreateEntry("samples.packed", CompressionLevel.SmallestSize).Open();

        var samplePairs = realSamples.Zip(guessedSamples, (guessed, real) => (Guessed: guessed, Real: real));

        foreach (var blockSamplePairs in samplePairs.Chunk(manifest.BlockSize))
        {
            var block = new byte[manifest.BlockSize * manifest.ChannelCount * 3];
            int i = 0;

            foreach (var pair in blockSamplePairs)
            {
                var real = (short)Math.Round(pair.Real * 32767.0f);
                var guessed = (short)Math.Round(pair.Guessed * 32767.0f);

                var difference = guessed - real;
                var packed = ShortToPacked(difference);

                WriteInterleaved(block, i++, packed);
            }

            sampleStream.Write(block);
        }
    }

    static void WriteInterleaved(byte[] block, int sampleIdx, uint value)
    {
        int blockSizePerChannel = block.Length / 3;

        for (int sampleBit = 0; sampleBit < 24; sampleBit++)
        {
            var bitValue = (value >> sampleBit) & 1;
            var bitIdx = sampleBit * blockSizePerChannel + sampleIdx;

            SetBit(block, bitIdx, bitValue);
        }
    }

    static void SetBit(byte[] block, int index, uint value)
    {
        var byteIdx = index / 8;
        var bitIdx = index % 8;
        block[byteIdx] = (byte)((byte)(block[byteIdx] & ~(1 << bitIdx)) | ((value & 1) << bitIdx));
    }

    static uint ShortToPacked(int x)
    {
        unchecked
        {
            if (x >= 0)
            {
                return (uint)x;
            }
            else
            {
                var packed = (uint)Math.Abs(x);
                packed <<= 1;
                packed |= 1;

                return packed;
            }
        }
    }

    private static Stream WriteManifest(Manifest manifest, ZipArchive archive)
    {
        var manifestEntry = archive.CreateEntry("manifest.json", CompressionLevel.SmallestSize);
        using var manifestStream = manifestEntry.Open();
        manifestStream.Write(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(manifest)));
        return manifestStream;
    }
}
