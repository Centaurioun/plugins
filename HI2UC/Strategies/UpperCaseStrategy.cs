using System.Globalization;

namespace Nikse.SubtitleEdit.PluginLogic.Strategies
{
    public class UpperCaseStrategy : ICaseStrategy
    {
        public UpperCaseStrategy(CultureInfo culture)
        {
            Culture = culture;
        }

        public CultureInfo Culture { get; }
        
        public string Name => "Uppercase";
        
        public string Execute(string input) => input.ToUpper(Culture);

        public override string ToString() => Name;
    }
}