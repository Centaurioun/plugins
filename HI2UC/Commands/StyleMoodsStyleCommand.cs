using System;
using System.Collections.Generic;
using Nikse.SubtitleEdit.PluginLogic.Strategies;

namespace Nikse.SubtitleEdit.PluginLogic.Commands
{
    public class StyleMoodsStyleCommand : IStyleCommand
    {
        private static readonly char[] Symbols = {'.', '!', '?', ')', ']'};
        private static readonly char[] HiChars = {'(', '['};

        private ICaseStrategy CaseStrategy { get; }

        public StyleMoodsStyleCommand(ICaseStrategy caseStrategy)
        {
            CaseStrategy = caseStrategy;
        }

        public void Convert(IList<Paragraph> paragraphs, ICaseController caseController)
        {
            foreach (var paragraph in paragraphs)
            {
                string text = paragraph.Text;

                // doesn't have balanced brackets. O(2n)
                if (!(HasBalancedParentheses(text) && HasBalancedBrackets(text)))
                {
                    caseController.AddResult(text, text, "Line contains unbalanced []/()", paragraph);
                }

                string output = Convert(text);

                if (!output.Equals(paragraph.Text, StringComparison.Ordinal))
                {
                    caseController.AddResult(text, output, "Moods", paragraph);
                    paragraph.Text = output;
                }
            }
        }

        private string Convert(string input)
        {
            int j = 0;
            for (var i = input.Length - 1; i > 0; i--)
            {
                var ch = input[i];
                if (ch == ']')
                {
                    j = i;
                }
                else if (ch == '[')
                {
                    if (j - i > 1)
                    {
                        int fromIdx = i + 1;
                        string textInside = input.Substring(fromIdx, j - fromIdx);

                        input = !string.IsNullOrWhiteSpace(HtmlUtils.RemoveTags(textInside))
                            ? input.Remove(fromIdx, j - fromIdx).Insert(fromIdx, CaseStrategy.Execute(textInside))
                            : input.Remove(i, j - i + 1); // e.g: Foobar [ ] => Foobar!
                    }
                    else
                    {
                        input = input.Remove(i, 2);
                    }

                    j = -1;
                }
            }

            return input.FixExtraSpaces();
        }

        private bool HasBalancedParentheses(string input)
        {
            int count = 0;
            for (int i = input.Length - 1; i >= 0; i--)
            {
                char ch = input[i];
                if (ch == ')')
                {
                    count++;
                }
                else if (ch == '(')
                {
                    count--;
                }

                // even if you check to the end there won't be enough to balance
                if (i - count < 0)
                {
                    Console.WriteLine("too much close");
                    return false;
                }
            }

            if (count > 0)
            {
                Console.WriteLine("too much close");
            }
            else if (count < 0)
            {
                Console.WriteLine("too much open");
            }

            return count == 0;
        }

        private bool HasBalancedBrackets(string input)
        {
            int count = 0;
            for (int i = input.Length - 1; i >= 0; i--)
            {
                char ch = input[i];
                if (ch == ']')
                {
                    count++;
                }
                else if (ch == '[')
                {
                    count--;
                }

                // even if you check to the end there won't be enough to balance
                if (i - count < 0)
                {
                    Console.WriteLine("too much close");
                    return false;
                }
            }

            if (count > 0)
            {
                Console.WriteLine("too much close");
            }
            else if (count < 0)
            {
                Console.WriteLine("too much open");
            }

            return count == 0;
        }
    }
}