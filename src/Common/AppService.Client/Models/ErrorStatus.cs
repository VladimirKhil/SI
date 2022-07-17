namespace AppService.Client.Models
{
    /// <summary>
    /// Defines an application error current status.
    /// </summary>
    public enum ErrorStatus
    {
        /// <summary>
        /// The error has not been fixed yet.
        /// </summary>
        NotFixed,
        /// <summary>
        /// The error has been fixed in current application version.
        /// </summary>
        Fixed,
        /// <summary>
        /// The error can not be reproduced by the application author.
        /// </summary>
        CannotReproduce
    }
}