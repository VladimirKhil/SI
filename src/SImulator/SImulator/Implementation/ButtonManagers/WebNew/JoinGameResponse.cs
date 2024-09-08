namespace SImulator.Implementation.ButtonManagers.WebNew;

/// <summary>
/// Defines a join game response.
/// </summary>
public sealed record JoinGameResponse
{
    /// <summary>
    /// Successfull join.
    /// </summary>
    public static readonly JoinGameResponse Success = new() { IsSuccess = true };

    /// <summary>
    /// Is join successfull.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error type for failed joins.
    /// </summary>
    public JoinGameErrorType ErrorType { get; set; }

    /// <summary>
    /// Optional message.
    /// </summary>
    public string? Message { get; set; }
}
