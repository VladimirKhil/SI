using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIQuester.ViewModel.PlatformSpecific;
using System.Threading.Tasks;

namespace SIQuester.ViewModel
{
    public sealed class HowToViewModel: WorkspaceViewModel
    {
        public override string Header => "Как использовать программу";

        private readonly IXpsDocumentWrapper _documentWrapper;

        public object Document { get { return _documentWrapper.GetDocument(); } }

        public HowToViewModel()
        {
            _documentWrapper = PlatformManager.Instance.GetHelp();
        }

        protected override async Task Close_Executed(object arg)
        {
            _documentWrapper.Dispose();
            await base.Close_Executed(arg);
        }
    }
}
