using SIQuester.ViewModel.Properties;
using System;
using System.Collections.Generic;

namespace SIQuester.Model
{
    [Serializable]
    public sealed class QuestionTypesNames: Dictionary<string, string>
    {
        public QuestionTypesNames()
        {
            this[SIPackages.Core.QuestionTypes.Simple] = Resources.SimpleQuestion;
            this[SIPackages.Core.QuestionTypes.Auction] = Resources.StakeQuestion;
            this[SIPackages.Core.QuestionTypes.Cat] = Resources.SecretQuestion;
            this[SIPackages.Core.QuestionTypes.BagCat] = Resources.SuperSecretQuestion;
            this[SIPackages.Core.QuestionTypes.Sponsored] = Resources.NoRiskQuestion;
            this[""] = Resources.OtherTypeQuestion;
        }

        private QuestionTypesNames(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
        {
            
        }
    }
}
