using System.Collections.Generic;

namespace Nikse.SubtitleEdit.PluginLogic.Strategies
{
    public class CaseController : ICaseController
    {
        public IList<Record> Records { get; }

        public CaseController()
        {
            Records = new List<Record>();
        }

        public void AddResult(string before, string after, string comment, Paragraph p)
        {
            Records.Add(new Record
            {
                Before = before,
                After = after,
                Comment = comment,
                Paragraph = p
            });
        }
    }
}