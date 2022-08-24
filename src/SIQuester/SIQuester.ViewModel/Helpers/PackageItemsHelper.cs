using SIPackages;

namespace SIQuester.ViewModel.Helpers
{
    /// <summary>
    /// Provides helper functions for package items.
    /// </summary>
    internal static class PackageItemsHelper
    {
        /// <summary>
        /// Creates a question with provided price.
        /// </summary>
        /// <param name="price">Question price.</param>
        internal static Question CreateQuestion(int price)
        {
            var question = new Question { Price = price };

            var atom = new Atom();
            question.Scenario.Add(atom);
            question.Right.Add("");

            return question;
        }
    }
}
