using SICore;
using SIGame.ViewModel.Properties;
using SIUI.ViewModel;
using System.Windows.Input;

namespace SIGame.ViewModel;

public sealed class ThemeSettingsViewModel : ViewModel<ThemeSettings>
{
    public SettingsViewModel SIUISettings { get; private set; }

    /// <summary>
    /// Выбрать логотип
    /// </summary>
    public ICommand SelectLogo { get; private set; }

    /// <summary>
    /// Очистить логотип
    /// </summary>
    public ICommand ClearLogo { get; private set; }

    /// <summary>
    /// Выбрать фон
    /// </summary>
    public ICommand SelectCustomBackground { get; private set; }

    /// <summary>
    /// Очистить фон
    /// </summary>
    public ICommand ClearCustomBackground { get; private set; }

    /// <summary>
    /// Выбрать фон
    /// </summary>
    public ICommand SelectCustomMainBackground { get; private set; }

    /// <summary>
    /// Очистить фон
    /// </summary>
    public ICommand ClearCustomMainBackground { get; private set; }

    public ICommand SelectColor { get; private set; }

    /// <summary>
    /// Выбрать мелодию
    /// </summary>
    public ICommand SelectSound { get; private set; }

    /// <summary>
    /// Очистить мелодию
    /// </summary>
    public ICommand ClearSound { get; private set; }

    public ThemeSettingsViewModel(ThemeSettings settings)
        : base(settings)
    {
        SIUISettings = new SettingsViewModel(settings.UISettings);
    }

    protected override void Initialize()
    {
        base.Initialize();

        SelectColor = new CustomCommand(SelectColor_Executed);
        SelectLogo = new CustomCommand(SelectLogo_Executed);
        ClearLogo = new CustomCommand(ClearLogo_Executed);
        SelectCustomBackground = new CustomCommand(SelectCustomBackground_Executed);
        ClearCustomBackground = new CustomCommand(ClearCustomBackground_Executed);
        SelectCustomMainBackground = new CustomCommand(SelectCustomMainBackground_Executed);
        ClearCustomMainBackground = new CustomCommand(ClearCustomMainBackground_Executed);
        SelectSound = new CustomCommand(SelectSound_Executed);
        ClearSound = new CustomCommand(ClearSound_Executed);
    }

    private void SelectLogo_Executed(object arg)
    {
        var fileName = PlatformSpecific.PlatformManager.Instance.SelectLogo();

        if (fileName != null)
        {
            var fileLength = new FileInfo(fileName).Length;
            if (fileLength > 250_000)
                PlatformSpecific.PlatformManager.Instance.ShowMessage(Resources.FileTooLarge, PlatformSpecific.MessageType.Warning);
            else
                _model.UISettings.LogoUri = fileName;
        }
    }

    private void ClearLogo_Executed(object arg)
    {
        _model.UISettings.LogoUri = null;
    }

    private void SelectSound_Executed(object arg)
    {
        var fileName = PlatformSpecific.PlatformManager.Instance.SelectSound();

        if (fileName != null)
        {
            var fileLength = new FileInfo(fileName).Length;
            if (fileLength > 5_000_000)
            {
                PlatformSpecific.PlatformManager.Instance.ShowMessage(Resources.FileTooLarge, PlatformSpecific.MessageType.Warning);
            }
            else
            {
                _model.GetType().GetProperty(arg.ToString()).SetValue(_model, fileName);
            }
        }
    }

    private void ClearSound_Executed(object arg) => _model.GetType().GetProperty(arg.ToString()).SetValue(_model, null);

    private void SelectColor_Executed(object arg)
    {
        var color = PlatformSpecific.PlatformManager.Instance.SelectColor();

        if (color != null)
        {
            if (Convert.ToInt32(arg) == 0)
                _model.UISettings.TableColorString = color;
            else
                _model.UISettings.TableBackColorString = color;
        }
    }

    private void SelectCustomBackground_Executed(object arg)
    {
        var fileName = PlatformSpecific.PlatformManager.Instance.SelectStudiaBackground();
        if (fileName != null)
        {
            var fileLength = new FileInfo(fileName).Length;
            if (fileLength > 1500000)
                PlatformSpecific.PlatformManager.Instance.ShowMessage(Resources.FileTooLarge, PlatformSpecific.MessageType.Warning);
            else
                _model.CustomBackgroundUri = fileName;
        }
    }

    private void ClearCustomBackground_Executed(object arg)
    {
        _model.CustomBackgroundUri = null;
    }

    private void SelectCustomMainBackground_Executed(object arg)
    {
        var fileName = PlatformSpecific.PlatformManager.Instance.SelectMainBackground();
        if (fileName != null)
        {
            var fileLength = new FileInfo(fileName).Length;
            if (fileLength > 1_500_000)
                PlatformSpecific.PlatformManager.Instance.ShowMessage(Resources.FileTooLarge, PlatformSpecific.MessageType.Warning);
            else
                _model.CustomMainBackgroundUri = fileName;
        }
    }

    private void ClearCustomMainBackground_Executed(object arg)
    {
        _model.CustomMainBackgroundUri = null;
    }

    internal void Reset()
    {
        SIUISettings.Reset();

        _model.MaximumTableTextLength = ThemeSettings.DefaultMaximumTableTextLength;
        _model.MaximumReplicTextLength = ThemeSettings.DefaultMaximumReplicTextLength;
    }
}
