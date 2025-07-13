namespace SICore.Models;

[Flags]
internal enum InformStages
{
    None = 0,
    RoundNames = 1,
    RoundContent = 2,
    RoundThemesNames = 4,
    RoundThemesComments = 8,
    Table = 16,
    Theme = 32,
    Layout = 64,
    ContentShape = 128,
}
