using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SImulator.Implementation.ButtonManagers.WebNew;

/// <summary>
/// Defines game rules filter.
/// </summary>
[Flags]
public enum GameRules
{
    None = 0,
    FalseStart = 1,
    Oral = 2,
    IgnoreWrong = 4,
}
