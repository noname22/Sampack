using Sampack.Core;

namespace Sampack.SampleGuesser.GuessLastSample;
public class GuessLastSample : ISampleGuesser
{
    public string Name => "guess-last-sample";
    public int DesiredHistoricSampleCount => 1;
    public float[] GuessNextSample(float[] historicSamples, int nChannels) => historicSamples;
}
