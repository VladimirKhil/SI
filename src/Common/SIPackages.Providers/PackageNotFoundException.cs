using System;
using System.Collections.Generic;
using System.Text;

namespace SIPackages.Providers
{
    public sealed class PackageNotFoundException: Exception
    {

        public PackageNotFoundException()
        {
        }

        public PackageNotFoundException(string message)
            : base(message)
        {

        }

        public PackageNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
