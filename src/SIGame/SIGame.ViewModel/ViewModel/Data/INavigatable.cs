using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIGame.ViewModel.Data
{
    public interface INavigatable
    {
        event Action<ContentBox> Navigate;

        void OnNavigatedFrom(object data);
    }
}
