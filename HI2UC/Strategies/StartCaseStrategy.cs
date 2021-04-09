using System.Globalization;
using System.Linq;

namespace Nikse.SubtitleEdit.PluginLogic.Strategies
{
    public class StartCaseStrategy : ICaseStrategy
    {
        public StartCaseStrategy(CultureInfo culture)
        {
            Culture = culture;
        }

        public CultureInfo Culture { get; }
        
        public string Name => "Start case";

        public string Execute(string input)
        {
            return string.Join(" ", input.Split(' ', '-').Select(w => w.ToLower().CapitalizeFirstLetter()));
        }

        public override string ToString() => Name;
    }
}