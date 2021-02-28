using SICore.Connections;
using SIData;
using SIUI.ViewModel;

namespace SICore
{
    /// <summary>
    /// Зритель
    /// </summary>
    public interface IViewer : ILogic
    {
        IPlayer PlayerLogic { get; }

        IShowman ShowmanLogic { get; }

        TableInfoViewModel TInfo { get; }

        bool CanSwitchType { get; }

        /// <summary>
        /// Получение сообщений
        /// </summary>
        void ReceiveText(Message m);

        /// <summary>
        /// Печать в протокол формы
        /// </summary>
        /// <param name="text">Текст</param>
        void Print(string text);

        /// <summary>
        /// Новое состояние игры
        /// </summary>
        void Stage();

        /// <summary>
        /// Получены темы игры
        /// </summary>
        void GameThemes();

        /// <summary>
        /// Получены темы раунда
        /// </summary>
        void RoundThemes(bool print);

        /// <summary>
        /// Игрок выбрал вопрос
        /// </summary>
        void Choice();

        /// <summary>
        /// Получена часть вопроса
        /// </summary>
        void SetAtom(string[] mparams);

        /// <summary>
        /// Получена фоновая часть вопроса
        /// </summary>
        void SetSecondAtom(string[] mparams);

        void SetRight(string answer);

        void Resume();

        /// <summary>
        /// Можно жать на кнопку
        /// </summary>
        void Try();

        /// <summary>
        /// Жать уже нельзя
        /// </summary>
        void EndTry(string text);

        /// <summary>
        /// Показать табло
        /// </summary>
        void ShowTablo();

        /// <summary>
        /// Игрок получил или потерял деньги
        /// </summary>
        void Person(int playerIndex, bool isRight);

        /// <summary>
        /// Известен тип вопроса
        /// </summary>
        void QType();

        /// <summary>
        /// Завершение раунда
        /// </summary>
        void StopRound();

        /// <summary>
        /// Удалена тема
        /// </summary>
        void Out(int themeIndex);

        /// <summary>
        /// Победитель игры
        /// </summary>
        void Winner();

        /// <summary>
        /// Время вышло
        /// </summary>
        void TimeOut();

        /// <summary>
        /// Размышления в финале
        /// </summary>
        void FinalThink();

        /// <summary>
        /// Обновление изображения
        /// </summary>
        /// <param name="i"></param>
        /// <param name="path"></param>
        void UpdatePicture(Account account, string path);

        /// <summary>
        /// Попытка подключения
        /// </summary>
        void TryConnect(IConnector connector);

        void OnTextSpeed(double speed);

        void SetText(string text);

        void OnPauseChanged(bool isPaused);

        void TableLoaded();

        void PrintGreeting();
        void TextShape(string[] mparams);
        void OnTimeChanged();
        void OnTimerChanged(int timerIndex, string timerCommand, string arg, string person);
        void OnPersonFinalStake(int playerIndex);
        void OnPersonFinalAnswer(int playerIndex);
        void OnPackageLogo(string v);
        void OnPersonApellated(int playerIndex);
        void OnPersonPass(int playerIndex);
        void OnReplic(string personCode, string text);
    }
}
