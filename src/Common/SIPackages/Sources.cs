using SIPackages.Core;
using SIPackages.Properties;
using System.Collections.Generic;

namespace SIPackages
{
    /// <summary>
    /// Defines a package item sources.
    /// </summary>
    public sealed class Sources : List<string>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="Sources" /> class.
        /// </summary>
        public Sources() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Sources" /> class.
        /// </summary>
        /// <param name="collection">Sources collection.</param>
        public Sources(IEnumerable<string> collection)
            : base(collection)
        {

        }

        /// <inheritdoc />
        public override string ToString() => $"{Resources.Sources}: {this.ToCommonString()}";
    }
}
