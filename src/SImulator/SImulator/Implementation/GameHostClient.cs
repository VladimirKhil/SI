using SIEngine;
using SImulator.ViewModel;
#if LEGACY
using System.ServiceModel;
#endif

namespace SImulator.Implementation
{
#if LEGACY
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant)]
#endif
    internal sealed class GameHostClient: GameHost
    {
        public GameHostClient(EngineBase engine)
            : base(engine)
        {

        }
    }
}
