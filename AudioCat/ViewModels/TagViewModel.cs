using AudioCat.Models;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AudioCat.ViewModels;

public interface IMediaTagViewModel : IMediaTag, IEnableCapable
{
    public new string Name { get; set; }
    public new string Value { get; set; }
}

[DebuggerDisplay("{Name,nq}: {Value}; IsEnabled: {IsEnabled,nq}")]
public sealed class TagViewModel : IMediaTagViewModel, INotifyPropertyChanged
{
    #region Backing Fields
    private bool _isEnabled = true;
    private string _name = "";
    private string _value = "";
    #endregion

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (value == _isEnabled)
                return;
            _isEnabled = value;
            OnPropertyChanged();
        }
    }
    public string Name
    {
        get => _name;
        set
        {
            if (value == _name)
                return;
            _name = value;
            OnPropertyChanged();
        }
    }
    public string Value
    {
        get => _value;
        set
        {
            if (value == _value)
                return;
            _value = value;
            OnPropertyChanged();
        }
    }

    public static IMediaTagViewModel CreateFrom(IMediaTag tag) => new TagViewModel { Name = tag.Name, Value = tag.Value };
    public static IMediaTagViewModel Copy(IMediaTagViewModel tag) => new TagViewModel { IsEnabled = tag.IsEnabled, Name = tag.Name, Value = tag.Value };

    #region INotifyPropertyChanged Implementation
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    #endregion
}