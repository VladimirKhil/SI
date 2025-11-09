using Notions;
using OpenAI;
using OpenAI.Chat;
using SIPackages;
using SIPackages.Core;
using SIQuester.Model;
using SIQuester.ViewModel.Helpers;
using SIQuester.ViewModel.PlatformSpecific;
using SIQuester.ViewModel.Properties;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text;
using System.Text.Json;

namespace SIQuester.ViewModel.Services;

internal static class QuestionsGenerator
{
    public static async Task GenerateThemeQuestionsAsync(ThemeViewModel themeViewModel)
    {
        if (themeViewModel.OwnerRound == null)
        {
            return;
        }

        var theme = themeViewModel.Model;

        if (string.IsNullOrEmpty(AppSettings.Default.GPTApiKey))
        {
            throw new Exception(Resources.ApiKeyNotSet);
        }

        var prompt = AppSettings.Default.GPTPrompt;

        if (string.IsNullOrEmpty(prompt))
        {
            prompt = Resources.DefaultGPTPrompt;
        }

        var themeNameSet = !string.IsNullOrEmpty(theme.Name);
        var topicName = themeNameSet ? $"\"{theme.Name}\"" : "";

        var questionToGenerateCount = themeViewModel.OwnerRound.Model.Type == RoundTypes.Final ? 1 : 5;
        var promptExamples = new StringBuilder();

        for (var i = 0; i < themeViewModel.Questions.Count; i++)
        {
            var question = themeViewModel.Questions[i];

            var text = question.Model.GetText();
            var answer = question.Right.FirstOrDefault() ?? "";
            var comment = question.Info.Comments.Text;

            if (text.Length > 0 && answer.Length > 0)
            {
                questionToGenerateCount--;
                promptExamples.AppendLine($"{i + 1}. {Resources.Question}: {text}").AppendLine($"{Resources.Answer}: {answer}");

                if (comment.Length > 0)
                {
                    promptExamples.AppendLine($"{Resources.Comment}: {comment}");
                }

                if (questionToGenerateCount == 0)
                {
                    break;
                }
            }
        }

        if (questionToGenerateCount == 0)
        {
            PlatformManager.Instance.Inform(Resources.NoQuestionsToGenerate);
            return;
        }

        string userPrompt;

        if (themeViewModel.OwnerRound.Model.Type == RoundTypes.Final)
        {
            userPrompt = string.Format(Resources.GPTCreateTopicFinalRoundPrompt, topicName, questionToGenerateCount);
        }
        else
        {
            userPrompt = string.Format(Resources.CreateTopicPrompt, topicName, questionToGenerateCount);

            if (promptExamples.Length > 0)
            {
                userPrompt += $" {Resources.BasedOnExamples}:\n{promptExamples}";
            }
        }

        List<ChatMessage> messages = new()
        {
            new SystemChatMessage(prompt),
            new UserChatMessage(userPrompt)
        };

        ChatCompletionOptions options = new()
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "theme_questions",
                jsonSchema: JsonSchema,
                jsonSchemaIsStrict: true)
        };

        var client = new ChatClient(AppSettings.Default.GPTModel, new ApiKeyCredential(AppSettings.Default.GPTApiKey), new OpenAIClientOptions
        {
            NetworkTimeout = TimeSpan.FromMinutes(4),
            RetryPolicy = new ClientRetryPolicy(0)
        });

        ClientResult<ChatCompletion> completion;

        using (var dialog = PlatformManager.Instance.ShowProgressDialog())
        {
            completion = await client.CompleteChatAsync(messages, options);
        }

        var data = completion.Value.Content[0].Text;
        var themeInfo = JsonSerializer.Deserialize<ThemeInfo>(data) ?? throw new Exception(Resources.FailedToParseGPTResponse);

        var document = (themeViewModel.OwnerRound?.OwnerPackage?.Document) ?? throw new InvalidOperationException("Document not found");
        using var change = document.OperationsManager.BeginComplexChange();

        for (var i = 0; i < themeViewModel.Questions.Count; i++)
        {
            var question = themeViewModel.Questions[i];

            if (question.Model.GetText().Length == 0 && (question.Right.FirstOrDefault()?.Length ?? 0) == 0)
            {
                themeViewModel.Questions.RemoveAt(i);
                i--;
            }
        }

        if (!themeNameSet && !string.IsNullOrEmpty(themeInfo.Name))
        {
            theme.Name = themeInfo.Name;
        }

        if (!string.IsNullOrEmpty(themeInfo.Description))
        {
            themeViewModel.Info.Comments.Text = themeInfo.Description.ClearPoints();
        }

        foreach (var questionInfo in themeInfo.Questions)
        {
            var price = themeViewModel.DetectNextQuestionPrice(themeViewModel.OwnerRound);
            var question = PackageItemsHelper.CreateQuestion(price, questionInfo.Text.ClearPoints(), questionInfo.Answer.ClearPoints());

            if (questionInfo.AnswerOptions.Length > 0)
            {
                question.Parameters[QuestionParameterNames.AnswerType] = new StepParameter
                {
                    Type = StepParameterTypes.Simple,
                    SimpleValue = StepParameterValues.SetAnswerTypeType_Select
                };

                StepParameters groupValue = new();

                for (var i = 0; i < questionInfo.AnswerOptions.Length; i++)
                {
                    groupValue.Add(
                        IndexLabelHelper.GetIndexLabel(i),
                        new()
                        {
                            Type = StepParameterTypes.Content,
                            ContentValue = new()
                            {
                                    new() { Type = ContentTypes.Text, Value = questionInfo.AnswerOptions[i].ClearPoints() }
                            }
                        });
                }

                question.Parameters[QuestionParameterNames.AnswerOptions] = new StepParameter
                {
                    Type = StepParameterTypes.Group,
                    GroupValue = groupValue
                };
            }

            if (!string.IsNullOrEmpty(questionInfo.Comment))
            {
                question.Info.Comments.Text = questionInfo.Comment.ClearPoints();
            }

            if (!string.IsNullOrEmpty(questionInfo.Source))
            {
                question.Info.Sources.Add(questionInfo.Source.ClearPoints());
            }

            var questionViewModel = new QuestionViewModel(question);
            themeViewModel.Questions.Add(questionViewModel);
        }

        change.Commit();
        themeViewModel.IsExpanded = true;
    }

    public sealed class QuestionInfo
    {
        public string Text { get; set; } = string.Empty;

        public string[] AnswerOptions { get; set; } = Array.Empty<string>();

        public string Answer { get; set; } = string.Empty;

        public string Comment { get; set; } = string.Empty;

        public string Source { get; set; } = string.Empty;
    }

    public sealed class ThemeInfo
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public List<QuestionInfo> Questions { get; set; } = new List<QuestionInfo>();
    }

    private static readonly BinaryData JsonSchema = BinaryData.FromBytes(
        Encoding.UTF8.GetBytes(
        @"{
            ""type"": ""object"",
            ""definitions"": {
                ""QuestionInfo"": {
                    ""type"": ""object"",
                    ""additionalProperties"": false,
                    ""properties"": {
                        ""Text"": {
                            ""type"": ""string""
                        },
                        ""AnswerOptions"": {
                            ""type"": ""array"",
                            ""items"": {
                                ""type"": ""string""
                            }
                        },
                        ""Answer"": {
                            ""type"": ""string""
                        },
                        ""Comment"": {
                            ""type"": ""string""
                        },
                        ""Source"": {
                            ""type"": ""string""
                        }
                    },
                    ""required"": [
                        ""Text"",
                        ""AnswerOptions"",
                        ""Answer"",
                        ""Comment"",
                        ""Source""
                    ]
                }
            },
            ""properties"": {
                ""Name"": {
                    ""type"": ""string""
                },
                ""Description"": {
                    ""type"": ""string""
                },
                ""Questions"": {
                    ""type"": ""array"",
                    ""items"": {
                        ""$ref"": ""#/definitions/QuestionInfo""
                    }
                }
            },
            ""required"": [
                ""Name"",
                ""Description"",
                ""Questions""
            ],
            ""additionalProperties"": false
        }"));
}
