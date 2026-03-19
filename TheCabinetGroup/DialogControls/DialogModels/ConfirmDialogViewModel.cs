using CommunityToolkit.Mvvm.ComponentModel;
using TheCabinetGroup.ViewModels;

namespace TheCabinetGroup.DialogControls.DialogModels;

public partial class ConfirmDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _message = string.Empty;

    public ConfirmDialogViewModel(string title, string message)
    {
        Title = title;
        Message = message;
    }   
}