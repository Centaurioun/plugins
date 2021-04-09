using System.Collections.Generic;
using Nikse.SubtitleEdit.PluginLogic.Strategies;

namespace Nikse.SubtitleEdit.PluginLogic.Commands
{
    public interface IStyleCommand
    {
        void Convert(IList<Paragraph> paragraphs, ICaseController caseController);
    }
}