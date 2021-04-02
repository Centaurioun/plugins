using System.Globalization;

namespace Nikse.SubtitleEdit.PluginLogic.Strategies
{
    public class SentenceCaseStrategy : ICaseStrategy
    {
        public string Name => "Sentence case";

        public string Execute(string input) => input.CapitalizeFirstLetter();

        public override string ToString() => Name;
    }
}