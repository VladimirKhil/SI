using SImulator.ViewModel.PlatformSpecific;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SImulator.ViewModel.Tests
{
    internal sealed class TestPackageSource : IPackageSource
    {
        public string Name => throw new NotImplementedException();

        public string Token => "";

        public Task<Stream> GetPackageAsync() => 
            Task.FromResult((Stream)File.OpenRead(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "1.siq")));
    }
}
