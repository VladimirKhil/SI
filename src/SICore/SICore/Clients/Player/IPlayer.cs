namespace SICore
{
    /// <summary>
    /// Игрок
    /// </summary>
    public interface IPlayer : IPerson
    {
        /// <summary>
        /// Окончание размышлений
        /// </summary>
        void EndThink();

        /// <summary>
        /// Надо отвечать
        /// </summary>
        void Answer();

        /// <summary>
        /// Проверка правильности ответа
        /// </summary>
        void IsRight(bool voteForRight);

        /// <summary>
        /// Подключён
        /// </summary>
        void Connected(string name);

        void Report();

        void Clear();
    }
}
