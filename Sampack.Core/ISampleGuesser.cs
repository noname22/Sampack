namespace Sampack.Core;

public interface ISampleGuesser
{
    string Name { get; }
    int DesiredHistoricSampleCount { get; }
    float[] GuessNextSample(float[] historicSamples, int nChannels);
}
