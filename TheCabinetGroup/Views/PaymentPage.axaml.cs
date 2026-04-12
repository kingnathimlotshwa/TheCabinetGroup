using Avalonia.Controls;

using TheCabinetGroup.ViewModels;

namespace TheCabinetGroup.Views;

public partial class PaymentPage : UserControl
{
    public PaymentPage()
    {
        InitializeComponent();

        // CHANGED: StorageProvider must be wired up from both events because when
        // PaymentPage is declared inline in AXAML with DataContext="{Binding PaymentVm}",
        // Avalonia sets DataContext *after* AttachedToVisualTree fires, so the original
        // single-event hook always found DataContext null and StorageProvider was never set.
        // Hooking both events ensures it is assigned regardless of which fires last.
        AttachedToVisualTree += (_, _) => TryWireStorageProvider();
        DataContextChanged += (_, _) => TryWireStorageProvider();
    }

    private void TryWireStorageProvider()
    {
        if (DataContext is PaymentViewModel vm && TopLevel.GetTopLevel(this) is { } topLevel)
            vm.StorageProvider = topLevel.StorageProvider;
    }
}

