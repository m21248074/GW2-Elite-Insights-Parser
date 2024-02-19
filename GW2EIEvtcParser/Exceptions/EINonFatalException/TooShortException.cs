namespace GW2EIEvtcParser.Exceptions
{
    public class TooShortException : EINonFatalException
    {
        internal TooShortException(long shortnessValue, long minValue) : base("戰鬥時間太短: " + shortnessValue + " < " + minValue)
        {
        }

    }
}
