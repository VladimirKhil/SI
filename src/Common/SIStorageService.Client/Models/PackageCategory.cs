namespace SIStorageService.Client.Models
{
    /// <summary>
    /// Defines a package category.
    /// </summary>
    public sealed class PackageCategory
    {
        public int ID { get; set; }

        /// <summary>
        /// Category name.
        /// </summary>
        public string Name { get; set; }
    }
}
