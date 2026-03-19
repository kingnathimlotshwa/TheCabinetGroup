using Jab;
using ShadUI;

namespace TheCabinetGroup.Injections;

[
    ServiceProviderModule, 
    Singleton<DialogManager>, 
    Singleton<ToastManager>]
public interface IUtilitiesModule
{
}