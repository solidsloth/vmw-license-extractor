using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LicenseExtractor.Lib
{
    public class PdfKeyExtractor
    {
        private readonly ILogger<PdfKeyExtractor> logger;

        public const string Delimeter = " - ";

        public static Regex KeyRegex = new Regex(".*([A-Z0-9]{5}-)+.*");
        public static Regex ReplaceRegex = new Regex("([A-Z0-9]{5}-[A-Z0-9]{5}-[A-Z0-9]{5}-[A-Z0-9]{5}-[A-Z0-9]{5})");

        public PdfKeyExtractor(ILogger<PdfKeyExtractor> logger)
        {
            this.logger = logger;
        }

        public string FindKey(string pdfPath, string search)
        {
            if (string.IsNullOrWhiteSpace(pdfPath))
            {
                throw new ArgumentException("message", nameof(pdfPath));
            }

            if (string.IsNullOrWhiteSpace(search))
            {
                throw new ArgumentException("message", nameof(search));
            }

            using (var reader = new PdfReader(pdfPath))
            {
                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    string[] lines = PdfTextExtractor.GetTextFromPage(reader, i).Split('\n');

                    for (int l = 0; l < lines.Length; l++)
                    {
                        string line = lines[l];

                        int lastDash = line.LastIndexOf(Delimeter);
                        int skip = Delimeter.Length;

                        if (lastDash == -1)
                        {
                            lastDash = line.Length;
                            skip = 0;
                        }

                        string key = line.Substring(0, lastDash).Trim();
                        string value = line.Substring(lastDash + skip).Trim();

                        if (string.IsNullOrWhiteSpace(key)) continue;

                        string[] parts = line.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);

                        if (key.IndexOf(search.Split(':')[0], StringComparison.OrdinalIgnoreCase) > -1)
                        {
                            bool hasKey = KeyRegex.IsMatch(line);

                            // Check if the value is on this line or one of the following lines.
                            if (!hasKey)
                            {
                                if (search.Contains(':'))
                                {
                                    string subKey = search.Split(':')[1];

                                    while (true)
                                    {
                                        l++;

                                        string subLine = lines[l];

                                        if (!subLine.Contains(subKey)) continue;

                                        MatchCollection matches = ReplaceRegex.Matches(subLine);

                                        if (matches.Count < 1)
                                        {
                                            // Add the next line to the search in case there is a line break in the key.
                                            matches = ReplaceRegex.Matches(subLine + lines[l + 1]);
                                        }

                                        // If we still have no matches then we should just keep searching.
                                        if (matches.Count < 1)
                                        {
                                            break;
                                        }

                                        var ret = new List<string>(matches.Count);
                                        for (int m = 0; m < matches.Count; m++)
                                        {
                                            Match match = matches[m];

                                            if (!match.Success)
                                            {
                                                continue;
                                            }

                                            ret.Add(matches[m].Value);
                                        }

                                        return string.Join(Environment.NewLine, ret);
                                    }

                                    continue;
                                }
                                else
                                {
                                    i++;
                                    continue;
                                }
                            }
                            else
                            {
                                // If we're looking for a subkey, and this key exists on the current line, then this must be the wrong key.
                                if (search.Contains(":"))
                                {
                                    continue;
                                }

                                string next = lines[l + 1];
                                string withWrap = line + next;

                                MatchCollection matches = ReplaceRegex.Matches(withWrap);

                                var ret = new List<string>(matches.Count);
                                for (int m = 0; m < matches.Count; m++)
                                {
                                    Match match = matches[m];

                                    if (!match.Success)
                                    {
                                        continue;
                                    }

                                    ret.Add(matches[m].Value);
                                }

                                return string.Join(Environment.NewLine, ret);
                            }
                        }
                    }
                }
            }

            return null;
        }
    }
}
