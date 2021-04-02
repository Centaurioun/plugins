namespace Nikse.SubtitleEdit.PluginLogic
{
    public static class Options
    {
        static Options()
        {
            Frame = 23.976;
            UiLineBreak = "<br />";
        }

        public static double Frame { get; set; }
        public static string UiLineBreak { get; set; }
    }
}
