namespace GW2EIEvtcParser.Exceptions
{
    public class SkipException : EINonFatalException
    {
        internal SkipException() : base("選項已啟用 - 跳過敗戰日誌")
        {
        }

    }
}
