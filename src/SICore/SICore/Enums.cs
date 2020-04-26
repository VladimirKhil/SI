﻿using System;
using System.ComponentModel.DataAnnotations;

namespace SICore
{
    /// <summary>
    /// Игровые задачи
    /// </summary>
    public enum Tasks
    {
        /// <summary>
        /// Нет задачи
        /// </summary>
        NoTask,
        /// <summary>
        /// Двигаться дальше
        /// </summary>
        MoveNext,
        /// <summary>
        /// Начало игры
        /// </summary>
        StartGame,
        /// <summary>
        /// Объявление пакета
        /// </summary>
        Package,
        /// <summary>
        /// Объявление раунда
        /// </summary>
        Round,
        /// <summary>
        /// Выяснение того, кто начнёт раунд
        /// </summary>
        AskFirst,
        /// <summary>
        /// Ожидание выяснения того, кто начнёт раунд
        /// </summary>
        WaitFirst,
        /// <summary>
        /// Просьба выбрать вопрос
        /// </summary>
        AskToChoose,
        /// <summary>
        /// Ожидание выбора вопроса
        /// </summary>
        WaitChoose,
        /// <summary>
        /// Объявление темы
        /// </summary>
        Theme,
        /// <summary>
        /// Объявление типа вопроса
        /// </summary>
        QuestionType,
        /// <summary>
        /// Предложение жать на кнопку
        /// </summary>
        AskToTry,
        /// <summary>
        /// Ожидание нажатия на кнопку
        /// </summary>
        WaitTry,
        /// <summary>
        /// Объявление источников и комментариев вопроса
        /// </summary>
        QuestSourComm,
        /// <summary>
        /// Выяснение ответа игрока
        /// </summary>
        AskAnswer,
        AskAnswerDeferred,
        /// <summary>
        /// Ожидание ответа игрока
        /// </summary>
        WaitAnswer,
        /// <summary>
        /// Выяснение правильности ответа у ведущего
        /// </summary>
        AskRight,
        /// <summary>
        /// Ожидание выяснения правильности ответа ведущим
        /// </summary>
        WaitRight,
        /// <summary>
        /// Вывод вопроса
        /// </summary>
        PrintQue,
        /// <summary>
        /// Вывод частичного текста вопроса
        /// </summary>
        PrintPartial,
        /// <summary>
        /// Объявление Кота в мешке
        /// </summary>
        PrintCat,
        /// <summary>
        /// Выяснение, кому будет отдан Кот в мешке
        /// </summary>
        AskCat,
        /// <summary>
        /// Ожидание решения игрока об отдаче Кота в мешке
        /// </summary>
        WaitCat,
        /// <summary>
        /// Определение стоимости Кота в мешке
        /// </summary>
        AskCatCost,
        /// <summary>
        /// Ожидание решения игрока о стоимости Кота в мешке
        /// </summary>
        WaitCatCost,
        /// <summary>
        /// Объявление информации о Коте в мешке
        /// </summary>
        CatInfo,
        /// <summary>
        /// Объявление аукциона
        /// </summary>
        PrintAuct,
        /// <summary>
        /// Ожидание решения ведущего о том, кто будет следующим делать ставку
        /// </summary>
        WaitNext,
        /// <summary>
        /// Выяснение ставки следующего игрока
        /// </summary>
        AskStake,
        /// <summary>
        /// Ожидание решения игрока о своей ставке
        /// </summary>
        WaitStake,
        /// <summary>
        /// Объявление игрока, играющего аукцион
        /// </summary>
        PrintAuctPlayer,
        /// <summary>
        /// Объявление финального состава
        /// </summary>
        PrintFinal,
        /// <summary>
        /// Просьба к игроку удалить тему в финале
        /// </summary>
        AskToDelete,
        /// <summary>
        /// Ожидание решения игркоа об удалении темы в финале
        /// </summary>
        WaitDelete,
        /// <summary>
        /// Ожидание решения ведущего о том, кто будет следующим удалять тему в финале
        /// </summary>
        WaitNextToDelete,
        /// <summary>
        /// Выяснение ставки игрока в финале
        /// </summary>
        AskFinalStake,
        /// <summary>
        /// Ожидание ставки игрока в финале
        /// </summary>
        WaitFinalStake,
        /// <summary>
        /// Объявление ответа игрока в финале
        /// </summary>
        Announce,
        /// <summary>
        /// Объявление ставки игрока в финале
        /// </summary>
        AnnounceStake,
        /// <summary>
        /// Завершение игры
        /// </summary>
        EndGame,
        /// <summary>
        /// Объявление победителя игры
        /// </summary>
        Winner,
        /// <summary>
        /// Прощание с игроками
        /// </summary>
        GoodLuck,
        /// <summary>
        /// Вопрос от спонсора
        /// </summary>
        PrintSponsored,
        /// <summary>
        /// Сообщить об апелляции
        /// </summary>
        PrintApellation,
        /// <summary>
        /// Ожидание решения игроков об апелляции
        /// </summary>
        WaitApellationDecision,
        /// <summary>
        /// Принять апелляцию
        /// </summary>
        CheckApellation,
        /// <summary>
        /// Ожидаем, пока игроки напишут отчёт
        /// </summary>
        WaitReport,
        /// <summary>
        /// Автоматическая игра
        /// </summary>
        AutoGame
    }

