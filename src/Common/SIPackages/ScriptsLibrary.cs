using SIPackages.Core;

namespace SIPackages;

/// <summary>
/// Contains predefined question scripts.
/// </summary>
public static class ScriptsLibrary
{
    private static readonly Dictionary<string, Script> _scripts;

    /// <summary>
    /// Predefined question scripts.
    /// </summary>
    public static IReadOnlyDictionary<string, Script> Scripts => _scripts;

    static ScriptsLibrary() => _scripts = new Dictionary<string, Script>
    {
        [QuestionTypes.Simple] = CreateSimpleScript(),
        [QuestionTypes.Stake] = CreateStakeScript(),
        [QuestionTypes.StakeAll] = CreateStakeAllScript(),
        [QuestionTypes.Secret] = CreateSecretScript(),
        [QuestionTypes.SecretPublicPrice] = CreateSecretPublicPriceScript(),
        [QuestionTypes.SecretNoQuestion] = CreateSecretNoQuestionScript(),
        [QuestionTypes.NoRisk] = CreateNoRiskScript()
    };

    private static Script CreateSimpleScript()
    {
        var simpleScript = new Script();

        simpleScript.Steps.Add(CreateSetAnswerTypeStep());
        simpleScript.Steps.Add(CreateQuestionStep());
        simpleScript.Steps.Add(CreateAskAnswerStep());
        simpleScript.Steps.Add(CreateAnswerStep());

        return simpleScript;
    }

    private static Script CreateStakeScript()
    {
        var stakeScript = new Script();

        stakeScript.Steps.Add(CreateSetAnswerTypeStep());
        stakeScript.Steps.Add(CreateSetAnswererStep());
        stakeScript.Steps.Add(CreateQuestionStep());
        stakeScript.Steps.Add(CreateAskAnswerStep(StepParameterValues.AskAnswerMode_Direct));
        stakeScript.Steps.Add(CreateAnswerStep());

        return stakeScript;
    }

    private static Script CreateStakeAllScript()
    {
        var stakeScript = new Script();

        stakeScript.Steps.Add(CreateSetAnswerTypeStep());
        stakeScript.Steps.Add(CreateSetAnswererAllStep());
        stakeScript.Steps.Add(CreateQuestionStep());
        stakeScript.Steps.Add(CreateAskAnswerStep(StepParameterValues.AskAnswerMode_Direct));
        stakeScript.Steps.Add(CreateAnswerStep());

        return stakeScript;
    }

    private static Script CreateSecretScript()
    {
        var secretScript = new Script();

        secretScript.Steps.Add(CreateSetAnswerTypeStep());
        secretScript.Steps.Add(CreateSetAnswererSecretStep());
        secretScript.Steps.Add(CreateSetThemeStep());
        secretScript.Steps.Add(CreateAnnouncePriceSecretStep());
        secretScript.Steps.Add(CreateSetPriceSecretStep());
        secretScript.Steps.Add(CreateQuestionStep());
        secretScript.Steps.Add(CreateAskAnswerStep(StepParameterValues.AskAnswerMode_Direct));
        secretScript.Steps.Add(CreateAnswerStep());

        return secretScript;
    }

    private static Script CreateSecretPublicPriceScript()
    {
        var secretScript = new Script();

        secretScript.Steps.Add(CreateSetAnswerTypeStep());
        secretScript.Steps.Add(CreateSetThemeStep());
        secretScript.Steps.Add(CreateAnnouncePriceSecretStep());
        secretScript.Steps.Add(CreateSetAnswererSecretStep());
        secretScript.Steps.Add(CreateSetPriceSecretStep());
        secretScript.Steps.Add(CreateQuestionStep());
        secretScript.Steps.Add(CreateAskAnswerStep(StepParameterValues.AskAnswerMode_Direct));
        secretScript.Steps.Add(CreateAnswerStep());

        return secretScript;
    }

    private static Script CreateSecretNoQuestionScript()
    {
        var secretScript = new Script();

        secretScript.Steps.Add(CreateSetAnswererSecretStep());
        secretScript.Steps.Add(CreateSetThemeStep());
        secretScript.Steps.Add(CreateAnnouncePriceSecretStep());
        secretScript.Steps.Add(CreateSetPriceSecretStep());
        secretScript.Steps.Add(CreateAcceptStep());

        return secretScript;
    }

    private static Script CreateNoRiskScript()
    {
        var noRiskScript = new Script();

        noRiskScript.Steps.Add(CreateSetAnswerTypeStep());
        noRiskScript.Steps.Add(CreateSetAnswererNoRiskStep());
        noRiskScript.Steps.Add(CreateSetPriceNoRiskStep());
        noRiskScript.Steps.Add(CreateQuestionStep());
        noRiskScript.Steps.Add(CreateAskAnswerStep(StepParameterValues.AskAnswerMode_Direct));
        noRiskScript.Steps.Add(CreateAnswerStep());

        return noRiskScript;
    }

    private static Step CreateSetAnswerTypeStep()
    {
        var setAnswerTypeStep = new Step { Type = StepTypes.SetAnswerType };

        setAnswerTypeStep.Parameters.Add(
            StepParameterNames.Type,
            new StepParameter
            {
                IsRef = true,
                Type = StepParameterTypes.Simple,
                SimpleValue = QuestionParameterNames.AnswerType
            });

        setAnswerTypeStep.Parameters.Add(
            StepParameterNames.Options,
            new StepParameter
            {
                IsRef = true,
                Type = StepParameterTypes.Simple,
                SimpleValue = QuestionParameterNames.AnswerOptions
            });

        return setAnswerTypeStep;
    }

