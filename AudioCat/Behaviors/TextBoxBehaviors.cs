using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AudioCat.Behaviors;

public static class TextBoxBehaviors
{
    public static readonly DependencyProperty IsDigitOnlyProperty =
        DependencyProperty.RegisterAttached("IsDigitOnly", typeof(bool), typeof(TextBoxBehaviors), new PropertyMetadata(false, OnIsDigitOnlyChanged));

    public static bool GetIsDigitOnly(DependencyObject obj) => 
        (bool)obj.GetValue(IsDigitOnlyProperty);

    public static void SetIsDigitOnly(DependencyObject obj, bool value) => 
        obj.SetValue(IsDigitOnlyProperty, value);

    private static void OnIsDigitOnlyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
    {
        if (dependencyObject is not TextBox textBox) 
            return;

        if ((bool)eventArgs.NewValue)
        {
            textBox.PreviewTextInput += OnTextBoxPreviewTextInput;
            DataObject.AddPastingHandler(textBox, OnPaste);
        }
        else
        {
            textBox.PreviewTextInput -= OnTextBoxPreviewTextInput;
            DataObject.RemovePastingHandler(textBox, OnPaste);
        }
    }

    private static void OnTextBoxPreviewTextInput(object sender, TextCompositionEventArgs eventArgs)
    {
        eventArgs.Handled = !IsTextAllowed(eventArgs.Text);
    }

    private static void OnPaste(object sender, DataObjectPastingEventArgs eventArgs)
    {
        if (!eventArgs.DataObject.GetDataPresent(DataFormats.Text) || !IsTextAllowed((string)eventArgs.DataObject.GetData(DataFormats.Text)!)) 
            eventArgs.CancelCommand();
    }

    private static bool IsTextAllowed(string text)
    {
        foreach (var ch in text)
        {
            if (!char.IsDigit(ch))
                return false;
        }

        return true;
    }
}