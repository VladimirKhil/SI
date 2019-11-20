using SIEngine;
using SImulator.ViewModel;
using System.Collections.Generic;
using System.ServiceModel;

namespace SImulator.Implementation
{
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant)]
    internal sealed class GameHostClient: GameHost
    {
        public GameHostClient(EngineBase engine)
            : base(engine)
        {

        }
    }
}
