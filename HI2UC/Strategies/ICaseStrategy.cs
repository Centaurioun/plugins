namespace Nikse.SubtitleEdit.PluginLogic.Strategies
{
    public interface ICaseStrategy
    {
        string Name { get; }
        string Execute(string input);
    }
}