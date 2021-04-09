using System.Globalization;

namespace Nikse.SubtitleEdit.PluginLogic.Strategies
{
    public class SentenceCaseStrategy : ICaseStrategy
    {
        public SentenceCaseStrategy(CultureInfo culture)
        {
            Culture = culture;
        }

        public CultureInfo Culture { get; }

        public string Name => "Sentence case";

        public string Execute(string input) => input.ToLower(Culture).CapitalizeFirstLetter(Culture);

        public override string ToString() => Name;
    }
}