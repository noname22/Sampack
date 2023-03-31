using Sampack.Core;

namespace Sampack.SampleGuesser.GuessLastSample;
public class CheatingGuesser : ISampleGuesser
{
    public string Name => "cheating-guesser";
    public int DesiredHistoricSampleCount => 1;

    float[] nextSample = null!;

    public float[] GuessNextSample(float[] historicSamples, int nChannels) => nextSample;

    public void ProvideNextSample(float[] nextSample, int nChannels) => this.nextSample = nextSample;
}
