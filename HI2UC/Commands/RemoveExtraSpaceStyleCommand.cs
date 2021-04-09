using System.Collections.Generic;
using System.Text.RegularExpressions;
using Nikse.SubtitleEdit.PluginLogic.Strategies;

namespace Nikse.SubtitleEdit.PluginLogic.Commands
{
    public class RemoveExtraSpaceStyleCommand : IStyleCommand
    {
        private static readonly Regex PreRegex = new("(?<=\\()\\s+");
        private static readonly Regex PostRergex = new("\\s+(?=[)\\]])");

        public void Convert(IList<Paragraph> paragraphs, ICaseController caseController)
        {
            foreach (var paragraph in paragraphs)
            {
                string text = paragraph.Text;
                string output = PreRegex.Replace(text, string.Empty);
                output = PostRergex.Replace(text, string.Empty);
                if (!text.Equals(output))
                {
                    caseController.AddResult(text, output, "Trim padding", paragraph);
                    paragraph.Text = output;
                }
            }
        }
    }
}