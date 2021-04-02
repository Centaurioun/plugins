namespace Nikse.SubtitleEdit.PluginLogic.Strategies
{
    public class LowerCaseStrategy : ICaseStrategy
    {
        public string Name => "Lowercase";
        
        public string Execute(string input) => input.ToLower();

        public override string ToString() => Name;
    }
}