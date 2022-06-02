using System.Threading.Tasks;

namespace SImulator.Implementation.ButtonManagers.Web
{
    public interface IButtonClient
    {
        Task StateChanged(int state);
    }
}
