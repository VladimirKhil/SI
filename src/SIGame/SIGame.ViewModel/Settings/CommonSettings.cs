using SIData;
using SIGame.ViewModel;
using SIStatisticsService.Contract.Models;
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace SIGame;

/// <summary>
/// Defines persistent application state.
/// </summary>
public sealed class CommonSettings
{
    /// <summary>
    /// Имя игры
    /// </summary>
    public const string AppName = "SIGame";

    /// <summary>
    /// Имя производителя (англ.)
    /// </summary>
    public const string ManufacturerEn = "Khil-soft";
    /// <summary>
    /// Имя игры (англ.)
    /// </summary>
    public const string AppNameEn = "SIGame";

    public const string LogsFolderName = "Logs";

    internal const string OnlineGameUrl = "https://vladimirkhil.com/si/online/?gameId=";

    /// <summary>
    /// Экземпляр общих настроек
    /// </summary>
    public static CommonSettings Default { get; set; }

    /// <summary>
    /// Зарегистрированные учётные записи пользователей
    /// </summary>
    public List<HumanAccount> Humans2 { get; set; }

    /// <summary>
    /// Зарегистрированные компьютерные игроки
    /// </summary>
    public List<ComputerAccount> CompPlayers2 { get; set; }

    /// <summary>
    /// Лучшие игроки
    /// </summary>
    public ObservableCollection<BestPlayer> BestPlayers { get; set; }

    /// <summary>
    /// Компьютерные ведущие
    /// </summary>
    public List<ComputerAccount> CompShowmans2 { get; set; }

    /// <summary>
    /// Отложенные отчёты об ошибках
    /// </summary>
    public ErrorInfoList DelayedErrorsNew { get; set; }

    public CommonSettings()
    {
        Humans2 = new List<HumanAccount>();
        CompPlayers2 = new List<ComputerAccount>();
        CompShowmans2 = new List<ComputerAccount>();
        BestPlayers = new ObservableCollection<BestPlayer>();
        DelayedErrorsNew = new ErrorInfoList();
    }

    public void Save(Stream stream, XmlSerializer? serializer = null)
    {
        serializer ??= new XmlSerializer(typeof(CommonSettings));
        serializer.Serialize(stream, this);
    }

    public static CommonSettings Load(Stream stream)
    {
        var serializer = new XmlSerializer(typeof(CommonSettings));
        return (CommonSettings?)serializer.Deserialize(stream) ?? new CommonSettings();
    }

    internal void LoadFrom(Stream stream)
    {
        var settings = Load(stream);

        foreach (var item in settings.Humans2)
        {
            var human = Humans2.FirstOrDefault(h => h.Name == item.Name);
            if (human == null)
            {
                Humans2.Add(item);
            }
            else if (human.CanBeDeleted)
            {
                human.IsMale = item.IsMale;
                human.Picture = item.Picture;
            }
        }

        foreach (var item in settings.CompPlayers2)
        {
            var comp = CompPlayers2.FirstOrDefault(c => c.Name == item.Name);
            if (comp == null)
            {
                CompPlayers2.Add(item);
            }
            else if (comp.CanBeDeleted)
            {
                comp.LoadInfo(item);
            }
        }
    }
}
