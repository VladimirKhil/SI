namespace SICore;

/// <summary>
/// Marks messages which could be resent several times, so their processing should be idempotent.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
internal sealed class IdempotencyRequiredAttribute : Attribute { }
