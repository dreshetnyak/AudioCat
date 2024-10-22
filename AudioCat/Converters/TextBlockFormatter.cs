using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows;
using System.Text;

namespace AudioCat.Converters;

internal class TextBlockFormatter
{
    #region Internal Types
    private enum FormatterType
    {
        Bold,
        Italic,
        Underline
    }

    private sealed class Formatter(string start, string end, FormatterType type)
    {
        public string Start { get; } = start;
        public string End { get; } = end;
        public FormatterType Type { get; } = type;
    }

    #endregion

    private static readonly Formatter[] Formatters =
    [
        new Formatter("[b]", "[/b]", FormatterType.Bold),
        new Formatter("[i]", "[/i]", FormatterType.Italic),
        new Formatter("[u]", "[/u]", FormatterType.Underline)
    ];

    public static readonly DependencyProperty FormattedTextProperty =
    DependencyProperty.RegisterAttached(
        "FormattedText",
        typeof(string),
        typeof(TextBlockFormatter),
        new PropertyMetadata(null, OnFormattedTextChanged));

    public static string GetFormattedText(DependencyObject obj) => 
        (string)obj.GetValue(FormattedTextProperty);

    public static void SetFormattedText(DependencyObject obj, string value) => 
        obj.SetValue(FormattedTextProperty, value);

    private static void OnFormattedTextChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not TextBlock textBlock || e.NewValue == null) 
            return;

        textBlock.Inlines.Clear();
        var input = (string)e.NewValue;
        var output = new StringBuilder();
        var formatters = new List<FormatterType>();

        var processedBytes = 0;
        for (var readOffset = 0; readOffset < input.Length; readOffset += processedBytes)
        {
            if (TryGetFormatterStartAt(input, readOffset, out var startingFormatter))
            {
                AddFormattedText(textBlock, output, formatters);
                formatters.Add(startingFormatter!.Type);
                processedBytes = startingFormatter.Start.Length;
            }
            else if (TryGetFormatterEndAt(input, readOffset, out var endingFormatter))
            {
                AddFormattedText(textBlock, output, formatters);
                RemoveFormatter(formatters, endingFormatter!.Type);
                processedBytes = endingFormatter.End.Length;
            }

            output.Append(input[readOffset]);
            processedBytes++;
        }

        AddFormattedText(textBlock, output, formatters);
    }

    private static bool TryGetFormatterStartAt(string value, int readIndex, out Formatter? startingFormatter)
    {
        foreach (var formatter in Formatters)
        {
            if (!value.AsSpan(readIndex).StartsWith(formatter.Start))
                continue;
            startingFormatter = formatter;
            return true;
        }

        startingFormatter = null;
        return false;
    }

    private static bool TryGetFormatterEndAt(string value, int readOffset, out Formatter? endingFormatter)
    {
        foreach (var formatter in Formatters)
        {
            if (!value.AsSpan(readOffset).StartsWith(formatter.End))
                continue;
            endingFormatter = formatter;
            return true;
        }

        endingFormatter = null;
        return false;
    }

    private static void RemoveFormatter(List<FormatterType> formatters, FormatterType type)
    {
        var typeIndex = formatters.LastIndexOf(type);
        if (typeIndex != -1)
            formatters.RemoveAt(typeIndex);
    }

    private static void AddFormattedText(TextBlock textBlock, StringBuilder text, IReadOnlyList<FormatterType> formats)
    {
        if (text.Length == 0)
            return;
        textBlock.Inlines.Add(FormatText(text, formats));
        text.Clear();
    }

    private static Inline FormatText(StringBuilder text, IReadOnlyList<FormatterType> formats)
    {
        Inline inline = new Run(text.ToString());
        foreach (var formatType in formats)
        {
            switch (formatType)
            {
                case FormatterType.Bold:
                    inline = new Bold(inline);
                    break;
                case FormatterType.Italic:
                    inline = new Italic(inline);
                    break;
                case FormatterType.Underline:
                    inline = new Underline(inline);
                    break;
            }
        }

        return inline;
    }
}