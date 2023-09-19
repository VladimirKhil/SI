using SIPackages;
using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;
using YamlDotNet.Serialization.NamingConventions;

namespace SIQuester.ViewModel.Serializers;

internal sealed class YamlSerializer
{
    private static readonly ISerializer _serializer;

    private static readonly IDeserializer _deserializer;

    static YamlSerializer()
    {
        var ignore = new YamlIgnoreAttribute();

        _serializer = new SerializerBuilder()
            .DisableAliases()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithEventEmitter(next => new FlowEmitter(next))
            .WithAttributeOverride<Package>(package => package.ID, new YamlMemberAttribute { Alias = "id" })
            .WithAttributeOverride<Package>(package => package.LogoItem, ignore)
            .WithAttributeOverride<Atom>(atom => atom.IsLink, ignore)
            .WithAttributeOverride<Atom>(atom => atom.TypeString, ignore)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults | DefaultValuesHandling.OmitEmptyCollections)
            .Build();

        _deserializer = new DeserializerBuilder()
            .WithTypeInspector(typeInspector => new CustomTypeInspector(typeInspector))
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithAttributeOverride<Package>(package => package.ID, new YamlMemberAttribute { Alias = "id" })
            .WithAttributeOverride<Package>(package => package.LogoItem, ignore)
            .WithAttributeOverride<Atom>(atom => atom.IsLink, ignore)
            .WithAttributeOverride<Atom>(atom => atom.TypeString, ignore)
            .Build();
    }

    internal static void SerializePackage(TextWriter textWriter, Package package) => _serializer.Serialize(textWriter, package);

    internal static Package DeserializePackage(TextReader textReader) => _deserializer.Deserialize<Package>(textReader);

    private sealed class FlowEmitter : ChainedEventEmitter
    {
        public FlowEmitter(IEventEmitter nextEmitter) : base(nextEmitter) { }

        public override void Emit(SequenceStartEventInfo eventInfo, IEmitter emitter)
        {
            var sourceType = eventInfo.Source.Type;

            if (sourceType == typeof(Answers) || sourceType == typeof(Authors) || sourceType == typeof(Sources))
            {
                eventInfo.Style = SequenceStyle.Flow;
            }

            nextEmitter.Emit(eventInfo, emitter);
        }
    }

    private sealed class CustomTypeInspector : ITypeInspector
    {
        private readonly ITypeInspector _typeInspector;

        public CustomTypeInspector(ITypeInspector typeInspector) => _typeInspector = typeInspector;

        public IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container)
        {
            return _typeInspector.GetProperties(type, container);
        }

        public IPropertyDescriptor GetProperty(Type type, object? container, string name, [MaybeNullWhen(true)] bool ignoreUnmatched)
        {
            if (type == typeof(Package) && name == nameof(Package.Rounds).ToLowerInvariant())
            {
                return new CollectionPropertyDesriptor<Round>(nameof(Package.Rounds), typeof(List<Round>));
            }
            else if (type == typeof(Package) && name == nameof(Package.Tags).ToLowerInvariant())
            {
                return new CollectionPropertyDesriptor<string>(nameof(Package.Tags), typeof(List<string>));
            }
            else if (type == typeof(Round) && name == nameof(Round.Themes).ToLowerInvariant())
            {
                return new CollectionPropertyDesriptor<Theme>(nameof(Round.Themes), typeof(List<Theme>));
            }
            else if (type == typeof(Theme) && name == nameof(Theme.Questions).ToLowerInvariant())
            {
                return new CollectionPropertyDesriptor<Question>(nameof(Theme.Questions), typeof(List<Question>));
            }
            else if (type == typeof(Question) && name == nameof(Question.Scenario).ToLowerInvariant())
            {
                return new CollectionPropertyDesriptor<Atom>(nameof(Question.Scenario), typeof(Scenario));
            }
            else if (type == typeof(Question) && name == nameof(Question.Right).ToLowerInvariant())
            {
                return new CollectionPropertyDesriptor<string>(nameof(Question.Right), typeof(Answers));
            }
            else if (type == typeof(Question) && name == nameof(Question.Wrong).ToLowerInvariant())
            {
                return new CollectionPropertyDesriptor<string>(nameof(Question.Wrong), typeof(Answers));
            }
            else if (type == typeof(Question) && name == nameof(Question.Parameters).ToLowerInvariant())
            {
                return new StepParametersPropertyDesriptor();
            }
            else if (type == typeof(Question) && name == nameof(Question.Type).ToLowerInvariant())
            {
                return new QuestionTypePropertyDesriptor();
            }
            else if (type == typeof(QuestionType) && name == nameof(QuestionType.Params).ToLowerInvariant())
            {
                return new CollectionPropertyDesriptor<QuestionTypeParam>(nameof(QuestionType.Params), typeof(List<QuestionTypeParam>));
            }
            else if (type.IsSubclassOf(typeof(InfoOwner)) && name == nameof(InfoOwner.Info).ToLowerInvariant())
            {
                return new InfoPropertyDesriptor();
            }
            else if (type == typeof(Info) && name == nameof(Info.Authors).ToLowerInvariant())
            {
                return new CollectionPropertyDesriptor<string>(nameof(Info.Authors), typeof(Authors));
            }
            else if (type == typeof(Info) && name == nameof(Info.Sources).ToLowerInvariant())
            {
                return new CollectionPropertyDesriptor<string>(nameof(Info.Sources), typeof(Sources));
            }
            else if (type == typeof(Info) && name == nameof(Info.Comments).ToLowerInvariant())
            {
                return new CommentsPropertyDesriptor();
            }
            else if (type == typeof(Script) && name == nameof(Script.Steps).ToLowerInvariant())
            {
                return new CollectionPropertyDesriptor<Step>(nameof(Script.Steps), typeof(List<Step>));
            }
            else if (type == typeof(Step) && name == nameof(Step.Parameters).ToLowerInvariant())
            {
                return new ParametersPropertyDesriptor();
            }

            return _typeInspector.GetProperty(type, container, name, ignoreUnmatched);
        }
    }

    private abstract class CustomPropertyDesriptor : IPropertyDescriptor
    {
        private readonly Type _type;

        public string Name => throw new NotImplementedException();

        public bool CanWrite => throw new NotImplementedException();

        public Type Type => _type;

        public Type? TypeOverride { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int Order { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public ScalarStyle ScalarStyle { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public CustomPropertyDesriptor(Type type)
        {
            _type = type;
        }

        public T GetCustomAttribute<T>() where T : Attribute
        {
            throw new NotImplementedException();
        }

        public IObjectDescriptor Read(object target)
        {
            throw new NotImplementedException();
        }

        public abstract void Write(object target, object? value);
    }

    private sealed class CollectionPropertyDesriptor<T> : CustomPropertyDesriptor
    {
        private readonly string _propertyName;

        public CollectionPropertyDesriptor(string propertyName, Type type) : base(type) 
        {
            _propertyName = propertyName;
        }

        public override void Write(object target, object? value)
        {
            var propertyInfo = target.GetType().GetProperty(_propertyName);

            if (propertyInfo == null)
            {
                throw new InvalidOperationException($"Property {_propertyName} not found");
            }

            var collection = propertyInfo.GetValue(target, null) as ICollection<T>;

            if (collection == null)
            {
                throw new InvalidOperationException($"Collection value {_propertyName} not found");
            }

            if (value is not IEnumerable<T> sourceColletion)
            {
                throw new InvalidOperationException($"Value is not a collection");
            }

            foreach (var item in sourceColletion)
            {
                collection.Add(item);
            }
        }
    }

    private sealed class StepParametersPropertyDesriptor : CustomPropertyDesriptor
    {
        public StepParametersPropertyDesriptor() : base(typeof(StepParameters)) { }

        public override void Write(object target, object? value)
        {
            var question = (Question)target;

            if (value is not StepParameters stepParameters)
            {
                return;
            }

            question.Parameters ??= new StepParameters();

            foreach (var param in stepParameters)
            {
                question.Parameters[param.Key] = param.Value;
            }
        }
    }

    private sealed class QuestionTypePropertyDesriptor : CustomPropertyDesriptor
    {
        public QuestionTypePropertyDesriptor() : base(typeof(QuestionType)) { }

        public override void Write(object target, object? value)
        {
            var question = (Question)target;

            if (value is not QuestionType questionType)
            {
                return;
            }

            question.Type.Name = questionType.Name;

            foreach (var param in questionType.Params)
            {
                question.Type.Params.Add(param);
            }
        }
    }

    private sealed class InfoPropertyDesriptor : CustomPropertyDesriptor
    {
        public InfoPropertyDesriptor() : base(typeof(Info)) { }

        public override void Write(object target, object? value)
        {
            var infoOwner = (InfoOwner)target;

            if (value is not Info info)
            {
                return;
            }

            infoOwner.Info.Authors.AddRange(info.Authors);
            infoOwner.Info.Sources.AddRange(info.Sources);
            infoOwner.Info.Comments.Text = info.Comments.Text;
        }
    }

    private sealed class CommentsPropertyDesriptor : CustomPropertyDesriptor
    {
        public CommentsPropertyDesriptor() : base(typeof(Comments)) { }

        public override void Write(object target, object? value)
        {
            var info = (Info)target;

            if (value is not Comments comments)
            {
                return;
            }

            info.Comments.Text = comments.Text;
        }
    }

    private sealed class ParametersPropertyDesriptor : CustomPropertyDesriptor
    {
        public ParametersPropertyDesriptor() : base(typeof(StepParameters)) { }

        public override void Write(object target, object? value)
        {
            var step = (Step)target;

            if (value is not StepParameters parameters)
            {
                return;
            }

            foreach (var item in parameters)
            {
                step.Parameters[item.Key] = item.Value;
            }
        }
    }

    private sealed class RightPropertyDesriptor : CustomPropertyDesriptor
    {
        public RightPropertyDesriptor() : base(typeof(Answers)) { }

        public override void Write(object target, object? value)
        {
            var question = (Question)target;

            if (value is not Answers answers)
            {
                return;
            }

            foreach (var answer in answers)
            {
                question.Right.Add(answer);
            }
        }
    }
}