    /// <summary>
    /// Режим лога
    /// </summary>
    public enum LogMode
    {
        Protocol,
        Log,
        Chat
    }

    /// <summary>
    /// Виды диалоговых окон
    /// </summary>
    public enum DialogModes
    {
        None,
        AnswerValidation,
        ChangeSum,
        Answer,
        CatCost,
        Stake,
        FinalStake,
        Report,
        Manage
    }

    /// <summary>
    /// Типы решений
    /// </summary>
    public enum DecisionType
    {
        /// <summary>
        /// Решение не ожидается
        /// </summary>
        None,
        /// <summary>
        /// Выбор вопроса
        /// </summary>
        QuestionChoosing,
        /// <summary>
        /// Нажатие игроком кнопки
        /// </summary>
        PlayerButtonPressing,
        /// <summary>
        /// Отдача Кота в мешке
        /// </summary>
        CatGiving,
        /// <summary>
        /// Выбор стоимости Кота
        /// </summary>
        CatCostSetting,
        /// <summary>
        /// Выставление ставки на аукционе
        /// </summary>
        AuctionStakeMaking,
        /// <summary>
        /// Удаление темы в финале
        /// </summary>
        FinalThemeDeleting,
        /// <summary>
        /// Выставление ставки в финале
        /// </summary>
        FinalStakeMaking,
        /// <summary>
        /// Нажатие на кнопку
        /// </summary>
        Pressing,
        /// <summary>
        /// Выдача ответа
        /// </summary>
        Answering,
        /// <summary>
        /// Проверка правильности ответа
        /// </summary>
        AnswerValidating,
        /// <summary>
        /// Выбор игрока, начинающего раунд
        /// </summary>
        StarterChoosing,
        /// <summary>
        /// Выбор следующего ставящего на аукционе
        /// </summary>
        NextPersonStakeMaking,
        /// <summary>
        /// Выбор следущего игрока, удаляющего тему в финале
        /// </summary>
        NextPersonFinalThemeDeleting,
        /// <summary>
        /// Решение игроков о правильности ответа
        /// </summary>
        ApellationDecision,
        /// <summary>
        /// Отправка отчёта
        /// </summary>
        Reporting
    }

    /// <summary>
    /// Стиль игры
    /// </summary>
    public enum PlayerStyle
    {
        /// <summary>
        /// Агрессивный
        /// </summary>
        [Display(Description = "PlayerStyle_Agressive")]
        Agressive,
        /// <summary>
        /// Спокойный
        /// </summary>
        [Display(Description = "PlayerStyle_Normal")]
        Normal,
        /// <summary>
        /// Осторожный
        /// </summary>
        [Display(Description = "PlayerStyle_Careful")]
        Careful
    }

    /// <summary>
    /// Состояние игрока
    /// </summary>
    public enum PlayerState
    {
        /// <summary>
        /// Обычное
        /// </summary>
        None,
        /// <summary>
        /// Выиграл кнопку
        /// </summary>
        Press,
        /// <summary>
        /// Проиграл кнопку
        /// </summary>
        Lost,
        /// <summary>
        /// Ответил верно
        /// </summary>
        Right,
        /// <summary>
        /// Ответил неверно
        /// </summary>
        Wrong,
        /// <summary>
        /// Дал ответ в финале
        /// </summary>
        HasAnswered,
        /// <summary>
        /// Спасовал
        /// </summary>
        Pass
    }

    /// <summary>
    /// Виды ставок
    /// </summary>
    public enum StakeMode
    {
        /// <summary>
        /// Номинал
        /// </summary>
        Nominal,
        /// <summary>
        /// Сумма
        /// </summary>
        Sum,
        /// <summary>
        /// Пас
        /// </summary>
        Pass,
        /// <summary>
        /// Ва-банк
        /// </summary>
        AllIn
    }

    public enum StopReason
    {
        None,
        Pause,
        Decision,
        Answer,
        Appellation,
        Move,
        Wait
    }

    /// <summary>
    /// Роли в игре
    /// </summary>
    public enum GameRole
    {
        /// <summary>
        /// Зритель
        /// </summary>
        [Display(Description = "GameRole_Viewer")]
        Viewer,
        /// <summary>
        /// Игрок
        /// </summary>
        [Display(Description = "GameRole_Player")]
        Player,
        /// <summary>
        /// Ведущий
        /// </summary>
        [Display(Description = "GameRole_Showman")]
        Showman
    }

    [Flags]
    public enum GamesFilter
    {
        NoFilter = 0,
        New = 1,
        Sport = 2,
        Tv = 4,
        NoPassword = 8,
        All = 15
    }

    public enum MessageTypes
    {
        System,
        Special,
        Replic
    }
}
