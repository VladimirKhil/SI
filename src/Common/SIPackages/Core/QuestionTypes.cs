namespace SIPackages.Core
{
    public static class QuestionTypes
    {
        /// <summary>
        /// Простой вопрос
        /// </summary>
        public const string Simple = "simple";

        /// <summary>
        /// Вопрос со ставкой
        /// </summary>
        public const string Auction = "auction";

        /// <summary>
        /// Вопрос с секретом
        /// </summary>
        public const string Cat = "cat";

        /// <summary>
        /// Обобщённый Вопрос с секретом
        /// </summary>
        public const string BagCat = "bagcat";

        /// <summary>
        /// Вопрос без риска
        /// </summary>
        public const string Sponsored = "sponsored";
    }
}
