using System.Threading.Tasks;

namespace SImulator.Implementation.ButtonManagers.Web;

/// <summary>
/// Defines a client which can press the button.
/// </summary>
public interface IButtonClient
{
    /// <summary>
    /// Notifies client about its button state change.
    /// </summary>
    /// <param name="state">New button state code.</param>
    Task StateChanged(int state);
}
