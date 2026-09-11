using DiaryHelper.Models;

namespace DiaryHelper.Controls;

public class SegmentedLabel : Label
{
    public static readonly BindableProperty SegmentsProperty =
        BindableProperty.Create(
            nameof(Segments),
            typeof(IEnumerable<TextSegment>),
            typeof(SegmentedLabel),
            null,
            propertyChanged: OnSegmentsChanged);

    public IEnumerable<TextSegment>? Segments
    {
        get => (IEnumerable<TextSegment>?)GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    private static void OnSegmentsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is SegmentedLabel label)
        {
            label.UpdateFormattedText();
        }
    }

    private void UpdateFormattedText()
    {
        if (Segments == null || !Segments.Any())
        {
            FormattedText = null;
            return;
        }

        var formattedString = new FormattedString();

        foreach (var seg in Segments)
        {
            var span = new Span
            {
                Text = seg.Text,
                FontSize = this.FontSize,
                FontFamily = this.FontFamily
            };

            if (seg.IsCorrection)
            {
                span.TextColor = Color.FromArgb("#C2410C"); // Vibrant orange text
                span.BackgroundColor = Color.FromArgb("#FFEDD5"); // Soft warm orange background
                span.FontAttributes = FontAttributes.Bold;
                span.TextDecorations = TextDecorations.Underline;
            }
            else
            {
                span.TextColor = Color.FromArgb("#475569"); // Clean slate grey
            }

            formattedString.Spans.Add(span);
        }

        FormattedText = formattedString;
    }
}
