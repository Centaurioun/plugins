using System;
using System.Collections.Generic;
using System.Text;

namespace Nikse.SubtitleEdit.PluginLogic.Helpers
{
    public class UiUtils
    {
        public static string GetListViewTextFromString(string s) => s.Replace(Environment.NewLine, Options.UiLineBreak);

        public static string GetStringFromListViewText(string lviText) => lviText.Replace(Options.UiLineBreak, Environment.NewLine);
    }
}
