using SIPackages;
using SIPackages.Core;
using SIQuester.Model;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Helpers;
using SIQuester.ViewModel.PlatformSpecific;
using SIQuester.ViewModel.Properties;
using SIQuester.ViewModel.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using Utils.Commands;

namespace SIQuester.ViewModel;

/// <summary>
/// Defines a package view model.
/// </summary>
public sealed class PackageViewModel : ItemViewModel<Package>
{
    public override IItemViewModel? Owner => null;

    public QDocument Document { get; private set; }

    public ObservableCollection<RoundViewModel> Rounds { get; } = new();

    public ICommand AddRound { get; private set; }

    public SimpleCommand AddRestrictions { get; private set; }

    public SimpleCommand AddTags { get; private set; }

    public SimpleCommand ChangeLanguage { get; private set; }

    public TagsViewModel Tags { get; private set; }

    public override ICommand Add { get; protected set; }

    public override string AddHeader => Resources.AddRound;

    public override ICommand? Remove
    {
        get => null;
        protected set { }
    }

    /// <summary>
    /// Selects logo for package.
    /// </summary>
    public ICommand SelectLogo { get; private set; }

    public ICommand RemoveLogo { get; private set; }

    private IMedia? _logo = null;

    public IMedia? Logo
    {
        get
        {
            if (_logo == null)
            {
                if (Model.Logo != null && Model.Logo.Length > 0)
                {
                    _logo = Document.Images.Wrap(Model.Logo[1..]);
                }
            }

            return _logo;
        }
        set
        {
            if (_logo != value)
            {
                _logo = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand CopyInfo { get; private set; }

    public ICommand PasteInfo { get; private set; }

    /// <summary>
    /// Has quality control.
    /// </summary>
    public bool HasQualityControl
    {
        get => Model.HasQualityControl;
        set
        {
            if (Model.HasQualityControl != value)
            {
                if (value)
                {
                    if (!Document.CheckPackageQuality())
                    {
                        return;
                    }
                }

                Model.HasQualityControl = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Generates themes with the help of GPT.
    /// </summary>
    public ICommand GenerateThemes { get; private set; }

    public PackageViewModel(Package package, QDocument document)
        : base(package)
    {
        Document = document;

        foreach (var round in package.Rounds)
        {
            Rounds.Add(new RoundViewModel(round) { OwnerPackage = this });
        }

        Tags = new TagsViewModel(this, Model.Tags);

        BindHelper.Bind(Tags, Model.Tags);

        Model.PropertyChanged += Model_PropertyChanged;
        Rounds.CollectionChanged += Rounds_CollectionChanged;

        Add = AddRound = new SimpleCommand(AddRound_Executed);
        AddRestrictions = new SimpleCommand(AddRestrictions_Executed);
        AddTags = new SimpleCommand(AddTags_Executed);

        ChangeLanguage = new SimpleCommand(ChangeLanguage_Executed);

        SelectLogo = new SimpleCommand(SelectLogo_Executed);
        RemoveLogo = new SimpleCommand(RemoveLogo_Executed);
        
        CopyInfo = new SimpleCommand(CopyInfo_Executed);
        PasteInfo = new SimpleCommand(PasteInfo_Executed);
        
        GenerateThemes = new SimpleCommand(GenerateThemes_Executed);
    }

    private async void GenerateThemes_Executed(object? arg)
    {
        try
        {
            await QuestionsGenerator.GenerateThemesAsync(this);
        }
        catch (Exception exc)
        {
            PlatformManager.Instance.ShowErrorMessage(exc.Message);
        }
    }

    private void CopyInfo_Executed(object? arg)
    {
        try
        {
            PlatformManager.Instance.CopyInfo(new
            {
                Model.Info.Authors,
                Model.Info.Sources,
                Comments = Model.Info.Comments.Text,
                Model.Tags,
                Model.Restriction,
                Model.Difficulty,
                Model.Name,
                Model.ContactUri,
                Model.Date,
                Model.Publisher
            });
        }
        catch (Exception exc)
        {
            PlatformManager.Instance.ShowErrorMessage(exc.Message);
        }
    }

    private void PasteInfo_Executed(object? arg)
    {
        try
        {
            var info = PlatformManager.Instance.PasteInfo();

            if (info == null)
            {
                return;
            }

            using var change = Document.OperationsManager.BeginComplexChange();

            foreach (var pair in info)
            {
                switch (pair.Key)
                {
                    case nameof(Model.Info.Authors):
                        Info.Authors.ClearOneByOne();

                        foreach (var item in pair.Value.EnumerateArray())
                        {
                            Info.Authors.Add(item.GetString() ?? "");
                        }
                        break;

                    case nameof(Model.Info.Sources):
                        Info.Sources.ClearOneByOne();

                        foreach (var item in pair.Value.EnumerateArray())
                        {
                            Info.Sources.Add(item.GetString() ?? "");
                        }
                        break;

                    case nameof(Model.Info.Comments):
                        {
                            var value = pair.Value.GetString();

                            if (value != null)
                            {
                                Info.Comments.Text = value;
                            }
                        }
                        break;

                    case nameof(Model.Tags):
                        Tags.ClearOneByOne();

                        foreach (var item in pair.Value.EnumerateArray())
                        {
                            Tags.Add(item.GetString() ?? "");
                        }
                        break;

                    case nameof(Model.Restriction):
                        {
                            var value = pair.Value.GetString();

                            if (value != null)
                            {
                                Model.Restriction = value;
                            }
                        }
                        break;

                    case nameof(Model.Difficulty):
                        {
                            Model.Difficulty = pair.Value.GetInt32();
                        }
                        break;

                    case nameof(Model.Name):
                        {
                            var value = pair.Value.GetString();

                            if (value != null)
                            {
                                Model.Name = value;
                            }
                        }
                        break;

                    case nameof(Model.ContactUri):
                        {
                            var value = pair.Value.GetString();

                            if (value != null)
                            {
                                Model.ContactUri = value;
                            }
                        }
                        break;

                    case nameof(Model.Date):
                        {
                            var value = pair.Value.GetString();

                            if (value != null)
                            {
                                Model.Date = value;
                            }
                        }
                        break;

                    case nameof(Model.Publisher):
                        {
                            var value = pair.Value.GetString();

                            if (value != null)
                            {
                                Model.Publisher = value;
                            }
                        }
                        break;

                    default:
                        break;
                }
            }

            change.Commit();
        }
        catch (Exception exc)
        {
            PlatformManager.Instance.ShowErrorMessage(exc.Message);
        }
    }

    private void ChangeLanguage_Executed(object? arg)
    {
        Model.Language = Model.Language == "ru-RU" ? "en-US" : "ru-RU";
    }

    private void Model_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Package.Restriction))
        {
            AddRestrictions.CanBeExecuted = Model.Restriction.Length == 0;
        }
    }

    private void Rounds_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
            case NotifyCollectionChangedAction.Replace:
                if (e.NewItems == null || e.NewStartingIndex < 0)
                {
                    return;
                }

                for (int i = e.NewStartingIndex; i < e.NewStartingIndex + e.NewItems.Count; i++)
                {
                    if (Rounds[i].OwnerPackage != null)
                    {
                        throw new Exception("An attempt to add bound round");
                    }

                    Rounds[i].OwnerPackage = this;
                    Model.Rounds.Insert(i, Rounds[i].Model);
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems == null || e.OldStartingIndex < 0)
                {
                    return;
                }

                foreach (RoundViewModel round in e.OldItems)
                {
                    round.OwnerPackage = null;
                    Model.Rounds.RemoveAt(e.OldStartingIndex);

                    Document.ClearLinks(round);
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                Model.Rounds.Clear();

                foreach (var round in Rounds)
                {
                    round.OwnerPackage = this;
                    Model.Rounds.Add(round.Model);
                }
                break;
        }
    }
    
    private void AddRound_Executed(object? arg)
    {
        var round = new Round
        { 
            Name = (Rounds.Count + 1).ToString(),
            Type = RoundTypes.Standart
        };

        var roundViewModel = new RoundViewModel(round);
        Rounds.Add(roundViewModel);
        QDocument.ActivatedObject = roundViewModel;
        Document.Navigate.Execute(roundViewModel);
    }

    private void AddRestrictions_Executed(object? arg)
    {
        QDocument.ActivatedObject = this;
        Model.Restriction = Resources.Restriction;
    }

    private void AddTags_Executed(object? arg)
    {
        var newTags = PlatformManager.Instance.AskTags(Tags);

        if (newTags == null)
        {
            return;
        }

        using var change = Document.OperationsManager.BeginComplexChange();

        Tags.ClearOneByOne();

        foreach (var item in newTags)
        {
            Tags.Add(item);
        }

        change.Commit();
        OnPropertyChanged(nameof(Tags));
    }

    private void SelectLogo_Executed(object? arg)
    {
        var model = arg as MediaItemViewModel;

        if (model == null)
        {
            var images = Document.Images;
            var previousFileCount = images.Files.Count;

            images.AddItem.Execute(null);

            if (!images.HasPendingChanges)
            {
                return;
            }

            if (previousFileCount == images.Files.Count)
            {
                return;
            }

            model = images.Files.LastOrDefault();
        }

        if (model == null)
        {
            return;
        }

        Model.Logo = $"@{model.Model.Name}";
        _logo = null;
        OnPropertyChanged(nameof(Logo));
    }

    private void RemoveLogo_Executed(object? arg)
    {
        Model.Logo = "";
        _logo = null;
        OnPropertyChanged(nameof(Logo));
    }

    protected override void UpdateCosts(CostSetter costSetter)
    {
        using var change = Document.OperationsManager.BeginComplexChange();

        base.UpdateCosts(costSetter);

        foreach (var round in Rounds)
        {
            foreach (var theme in round.Themes)
            {
                theme.UpdateCostsCore(costSetter);
            }
        }

        change.Commit();
    }
}
