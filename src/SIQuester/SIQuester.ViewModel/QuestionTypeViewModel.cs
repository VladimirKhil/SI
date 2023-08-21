using SIPackages;
using SIPackages.Core;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Utils.Commands;

namespace SIQuester.ViewModel;

public sealed class QuestionTypeViewModel : ModelViewBase
{
    public string Name
    {
        get => Model.Name;
        set { Model.Name = value; }
    }

    public QuestionType Model { get; }

    public ObservableCollection<QuestionTypeParamViewModel> Params { get; private set; }

    public SimpleCommand AddQuestionTypeParam { get; private set; }

    public QuestionTypeViewModel(QuestionType model)
    {
        Model = model;
        Params = new ObservableCollection<QuestionTypeParamViewModel>();

        Model.PropertyChanged += Model_PropertyChanged;

        AddQuestionTypeParam = new SimpleCommand(AddQuestionTypeParam_Executed) { CanBeExecuted = AddQuestionTypeParam_CanExecute() };

        foreach (var item in Model.Params)
        {
            var viewModel = new QuestionTypeParamViewModel(item) { Owner = this };
            viewModel.RemoveQuestionTypeParam.CanBeExecuted = AddQuestionTypeParam.CanBeExecuted;
            Params.Add(viewModel);
        }

        Params.CollectionChanged += Params_CollectionChanged;
    }

    private void Params_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
            case NotifyCollectionChangedAction.Replace:
                for (int i = e.NewStartingIndex; i < e.NewStartingIndex + e.NewItems.Count; i++)
                {
                    Params[i].Owner = this;
                    Model.Params.Insert(i, Params[i].Model);
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                foreach (QuestionTypeParamViewModel param in e.OldItems)
                {
                    param.Owner = null;
                    Model.Params.RemoveAt(e.OldStartingIndex);
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                Model.Params.Clear();

                foreach (QuestionTypeParamViewModel param in Params)
                {
                    param.Owner = this;
                    Model.Params.Add(param.Model);
                }
                break;
        }
    }

    private void Model_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(QuestionType.Name))
        {
            AddQuestionTypeParam.CanBeExecuted = AddQuestionTypeParam_CanExecute();

            foreach (var item in Params)
            {
               item.RemoveQuestionTypeParam.CanBeExecuted = AddQuestionTypeParam.CanBeExecuted;
            }
        }

        OnPropertyChanged(e);
    }

    public void AddParam(string name, string value)
    {
        var param = new QuestionTypeParamViewModel(new QuestionTypeParam { Name = name, Value = value });
        param.RemoveQuestionTypeParam.CanBeExecuted = AddQuestionTypeParam.CanBeExecuted;
        Params.Add(param);
    }

    private void AddQuestionTypeParam_Executed(object? arg)
    {
        var param = new QuestionTypeParamViewModel();
        param.RemoveQuestionTypeParam.CanBeExecuted = AddQuestionTypeParam.CanBeExecuted;
        Params.Add(param);
    }

    private bool AddQuestionTypeParam_CanExecute()
    {
        var name = Model.Name;

        return name != QuestionTypes.Simple
            && name != QuestionTypes.Cat
            && name != QuestionTypes.BagCat
            && name != QuestionTypes.Auction
            && name != QuestionTypes.Sponsored;
    }
}
