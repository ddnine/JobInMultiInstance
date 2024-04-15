namespace JobInMultiInstance;

internal static class Constants
{
        
    public const int MaxCallbackRetryTimes = 10;
    //每次回调最多发送几条记录
    public const int MaxCallbackRecordsPerRequest =5;
    public static TimeSpan CallbackRetryInterval = TimeSpan.FromSeconds(600);
        

    public static class GlueType
    {
        public const string Default = "Default";
    }

    public static class ExecutorBlockStrategy
    {
        public const string SERIAL_EXECUTION = "SERIAL_EXECUTION";

        public const string DISCARD_LATER = "DISCARD_LATER";

        public const string COVER_EARLY = "COVER_EARLY";
    }
}