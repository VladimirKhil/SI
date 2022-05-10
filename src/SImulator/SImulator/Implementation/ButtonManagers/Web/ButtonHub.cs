using Microsoft.AspNetCore.SignalR;

namespace SImulator.Implementation.ButtonManagers.Web
{
    /// <summary>
    /// Provides a SignalR Hub for web clients.
    /// </summary>
    public sealed class ButtonHub : Hub<IButtonClient>
    {
        private readonly IButtonProcessor _buttonProcessor;

        public ButtonHub(IButtonProcessor buttonProcessor)
        {
            _buttonProcessor = buttonProcessor;
        }

        /// <summary>
        /// Press the button.
        /// </summary>
        /// <returns>Pressed player name.</returns>
        public string Press() => _buttonProcessor.Press(Context.ConnectionId);
    }
}
