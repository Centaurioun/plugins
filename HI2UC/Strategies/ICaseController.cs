namespace Nikse.SubtitleEdit.PluginLogic.Strategies
{
    public interface ICaseController
    {
        void AddResult(string before, string after, string comment, Paragraph p);
    }
}