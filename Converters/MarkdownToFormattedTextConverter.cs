using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows;

namespace BacklogManager.Converters
{
    public class MarkdownToFormattedTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            
            var text = value.ToString();
            var flowDocument = new FlowDocument();
            var paragraph = new Paragraph { LineHeight = 20 };

            // Parser le texte ligne par ligne
            var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // Ignorer les lignes vides
                if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    paragraph.Inlines.Add(new LineBreak());
                    continue;
                }
                
                // Liste à puces
                if (trimmedLine.StartsWith("• ") || trimmedLine.StartsWith("- ") || trimmedLine.StartsWith("* "))
                {
                    var listText = trimmedLine.Substring(2);
                    var listPara = new Paragraph(ParseInlineFormatting(listText))
                    {
                        Margin = new Thickness(20, 2, 0, 2),
                        TextIndent = -15
                    };
                    flowDocument.Blocks.Add(listPara);
                }
                // Liste numérotée (ex: "1. ", "2. ")
                else if (Regex.IsMatch(trimmedLine, @"^\d+\.\s"))
                {
                    var match = Regex.Match(trimmedLine, @"^\d+\.\s(.+)");
                    if (match.Success)
                    {
                        var listText = match.Groups[1].Value;
                        var listPara = new Paragraph(ParseInlineFormatting(listText))
                        {
                            Margin = new Thickness(20, 2, 0, 2),
                            TextIndent = -15
                        };
                        flowDocument.Blocks.Add(listPara);
                    }
                }
                // Titre (commence par #)
                else if (trimmedLine.StartsWith("# "))
                {
                    var titleText = trimmedLine.Substring(2);
                    var titlePara = new Paragraph(new Run(titleText))
                    {
                        FontWeight = FontWeights.Bold,
                        FontSize = 16,
                        Margin = new Thickness(0, 8, 0, 4)
                    };
                    flowDocument.Blocks.Add(titlePara);
                }
                // Séparateur horizontal
                else if (trimmedLine == "---" || trimmedLine == "***" || trimmedLine == "___")
                {
                    var separator = new Paragraph
                    {
                        BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                        BorderThickness = new Thickness(0, 0, 0, 1),
                        Margin = new Thickness(0, 10, 0, 10),
                        Padding = new Thickness(0)
                    };
                    flowDocument.Blocks.Add(separator);
                }
                // Texte normal
                else
                {
                    paragraph.Inlines.Add(ParseInlineFormatting(trimmedLine));
                    paragraph.Inlines.Add(new LineBreak());
                }
            }
            
            if (paragraph.Inlines.Count > 0)
            {
                flowDocument.Blocks.Add(paragraph);
            }

            return flowDocument;
        }

        private Inline ParseInlineFormatting(string text)
        {
            var span = new Span();
            var lastIndex = 0;

            // Gras **texte**
            var boldPattern = @"\*\*(.+?)\*\*";
            var boldMatches = Regex.Matches(text, boldPattern);
            
            // Italique *texte*
            var italicPattern = @"\*(.+?)\*";
            var italicMatches = Regex.Matches(text, italicPattern);

            // Code `texte`
            var codePattern = @"`(.+?)`";
            var codeMatches = Regex.Matches(text, codePattern);

            // Combiner toutes les correspondances
            var allMatches = new System.Collections.Generic.List<(int start, int length, string type, string content)>();
            
            foreach (Match match in boldMatches)
            {
                allMatches.Add((match.Index, match.Length, "bold", match.Groups[1].Value));
            }
            
            foreach (Match match in italicMatches)
            {
                // Éviter les doublons avec le gras
                if (!text.Substring(Math.Max(0, match.Index - 1), Math.Min(text.Length - Math.Max(0, match.Index - 1), match.Length + 2)).Contains("**"))
                {
                    allMatches.Add((match.Index, match.Length, "italic", match.Groups[1].Value));
                }
            }
            
            foreach (Match match in codeMatches)
            {
                allMatches.Add((match.Index, match.Length, "code", match.Groups[1].Value));
            }

            // Trier par position
            allMatches.Sort((a, b) => a.start.CompareTo(b.start));

            // Construire le texte formaté
            foreach (var match in allMatches)
            {
                // Ajouter le texte avant
                if (match.start > lastIndex)
                {
                    span.Inlines.Add(new Run(text.Substring(lastIndex, match.start - lastIndex)));
                }

                // Ajouter le texte formaté
                Run formattedRun = new Run(match.content);
                
                switch (match.type)
                {
                    case "bold":
                        formattedRun.FontWeight = FontWeights.Bold;
                        break;
                    case "italic":
                        formattedRun.FontStyle = FontStyles.Italic;
                        break;
                    case "code":
                        formattedRun.FontFamily = new FontFamily("Consolas");
                        formattedRun.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                        break;
                }
                
                span.Inlines.Add(formattedRun);
                lastIndex = match.start + match.length;
            }

            // Ajouter le reste du texte
            if (lastIndex < text.Length)
            {
                span.Inlines.Add(new Run(text.Substring(lastIndex)));
            }

            if (span.Inlines.Count > 0)
            {
                return span;
            }
            else
            {
                return new Run(text);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
