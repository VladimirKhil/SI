using SIPackages.Core;
using System.Diagnostics;

namespace SIPackages;

/// <summary>
/// Defines a question type.
/// </summary>
[Obsolete]
internal sealed class QuestionType : Named
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly List<QuestionTypeParam> _parameters = new();

    /// <summary>
    /// Type parameters specifying this type.
    /// </summary>
    internal List<QuestionTypeParam> Params => _parameters;

    /// <summary>
    /// Initializes a new instance of <see cref="QuestionType" /> class.
    /// </summary>
    public QuestionType() : base(QuestionTypes.Simple) { }

    /// <summary>
    /// Gets or sets type parameter value by parameter name.
    /// </summary>
    /// <param name="name">Parameter name.</param>
    /// <returns>Parameter value.</returns>
    public string this[string name]
    {
        get
        {
            var item = _parameters.FirstOrDefault(qtp => qtp.Name == name);
            return item != null ? item.Value : "";
        }
        set
        {
            var item = _parameters.FirstOrDefault(qtp => qtp.Name == name);

            if (item != null)
            {
                item.Value = value;
            }
            else
            {
                _parameters.Add(new QuestionTypeParam { Name = name, Value = value });
            }
        }
    }
}
