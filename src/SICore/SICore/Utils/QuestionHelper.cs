using SIPackages;
using SIUI.Model;

namespace SICore.Utils;

internal static class QuestionHelper
{
    internal const string InvalidThemeName = null;

    internal static bool IsActive(this QuestionInfo question) => question.Price != Question.InvalidPrice;
}
