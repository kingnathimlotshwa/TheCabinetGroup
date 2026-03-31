using System;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

using ShadUI;

namespace TheCabinetGroup.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    // Shared busy/error state — bind to these in any child View.
    private bool _isBusy;
    private string _errorMessage = string.Empty;
    private readonly ToastManager? _toast;

    public bool IsBusy
    {
        get => _isBusy;
        protected set => SetProperty(ref _isBusy, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        protected set => SetProperty(ref _errorMessage, value);
    }

    // Parameterless constructor kept for design-time and tests.
    protected ViewModelBase() { }

    protected ViewModelBase(ToastManager toast)
    {
        _toast = toast;
    }

    protected void ShowError(string message, int delay = 2)
    {
        ErrorMessage = message;
        _toast?.CreateToast(message).WithDelay(delay).ShowError();
    }

    protected void ShowSuccess(string message, int delay = 2)
        => _toast?.CreateToast(message).WithDelay(delay).ShowSuccess();

    protected void ShowWarning(string message, int delay = 2)
        => _toast?.CreateToast(message).WithDelay(delay).ShowWarning();

    protected void ShowInfo(string message, int delay = 2)
        => _toast?.CreateToast(message).WithDelay(delay).ShowInfo();

    /// <summary>
    /// Wraps an async operation: clears ErrorMessage, sets IsBusy,
    /// and catches both AppwriteException and general Exception,
    /// showing a toast for each. Pass the ToastManager from the child VM.
    /// </summary>
    protected async Task RunSafeAsync(Func<Task> action)
    {
        ErrorMessage = string.Empty;
        IsBusy = true;
        try
        {
            await action();
        }
        catch (Appwrite.AppwriteException ex)
        {
            ShowError(ex.Message ?? "An Appwrite error occurred.");
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
