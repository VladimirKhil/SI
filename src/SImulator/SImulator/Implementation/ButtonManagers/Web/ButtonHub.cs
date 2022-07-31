using Microsoft.AspNetCore.SignalR;

namespace SImulator.Implementation.ButtonManagers.Web
{
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
        public ButtonHub(IButtonProcessor buttonProcessor)
        {
            _buttonProcessor = buttonProcessor;
        }

        /// <summary>
        /// Presses the button.
        /// </summary>
        /// <returns>Pressed player name.</returns>
        public string Press() => _buttonProcessor.Press(Context.ConnectionId);
    }
}
