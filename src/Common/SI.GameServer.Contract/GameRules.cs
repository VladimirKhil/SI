using System;

namespace SI.GameServer.Contract
{
    [Flags]
    public enum GameRules
    {
        None = 0,
        FalseStart = 1,
        Oral = 2,
        IgnoreWrong = 4
    }
}
