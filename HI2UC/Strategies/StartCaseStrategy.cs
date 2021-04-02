using System.Linq;

namespace Nikse.SubtitleEdit.PluginLogic.Strategies
{
    public class StartCaseStrategy : ICaseStrategy
    {
        public string Name => "Start case";

        public string Execute(string input)
        {
            return string.Join(" ", input.Split(' ', '-').Select(w => w.CapitalizeFirstLetter()));
        }

        public override string ToString() => Name;
    }
}