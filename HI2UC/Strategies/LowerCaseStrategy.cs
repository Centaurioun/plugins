using System.Globalization;

namespace Nikse.SubtitleEdit.PluginLogic.Strategies
{
    public class LowerCaseStrategy : ICaseStrategy
    {
        public LowerCaseStrategy(CultureInfo culture)
        {
            Culture = culture;
        }

        public CultureInfo Culture { get; }
        
        public string Name => "Lowercase";
        
        public string Execute(string input) => input.ToLower(Culture);

        public override string ToString() => Name;
    }
}