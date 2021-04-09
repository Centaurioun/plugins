using System.Globalization;

namespace Nikse.SubtitleEdit.PluginLogic.Strategies
{
    public interface ICaseStrategy
    {
        CultureInfo Culture { get; }
        string Name { get; }
        string Execute(string input);
    }
}