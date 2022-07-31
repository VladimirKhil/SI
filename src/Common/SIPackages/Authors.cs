using SIPackages.Core;
using SIPackages.Properties;
using System.Collections.Generic;

namespace SIPackages
{
    /// <summary>
    /// Defines a list of package object authors names.
    /// </summary>
    public sealed class Authors : List<string>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="Authors" /> class.
        /// </summary>
        public Authors() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Authors" /> class.
        /// </summary>
        /// <param name="collection">Initial authors names collection.</param>
        public Authors(IList<string> collection) : base(collection) { }

        /// <inheritdoc />
        public override string ToString() => $"{Resources.Authors}: {this.ToCommonString()}";
    }
}
