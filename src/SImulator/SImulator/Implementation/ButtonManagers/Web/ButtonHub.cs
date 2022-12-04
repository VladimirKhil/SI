using Microsoft.AspNetCore.SignalR;

namespace SImulator.Implementation.ButtonManagers.Web;

/// <summary>
/// Provides a SignalR Hub for web clients.
/// </summary>
public sealed class ButtonHub : Hub<IButtonClient>
{
    private readonly IButtonProcessor _buttonProcessor;

    /// <summary>
    /// Initializes a new instance of <see cref="ButtonHub" /> class.
    /// </summary>
    /// <param name="buttonProcessor">Button processor.</param>
    public ButtonHub(IButtonProcessor buttonProcessor) => _buttonProcessor = buttonProcessor;

    /// <summary>
    /// Presses the button.
    /// </summary>
    /// <param name="token">Player token.</param>
    /// <returns>Pressed player name.</returns>
    public PressResponse Press(string token) => _buttonProcessor.Press(token);
}
