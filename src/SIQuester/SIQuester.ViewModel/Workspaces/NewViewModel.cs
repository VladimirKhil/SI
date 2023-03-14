using Microsoft.Extensions.Logging;
using SIPackages;
using SIPackages.Core;
using SIQuester.Model;
using SIQuester.ViewModel.Properties;
using System.Windows.Input;
using Utils.Commands;

namespace SIQuester.ViewModel;

/// <summary>
/// Represents a new package view model.
/// </summary>
public sealed class NewViewModel : WorkspaceViewModel
{
    private PackageType _packageType = PackageType.Classic;
    private string _packageName = Resources.SIGameQuestions;
    private string _packageAuthor = Environment.UserName;

    /// <summary>
    /// Creates a new package.
    /// </summary>
    public ICommand Create { get; }

    public override string Header => Resources.NewPackage;

    public PackageType PackageType
    {
        get => _packageType;
        set
        {
            if (_packageType != value)
            {
                _packageType = value;
                OnPropertyChanged();
            }
        }
    }

    public string PackageName
    {
        get => _packageName;
        set
        {
            if (_packageName != value)
            {
                _packageName = value;
                OnPropertyChanged();
            }
        }
    }

    public string PackageAuthor
    {
        get => _packageAuthor;
        set
        {
            if (_packageAuthor != value)
            {
                _packageAuthor = value;
                OnPropertyChanged();
            }
        }
    }

    public NonStandartPackageParams PackageParams { get; } = new NonStandartPackageParams();

    private readonly StorageContextViewModel _storageContextViewModel;

    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<NewViewModel> _logger;

    public NewViewModel(StorageContextViewModel storageContextViewModel, ILoggerFactory loggerFactory)
    {
        _storageContextViewModel = storageContextViewModel;
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger<NewViewModel>();

        Create = new SimpleCommand(Create_Executed);
    }

    private void Create_Executed(object? arg)
    {
        try
        {
            var doc = SIDocument.Create(_packageName, _packageAuthor);

            switch (_packageType)
            {
                case PackageType.Classic:
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            var round = doc.Package.CreateRound(RoundTypes.Standart, null);

                            for (int j = 0; j < 6; j++)
                            {
                                var theme = round.CreateTheme(null);

                                for (int k = 0; k < 5; k++)
                                {
                                    theme.CreateQuestion(100 * (i + 1) * (k + 1));
                                }
                            }
                        }
                        var final = doc.Package.CreateRound(RoundTypes.Final, Resources.FinalName);

                        for (int j = 0; j < 7; j++)
                        {
                            final.CreateTheme(null).CreateQuestion(0);
                        }
                    }
                    break;

                case PackageType.Special:
                    {
                        var param = PackageParams;

                        for (int i = 0; i < param.NumOfRounds; i++)
                        {
                            var round = doc.Package.CreateRound(RoundTypes.Standart, null);

                            for (int j = 0; j < param.NumOfThemes; j++)
                            {
                                var theme = round.CreateTheme(null);

                                for (int k = 0; k < param.NumOfQuestions; k++)
                                {
                                    theme.CreateQuestion(param.NumOfPoints * (i + 1) * (k + 1));
                                }
                            }
                        }

                        if (param.HasFinal)
                        {
                            var final = doc.Package.CreateRound(RoundTypes.Final, Resources.FinalName);

                            for (int j = 0; j < param.NumOfFinalThemes; j++)
                            {
                                final.CreateTheme(null).CreateQuestion(0);
                            }
                        }
                    }
                    break;

                case PackageType.ThemesCollection:
                    doc.Package.CreateRound(RoundTypes.Standart, Resources.ThemesCollection);
                    break;

                case PackageType.Empty:
                default:
                    break;
            }

            OnNewItem(new QDocument(doc, _storageContextViewModel, _loggerFactory) { FileName = doc.Package.Name });

            _logger.LogInformation("New document created. Name: {name}", doc.Package.Name);

            OnClosed();
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
    }
}
