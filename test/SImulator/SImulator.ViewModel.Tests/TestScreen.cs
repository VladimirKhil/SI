using SImulator.ViewModel.PlatformSpecific;
using System;
using System.Collections.Generic;
using System.Text;

namespace SImulator.ViewModel.Tests
{
    internal sealed class TestScreen : IScreen
    {
        public string Name => throw new NotImplementedException();

        public bool IsRemote => false;
    }
}
