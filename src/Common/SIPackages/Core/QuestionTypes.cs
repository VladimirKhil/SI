namespace SIPackages.Core
{
    /// <summary>
    /// Provides well-known question types.
    /// </summary>
    public static class QuestionTypes
    {
        /// <summary>
        /// Simple question type.
        /// </summary>
        public const string Simple = "simple";

        /// <summary>
        /// Stake question type.
        /// </summary>
        public const string Auction = "auction";

        /// <summary>
        /// Secret question type.
        /// </summary>
        public const string Cat = "cat";

        /// <summary>
        /// Extended secret question type.
        /// </summary>
        public const string BagCat = "bagcat";

        /// <summary>
        /// No-risk question.
        /// </summary>
        public const string Sponsored = "sponsored";

        /// <summary>
        /// Multiple choice question.
        /// </summary>
        public const string Choice = "choice";
    }
}
