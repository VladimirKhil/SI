using SIPackages;
using SIPackages.Core;
using SIQuester.Model;
using SIQuester.ViewModel.Helpers;
using SIQuester.ViewModel.Properties;
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
    public override IItemViewModel Owner => null;

    public QDocument Document { get; private set; }

    public ObservableCollection<RoundViewModel> Rounds { get; } = new();

    /// <summary>
    /// Does the package have the new format.
    /// </summary>
    public bool IsUpgraded => Model.Version >= 5.0;

    public ICommand AddRound { get; private set; }

    public SimpleCommand AddRestrictions { get; private set; }

    public SimpleCommand AddTags { get; private set; }

    public SimpleCommand ChangeLanguage { get; private set; }

    public TagsViewModel Tags { get; private set; }

    public override ICommand Add { get; protected set; }

    public override string AddHeader => Resources.AddRound;

    public override ICommand Remove
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

        Tags.CollectionChanged += Tags_CollectionChanged;
        Model.PropertyChanged += Model_PropertyChanged;
        Rounds.CollectionChanged += Rounds_CollectionChanged;

        Add = AddRound = new SimpleCommand(AddRound_Executed);
        AddRestrictions = new SimpleCommand(AddRestrictions_Executed);
        AddTags = new SimpleCommand(AddTags_Executed) { CanBeExecuted = Tags.Count == 0 };

        ChangeLanguage = new SimpleCommand(ChangeLanguage_Executed);

        SelectLogo = new SimpleCommand(SelectLogo_Executed);
        RemoveLogo = new SimpleCommand(RemoveLogo_Executed);
    }

    private void ChangeLanguage_Executed(object? arg)
    {
        Model.Language = Model.Language == "ru-RU" ? "en-US" : "ru-RU";
    }

    private void Tags_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        AddTags.CanBeExecuted = Tags.Count == 0;
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
                for (int i = e.NewStartingIndex; i < e.NewStartingIndex + e.NewItems.Count; i++)
                {
                    if (Rounds[i].OwnerPackage != null)
                    {
                        throw new Exception("Попытка вставить привязанный раунд!");
                    }

                    Rounds[i].OwnerPackage = this;
                    Model.Rounds.Insert(i, Rounds[i].Model);
                }
                break;

            case NotifyCollectionChangedAction.Remove:
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
            Name = (Rounds.Count + 1).ToString() + Resources.EndingRound,
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
        QDocument.ActivatedObject = Tags;
        Tags.Add("");
    }

    private void SelectLogo_Executed(object? arg)
    {
        var model = arg as MediaItemViewModel;

        if (model == null)
        {
            var images = Document.Images;
            var was = images.Files.Count;

            images.AddItem.Execute(null);

            if (!images.HasPendingChanges)
            {
                return;
            }

            if (was == images.Files.Count)
            {
                return;
            }

            model = images.Files.LastOrDefault();
        }

        Model.Logo = "@" + model.Model.Name;
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
