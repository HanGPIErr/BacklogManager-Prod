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
            var paragraph = new Paragraph { LineHeight = 20, Margin = new Thickness(0) };

            // Parser le texte ligne par ligne
            var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.None);
            
            bool inCodeBlock = false;
            var codeBlockContent = new System.Text.StringBuilder();
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmedLine = line.Trim();
                
                // Détecter les blocs de code ```
                if (trimmedLine.StartsWith("```"))
                {
                    if (!inCodeBlock)
                    {
                        // Début du bloc de code
                        inCodeBlock = true;
                        codeBlockContent.Clear();
                        
                        // Ajouter le paragraphe actuel avant le bloc de code
                        if (paragraph.Inlines.Count > 0)
                        {
                            flowDocument.Blocks.Add(paragraph);
                            paragraph = new Paragraph { LineHeight = 20, Margin = new Thickness(0) };
                        }
                    }
                    else
                    {
                        // Fin du bloc de code
                        inCodeBlock = false;
                        
                        // Créer le paragraphe de code
                        var codePara = new Paragraph(new Run(codeBlockContent.ToString().TrimEnd()))
                        {
                            FontFamily = new FontFamily("Consolas"),
                            FontSize = 12,
                            Background = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)),
                            Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                            Padding = new Thickness(12, 8, 12, 8),
                            Margin = new Thickness(0, 8, 0, 8),
                            BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                            BorderThickness = new Thickness(1)
                        };
                        flowDocument.Blocks.Add(codePara);
                        codeBlockContent.Clear();
                    }
                    continue;
                }
                
                // Si on est dans un bloc de code, accumuler le contenu
                if (inCodeBlock)
                {
                    codeBlockContent.AppendLine(line);
                    continue;
                }
                
                // Ignorer les lignes vides
                if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    if (paragraph.Inlines.Count > 0)
                    {
                        flowDocument.Blocks.Add(paragraph);
                        paragraph = new Paragraph { LineHeight = 20, Margin = new Thickness(0) };
                    }
                    continue;
                }
                
                // Liste à puces
                if (trimmedLine.StartsWith("• ") || trimmedLine.StartsWith("- ") || trimmedLine.StartsWith("* "))
                {
                    if (paragraph.Inlines.Count > 0)
                    {
                        flowDocument.Blocks.Add(paragraph);
                        paragraph = new Paragraph { LineHeight = 20, Margin = new Thickness(0) };
                    }
                    
                    var listText = trimmedLine.Substring(2);
                    var listPara = new Paragraph(ParseInlineFormatting(listText))
                    {
                        Margin = new Thickness(20, 2, 0, 2),
                        TextIndent = -15,
                        LineHeight = 20
                    };
                    flowDocument.Blocks.Add(listPara);
                }
                // Liste numérotée (ex: "1. ", "2. ")
                else if (Regex.IsMatch(trimmedLine, @"^\d+\.\s"))
                {
                    if (paragraph.Inlines.Count > 0)
                    {
                        flowDocument.Blocks.Add(paragraph);
                        paragraph = new Paragraph { LineHeight = 20, Margin = new Thickness(0) };
                    }
                    
                    var match = Regex.Match(trimmedLine, @"^(\d+)\.\s(.+)");
                    if (match.Success)
                    {
                        var number = match.Groups[1].Value;
                        var listText = match.Groups[2].Value;
                        var listPara = new Paragraph
                        {
                            Margin = new Thickness(20, 2, 0, 2),
                            LineHeight = 20
                        };
                        listPara.Inlines.Add(new Run($"{number}. ") { FontWeight = FontWeights.SemiBold });
                        listPara.Inlines.Add(ParseInlineFormatting(listText));
                        flowDocument.Blocks.Add(listPara);
                    }
                }
                // Titre ### (h3)
                else if (trimmedLine.StartsWith("### "))
                {
                    if (paragraph.Inlines.Count > 0)
                    {
                        flowDocument.Blocks.Add(paragraph);
                        paragraph = new Paragraph { LineHeight = 20, Margin = new Thickness(0) };
                    }
                    
                    var titleText = trimmedLine.Substring(4);
                    var titlePara = new Paragraph(new Run(titleText))
                    {
                        FontWeight = FontWeights.SemiBold,
                        FontSize = 14,
                        Margin = new Thickness(0, 12, 0, 6),
                        Foreground = new SolidColorBrush(Color.FromRgb(52, 73, 94))
                    };
                    flowDocument.Blocks.Add(titlePara);
                }
                // Titre ## (h2)
                else if (trimmedLine.StartsWith("## "))
                {
                    if (paragraph.Inlines.Count > 0)
                    {
                        flowDocument.Blocks.Add(paragraph);
                        paragraph = new Paragraph { LineHeight = 20, Margin = new Thickness(0) };
                    }
                    
                    var titleText = trimmedLine.Substring(3);
                    var titlePara = new Paragraph(new Run(titleText))
                    {
                        FontWeight = FontWeights.Bold,
                        FontSize = 15,
                        Margin = new Thickness(0, 12, 0, 6),
                        Foreground = new SolidColorBrush(Color.FromRgb(52, 73, 94))
                    };
                    flowDocument.Blocks.Add(titlePara);
                }
                // Titre # (h1)
                else if (trimmedLine.StartsWith("# "))
                {
                    if (paragraph.Inlines.Count > 0)
                    {
                        flowDocument.Blocks.Add(paragraph);
                        paragraph = new Paragraph { LineHeight = 20, Margin = new Thickness(0) };
                    }
                    
                    var titleText = trimmedLine.Substring(2);
                    var titlePara = new Paragraph(new Run(titleText))
                    {
                        FontWeight = FontWeights.Bold,
                        FontSize = 16,
                        Margin = new Thickness(0, 12, 0, 8),
                        Foreground = new SolidColorBrush(Color.FromRgb(52, 73, 94))
                    };
                    flowDocument.Blocks.Add(titlePara);
                }
                // Séparateur horizontal
                else if (trimmedLine == "---" || trimmedLine == "***" || trimmedLine == "___")
                {
                    if (paragraph.Inlines.Count > 0)
                    {
                        flowDocument.Blocks.Add(paragraph);
                        paragraph = new Paragraph { LineHeight = 20, Margin = new Thickness(0) };
                    }
                    
                    var separator = new Paragraph
                    {
                        BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                        BorderThickness = new Thickness(0, 0, 0, 1),
                        Margin = new Thickness(0, 8, 0, 8),
                        Padding = new Thickness(0)
                    };
                    flowDocument.Blocks.Add(separator);
                }
                // Texte normal
                else
                {
                    paragraph.Inlines.Add(ParseInlineFormatting(trimmedLine));
                    if (i < lines.Length - 1)
                    {
                        paragraph.Inlines.Add(new LineBreak());
                    }
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
                        formattedRun.FontSize = 13;
                        formattedRun.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                        formattedRun.Foreground = new SolidColorBrush(Color.FromRgb(212, 73, 80));
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
