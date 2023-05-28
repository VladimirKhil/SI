using System;

namespace SIGame.Contracts;

/// <summary>
/// Sends error messages to server.
/// </summary>
internal interface IErrorManager
{
    /// <summary>
    /// Sends error information to server.
    /// </summary>
    /// <param name="e">Error information.</param>
    /// <returns>Operation success flag.</returns>
    bool SendErrorReport(Exception e);

    /// <summary>
    /// Sends delayed error reports to server.
    /// </summary>
    void SendDelayedReports();
}