    private static Step CreateAcceptStep() => new() { Type = StepTypes.Accept };

    private static Step CreateAnnouncePriceSecretStep()
    {
        var announcePriceStep = new Step { Type = StepTypes.AnnouncePrice };

        var price = new StepParameter
        {
            IsRef = true,
            Type = StepParameterTypes.Simple,
            SimpleValue = QuestionParameterNames.Price
        };

        announcePriceStep.Parameters.Add(StepParameterNames.Content, price);

        return announcePriceStep;
    }

    private static Step CreateSetPriceSecretStep()
    {
        var setPriceStep = new Step { Type = StepTypes.SetPrice };
        setPriceStep.AddSimpleParameter(StepParameterNames.Mode, StepParameterValues.SetPriceMode_Select);

        var price = new StepParameter
        {
            IsRef = true,
            Type = StepParameterTypes.NumberSet,
            SimpleValue = QuestionParameterNames.Price
        };

        setPriceStep.Parameters.Add(StepParameterNames.Content, price);

        return setPriceStep;
    }

    private static Step CreateSetThemeStep()
    {
        var setThemeStep = new Step { Type = StepTypes.SetTheme };

        var theme = new StepParameter
        {
            IsRef = true,
            Type = StepParameterTypes.Simple,
            SimpleValue = QuestionParameterNames.Theme
        };

        setThemeStep.Parameters.Add(StepParameterNames.Content, theme);

        return setThemeStep;
    }

    private static Step CreateSetAnswererSecretStep()
    {
        var setAnswererStep = new Step { Type = StepTypes.SetAnswerer };
        setAnswererStep.AddSimpleParameter(StepParameterNames.Mode, StepParameterValues.SetAnswererMode_ByCurrent);

        var select = new StepParameter
        {
            IsRef = true,
            Type = StepParameterTypes.Simple,
            SimpleValue = QuestionParameterNames.SelectionMode
        };

        setAnswererStep.Parameters.Add(StepParameterNames.Select, select);

        return setAnswererStep;
    }

    private static Step CreateSetAnswererStep()
    {
        var setAnswererStep = new Step { Type = StepTypes.SetAnswerer };

        setAnswererStep.AddSimpleParameter(StepParameterNames.Mode, StepParameterValues.SetAnswererMode_Stake);
        setAnswererStep.AddSimpleParameter(StepParameterNames.Select, StepParameterValues.SetAnswererSelect_Highest);
        setAnswererStep.AddSimpleParameter(StepParameterNames.StakeVisibity, StepParameterValues.SetAnswererStakeVisibility_Visible);

        return setAnswererStep;
    }

    private static Step CreateSetAnswererAllStep()
    {
        var setAnswererStep = new Step { Type = StepTypes.SetAnswerer };

        setAnswererStep.AddSimpleParameter(StepParameterNames.Mode, StepParameterValues.SetAnswererMode_Stake);
        setAnswererStep.AddSimpleParameter(StepParameterNames.Select, StepParameterValues.SetAnswererSelect_AllPossible);
        setAnswererStep.AddSimpleParameter(StepParameterNames.StakeVisibity, StepParameterValues.SetAnswererStakeVisibility_Hidden);

        return setAnswererStep;
    }

    private static Step CreateSetPriceNoRiskStep()
    {
        var setPriceStep = new Step { Type = StepTypes.SetPrice };
        setPriceStep.AddSimpleParameter(StepParameterNames.Mode, StepParameterValues.SetPriceMode_NoRisk);

        return setPriceStep;
    }

    private static Step CreateSetAnswererNoRiskStep()
    {
        var setAnswererStep = new Step { Type = StepTypes.SetAnswerer };
        setAnswererStep.AddSimpleParameter(StepParameterNames.Mode, StepParameterValues.SetAnswererMode_Current);

        return setAnswererStep;
    }

    private static Step CreateAnswerStep()
    {
        var answerStep = new Step { Type = StepTypes.ShowContent };

        var answer = new StepParameter
        {
            IsRef = true,
            Type = StepParameterTypes.Simple,
            SimpleValue = QuestionParameterNames.Answer
        };

        answerStep.Parameters.Add(StepParameterNames.Content, answer);

        var answerFallback = new StepParameter
        {
            IsRef = true,
            Type = StepParameterTypes.Simple,
            SimpleValue = StepParameterValues.FallbackStepIdRef_Right,
        };

        answerStep.Parameters.Add(StepParameterNames.FallbackRefId, answerFallback);
        return answerStep;
    }

    private static Step CreateAskAnswerStep(string mode = StepParameterValues.AskAnswerMode_Button)
    {
        var askAnswerStep = new Step { Type = StepTypes.AskAnswer };
        askAnswerStep.AddSimpleParameter(StepParameterNames.Mode, mode);
        return askAnswerStep;
    }

    private static Step CreateQuestionStep()
    {
        var contentStep = new Step { Type = StepTypes.ShowContent };

        var content = new StepParameter
        {
            IsRef = true,
            Type = StepParameterTypes.Simple,
            SimpleValue = QuestionParameterNames.Question
        };

        contentStep.Parameters.Add(StepParameterNames.Content, content);
        return contentStep;
    }
}
