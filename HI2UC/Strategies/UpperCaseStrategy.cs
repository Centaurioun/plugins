namespace Nikse.SubtitleEdit.PluginLogic.Strategies
{
    public class UpperCaseStrategy : ICaseStrategy
    {
        public string Name => "Uppercase";
        
        public string Execute(string input) => input.ToUpper();

        public override string ToString() => Name;
    }
}