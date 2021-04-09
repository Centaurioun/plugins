using System;
using System.Collections.Generic;
using Nikse.SubtitleEdit.PluginLogic.Strategies;

namespace Nikse.SubtitleEdit.PluginLogic.Commands
{
    public class StyleNarratorStyleCommand : IStyleCommand
    {
        private static readonly char[] CloseChars = {']', ')', '>', '.', '?', '!', '}', '¿', '¡'};

        private ICaseStrategy CaseStrategy { get; }

        public StyleNarratorStyleCommand(ICaseStrategy caseStrategy)
        {
            CaseStrategy = caseStrategy;
        }

        public void Convert(IList<Paragraph> paragraphs, ICaseController caseController)
        {
            foreach (var paragraph in paragraphs)
            {
                string output = Convert(paragraph.Text);
                if (!paragraph.Text.Equals(output, StringComparison.Ordinal))
                {
                    caseController.AddResult(paragraph.Text, output, "Narrator converted", paragraph);
                    paragraph.Text = output;
                }
            }
        }

        private string Convert(string text)
        {
            string noTagText = HtmlUtils.RemoveTags(text, true).TrimEnd().TrimEnd('"');

            if (noTagText.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return text;
            }

            // Skip single line that ends with ':'.
            if (noTagText.IndexOf(':') < 0)
            {
                return text;
            }

            // Lena:
            // A ring?!

            // todo: handle
            //if (!Config.SingleLineNarrator && index + 1 == newLineIdx)
            //{
            //    return text;
            //}

            string[] lines = text.SplitToLines();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string noTagLine = HtmlUtils.RemoveTags(line, true);

                int noTagColonIdx = noTagLine.IndexOf(':');
                if (noTagColonIdx < 1)
                {
                    continue;
                }

                // Only allow colon at last position if it's 1st line.
                if (noTagColonIdx + 1 == noTagLine.Length && i + 1 == lines.Length)
                {
                    continue;
                }

                if (!IsQualifiedNarrator(noTagLine, noTagColonIdx))
                {
                    continue;
                }

                // Find index from original text.
                int colonIdx = line.IndexOf(':');

                // [foobar] Narrator: Hello world!
                int startIdx = Math.Max(0, line.LastIndexOfAny(CloseChars, colonIdx) + 1);

                // skip white-spaces
                while (startIdx < colonIdx && line[startIdx] == ' ') startIdx++;

                if (startIdx < colonIdx)
                {
                    var narrator = line.Substring(startIdx, colonIdx - startIdx);
                    lines[i] = (startIdx > 0 ? line.Substring(0, startIdx) : string.Empty) +
                               CaseStrategy.Execute(narrator) + line.Substring(colonIdx);
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        private string ConvertNarrators(string text)
        {
            int j = 0;
            for (int i = text.Length - 1; i >= 0; i--)
            {
                char ch = text[i];
                if (ch == ':')
                {
                    // check if doesn't precede a digit
                    if (i + 1 < text.Length && char.IsDigit(text[i + 1]) == false)
                    {
                        j = i;
                    }
                }
                // skip narrators inside brackets
                else if (ch == '(' || ch == '[' && j > i)
                {
                    j = -1;
                }
                // valid narrator found
                else if (j > 0 && (i == 0 || (ch == '.' || ch == '?' || ch == '!')))
                {
                    // foobar. narattor: hello world!

                    int k = i + 1;
                    while (k < j && text[k] == ' ')
                    {
                        k++;
                    }

                    string textFromRange = CaseStrategy.Execute(text.Substring(k, j - k));
                    text = text.Remove(k, j - k).Insert(k, textFromRange);
                }
            }

            return text;
        }

        private static bool IsQualifiedNarrator(string noTagsLine, int noTagColon)
        {
            int symbolIndex = Math.Max(noTagsLine.LastIndexOfAny(CloseChars, noTagColon), 0);
            string potentialNarrator = noTagsLine.Substring(symbolIndex + 1, noTagColon - symbolIndex).TrimStart();
            if (string.IsNullOrWhiteSpace(potentialNarrator))
            {
                return false;
            }

            if (noTagColon + 1 < noTagsLine.Length)
            {
                // e.g: 12:30am...
                if (char.IsDigit(noTagsLine[noTagColon + 1]) && noTagColon - 1 >= 0 &&
                    char.IsDigit(noTagsLine[noTagColon - 1]))
                {
                    return false;
                }

                // slash after https://
                if (noTagsLine[noTagColon + 1] == '/')
                {
                    return false;
                }
            }

            // ignore: - where it's safest. BRAN: No.
            if (symbolIndex > 0)
            {
                // text before symbol
                string preText = noTagsLine.Substring(0, symbolIndex + 1).Trim();
                // text after symbols exclude colon
                string textAfterSymbols = noTagsLine.Substring(symbolIndex + 1, noTagColon - symbolIndex - 1).Trim();

                // post symbol is uppercase - pre unnecessary
                if (textAfterSymbols.Equals(textAfterSymbols.ToUpper()) && preText.Equals(preText.ToUpper()) == false)
                {
                    return false;
                }
            }

            // Foobar[?!] Narrator: Hello (Note: not really sure if "." (dot) should be include since there are names
            // that are prefixed with Mr. Ivandro Ismael)
            return !potentialNarrator.ContainsAny(CloseChars) &&
                   !potentialNarrator.StartsWith("http", StringComparison.OrdinalIgnoreCase) &&
                   !potentialNarrator.EndsWith("improved by", StringComparison.OrdinalIgnoreCase) &&
                   !potentialNarrator.EndsWith("corrected by", StringComparison.OrdinalIgnoreCase) &&
                   !potentialNarrator.EndsWith("https", StringComparison.OrdinalIgnoreCase) &&
                   !potentialNarrator.EndsWith("http", StringComparison.OrdinalIgnoreCase);
        }
    }
}