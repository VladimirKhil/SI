using SIPackages.Core;
using SIQuester.ViewModel.Properties;
using System.Runtime.Serialization;

namespace SIQuester.Model;

/// <summary>
/// Defines a localized collection of well-known question types names.
/// </summary>
[Serializable]
public sealed class QuestionTypesNames : Dictionary<string, string>
{
    public QuestionTypesNames()
    {
        this[QuestionTypes.Simple] = Resources.SimpleQuestion;
        this[QuestionTypes.Auction] = Resources.StakeQuestion;
        this[QuestionTypes.Cat] = Resources.SecretQuestion;
        this[QuestionTypes.BagCat] = Resources.SuperSecretQuestion;
        this[QuestionTypes.Sponsored] = Resources.NoRiskQuestion;
        this[""] = Resources.OtherTypeQuestion;
    }

    private QuestionTypesNames(SerializationInfo serializationInfo, StreamingContext streamingContext) { }
}
