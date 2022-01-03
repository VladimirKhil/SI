using SImulator.ViewModel;
#if LEGACY
using System.ServiceModel;
#endif

namespace SImulator.Implementation
{
#if LEGACY
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true, ConcurrencyMode = ConcurrencyMode.Reentrant)]
#endif
    public sealed class RemoteGameUIServer: RemoteGameUI
    {
    }
}
