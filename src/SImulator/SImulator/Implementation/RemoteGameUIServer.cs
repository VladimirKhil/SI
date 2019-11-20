using SImulator.ViewModel;
using System.ServiceModel;

namespace SImulator.Implementation
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public sealed class RemoteGameUIServer: RemoteGameUI
    {
    }
}
