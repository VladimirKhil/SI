using SIPackages.Core;
using SIQuester.ViewModel.Properties;
using System.Runtime.Serialization;

namespace SIQuester.Model;

/// <summary>
/// Defines a localized collection of well-known question types names.
/// </summary>
[Serializable]
public sealed class QuestionTypesNamesNew : Dictionary<string, string>
{
    public QuestionTypesNamesNew()
    {
        this[QuestionTypes.Simple] = Resources.SimpleQuestion;
        this[QuestionTypes.Stake] = Resources.StakeQuestion;
        this[QuestionTypes.Secret] = Resources.SecretQuestion;
        this[QuestionTypes.SecretPublicPrice] = Resources.SecretPublicPrice;
        this[QuestionTypes.SecretNoQuestion] = Resources.SecretNoQuestion;
        this[QuestionTypes.NoRisk] = Resources.NoRiskQuestion;
        //this[QuestionTypes.StakeAll] = Resources.StakeAllQuestion;
        this[""] = Resources.OtherTypeQuestion;
    }

    private QuestionTypesNamesNew(SerializationInfo serializationInfo, StreamingContext streamingContext) { }
}
