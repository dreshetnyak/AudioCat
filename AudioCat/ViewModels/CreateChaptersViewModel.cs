using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AudioCat.ViewModels;

internal sealed class CreateChaptersViewModel : INotifyPropertyChanged
{

    #region INotifyPropertyChanged Implementation
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    #endregion
}