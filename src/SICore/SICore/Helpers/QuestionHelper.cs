using SIPackages;
using SIUI.Model;

namespace SICore.Helpers;

internal static class QuestionHelper
{
    internal const string InvalidThemeName = null;

    internal static bool IsActive(this QuestionInfo question) => question.Price != Question.InvalidPrice;
}
