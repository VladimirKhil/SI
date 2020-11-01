namespace SICore
{
    /// <summary>
    /// Общий интерфейс для игрока и ведущего
    /// </summary>
    public interface IPerson : IViewer
    {
        void OnInitialized();

        // Далее - для устной игры

        /// <summary>
        /// Выбор темы и вопроса
        /// </summary>
        void ChooseQuest();

        /// <summary>
        /// Отдать вопрос с секретом
        /// </summary>
        void Cat();

        /// <summary>
        /// Нужно сделать ставку
        /// </summary>
        void Stake();

        /// <summary>
        /// Убирает финальную тему
        /// </summary>
        void ChooseFinalTheme();

        /// <summary>
        /// Ставка в финале
        /// </summary>
        void FinalStake();

        /// <summary>
        /// Определение стоимости Вопроса с секретом
        /// </summary>
        void CatCost();

        void Table();

        void FinalThemes();
    }
}
