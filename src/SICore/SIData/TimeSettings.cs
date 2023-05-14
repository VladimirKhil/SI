using System.ComponentModel;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SIData;

/// <summary>
/// Настройки временных интервалов игры
/// </summary>
[DataContract]
public sealed class TimeSettings
{
    [XmlIgnore]
    [IgnoreDataMember]
    public Dictionary<TimeSettingsTypes, int> All { get; private set; } = new();

    /// <summary>
    /// Время для выбора игроком вопроса
    /// </summary>
    [XmlAttribute]
    [DefaultValue(30)]
    [DataMember]
    public int TimeForChoosingQuestion
    {
        get { return All[TimeSettingsTypes.ChoosingQuestion]; }
        set { All[TimeSettingsTypes.ChoosingQuestion] = value; }
    }

    /// <summary>
    /// Время для размышления над вопросом
    /// </summary>
    [XmlAttribute]
    [DefaultValue(5)]
    [DataMember]
    public int TimeForThinkingOnQuestion
    {
        get { return All[TimeSettingsTypes.ThinkingOnQuestion]; }
        set { All[TimeSettingsTypes.ThinkingOnQuestion] = value; }
    }

    /// <summary>
    /// Время на ввод ответа после нажатия кнопки
    /// </summary>
    [XmlAttribute]
    [DefaultValue(25)]
    [DataMember]
    public int TimeForPrintingAnswer
    {
        get { return All[TimeSettingsTypes.PrintingAnswer]; }
        set { All[TimeSettingsTypes.PrintingAnswer] = value; }
    }

    /// <summary>
    /// Время на размышление на отдачу Вопроса с секретом
    /// </summary>
    [XmlAttribute]
    [DefaultValue(30)]
    [DataMember]
    public int TimeForGivingACat
    {
        get { return All[TimeSettingsTypes.GivingCat]; }
        set { All[TimeSettingsTypes.GivingCat] = value; }
    }

    /// <summary>
    /// Время на размышление над ставкой в Вопросе со ставкой и в финале
    /// </summary>
    [XmlAttribute]
    [DefaultValue(30)]
    [DataMember]
    public int TimeForMakingStake
    {
        get { return All[TimeSettingsTypes.MakingStake]; }
        set { All[TimeSettingsTypes.MakingStake] = value; }
    }

    /// <summary>
    /// Время на размышление над спецвопросом
    /// </summary>
    [XmlAttribute]
    [DefaultValue(25)]
    [DataMember]
    public int TimeForThinkingOnSpecial
    {
        get { return All[TimeSettingsTypes.ThinkingOnSpecial]; }
        set { All[TimeSettingsTypes.ThinkingOnSpecial] = value; }
    }

    public const int DefaultTimeOfRound = 3600;

    /// <summary>
    /// Время раунда
    /// </summary>
    [XmlAttribute]
    [DefaultValue(DefaultTimeOfRound)]
    [DataMember]
    public int TimeOfRound
    {
        get { return All[TimeSettingsTypes.Round]; }
        set { All[TimeSettingsTypes.Round] = value; }
    }

    /// <summary>
    /// Время на выбор темы в финальном раунде
    /// </summary>
    [XmlAttribute]
    [DefaultValue(30)]
    [DataMember]
    public int TimeForChoosingFinalTheme
    {
        get { return All[TimeSettingsTypes.ChoosingFinalTheme]; }
        set { All[TimeSettingsTypes.ChoosingFinalTheme] = value; }
    }

    /// <summary>
    /// Время на обдумывание в финале
    /// </summary>
    [XmlAttribute]
    [DefaultValue(45)]
    [DataMember]
    public int TimeForFinalThinking
    {
        get { return All[TimeSettingsTypes.FinalThinking]; }
        set { All[TimeSettingsTypes.FinalThinking] = value; }
    }

    /// <summary>
    /// Время на принятие решений ведущим
    /// </summary>
    [XmlAttribute]
    [DefaultValue(30)]
    [DataMember]
    public int TimeForShowmanDecisions
    {
        get 
        {
            if (All.TryGetValue(TimeSettingsTypes.ShowmanDecisions, out int value))
                return value;

            return 30;
        }
        set { All[TimeSettingsTypes.ShowmanDecisions] = value; }
    }

    /// <summary>
    /// Время на просмотр правильного ответа
    /// </summary>
    [XmlAttribute]
    [DefaultValue(2)]
    [DataMember]
    public int TimeForRightAnswer
    {
        get
        {
            if (All.TryGetValue(TimeSettingsTypes.RightAnswer, out int value))
            {
                return value;
            }

            return 2;
        }
        set { All[TimeSettingsTypes.RightAnswer] = value; }
    }

    /// <summary>
    /// Время на ожидание при показе мультимедиа
    /// </summary>
    [XmlAttribute]
    [DefaultValue(0)]
    [DataMember]
    public int TimeForMediaDelay
    {
        get
        {
            if (All.TryGetValue(TimeSettingsTypes.MediaDelay, out int value))
            {
                return value;
            }

            return 0;
        }
        set { All[TimeSettingsTypes.MediaDelay] = value; }
    }
    
    [DefaultValue(3)]
    [DataMember]
    /// <summary>
    /// Время на блокировку игровой кнопки
    /// </summary>
    public int TimeForBlockingButton { get; } = 3;

    public TimeSettings()
    {
        All[TimeSettingsTypes.ChoosingQuestion] = 30;
        All[TimeSettingsTypes.ThinkingOnQuestion] = 5;
        All[TimeSettingsTypes.PrintingAnswer] = 25;
        All[TimeSettingsTypes.GivingCat] = 30;
        All[TimeSettingsTypes.MakingStake] = 30;
        All[TimeSettingsTypes.ThinkingOnSpecial] = 25;
        All[TimeSettingsTypes.Round] = DefaultTimeOfRound;
        All[TimeSettingsTypes.ChoosingFinalTheme] = 30;
        All[TimeSettingsTypes.FinalThinking] = 45;
        All[TimeSettingsTypes.ShowmanDecisions] = 30;
        All[TimeSettingsTypes.RightAnswer] = 2;
        All[TimeSettingsTypes.MediaDelay] = 0;
    }

    [OnDeserializing]
    internal void OnDeserializing(StreamingContext context)
    {
        All = new Dictionary<TimeSettingsTypes, int>();
    }

    public TimeSettings Clone()
    {
        var clone = new TimeSettings();
        foreach (var item in All)
        {
            clone.All[item.Key] = item.Value;
        }

        return clone;
    }
}
