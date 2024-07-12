using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AudioCat.Behaviors;

public static class TextBoxBehaviors
{
    public static readonly DependencyProperty IsDigitOnlyProperty =
        DependencyProperty.RegisterAttached("IsDigitOnly", typeof(bool), typeof(TextBoxBehaviors), new PropertyMetadata(false, OnIsDigitOnlyChanged));

    public static bool GetIsDigitOnly(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsDigitOnlyProperty);
    }

    public static void SetIsDigitOnly(DependencyObject obj, bool value)
    {
        obj.SetValue(IsDigitOnlyProperty, value);
    }

    private static void OnIsDigitOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox)
        {
            if ((bool)e.NewValue)
            {
                textBox.PreviewTextInput += TextBox_PreviewTextInput;
                DataObject.AddPastingHandler(textBox, OnPaste);
            }
            else
            {
                textBox.PreviewTextInput -= TextBox_PreviewTextInput;
                DataObject.RemovePastingHandler(textBox, OnPaste);
            }
        }
    }

    private static void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !IsTextAllowed(e.Text);
    }

    private static void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(DataFormats.Text))
        {
            string text = (string)e.DataObject.GetData(DataFormats.Text);
            if (!IsTextAllowed(text))
            {
                e.CancelCommand();
            }
        }
        else
        {
            e.CancelCommand();
        }
    }

    private static bool IsTextAllowed(string text)
    {
        Regex regex = new Regex("[^0-9]+"); // Regex that matches non-digit characters
        return !regex.IsMatch(text);
    }
}
