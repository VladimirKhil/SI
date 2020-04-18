using SIUI.Model;

namespace SICore.Utils
{
    internal static class QuestionHelper
    {
        internal const int InvalidQuestionPrice = -1;

        internal const string InvalidThemeName = null;
        internal static bool IsActive(this QuestionInfo question) => question.Price > InvalidQuestionPrice;
    }
}
