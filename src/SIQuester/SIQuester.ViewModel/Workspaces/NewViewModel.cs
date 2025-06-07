﻿using Microsoft.Extensions.Logging;
using SIPackages;
using SIPackages.Core;
using SIPackages.Helpers;
using SIQuester.Model;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Model;
using SIQuester.ViewModel.PlatformSpecific;
using SIQuester.ViewModel.Properties;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Utils.Commands;

namespace SIQuester.ViewModel;

/// <summary>
/// Represents a new package view model.
/// </summary>
public sealed class NewViewModel : WorkspaceViewModel
{
    private PackageTemplate _currentTemplate;
    private string _packageName = Resources.SIGameQuestions;
    private string _packageAuthor = Environment.UserName;
    private readonly AppSettings _appSettings;

    /// <summary>
    /// Creates a new package.
    /// </summary>
    public ICommand Create { get; }

    /// <summary>
    /// Removes current template.
    /// </summary>
    public ICommand RemoveTemplate { get; }

    public override string Header => Resources.NewPackage;

    /// <summary>
    /// Available package templates.
    /// </summary>
    public ObservableCollection<PackageTemplate> Templates { get; }

    /// <summary>
    /// Currently selected template.
    /// </summary>
    public PackageTemplate CurrentTemplate
    {
        get => _currentTemplate;
        set
        {
            if (_currentTemplate != value)
            {
                _currentTemplate = value;
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

    private bool _qualityControl;

    public bool QualityControl
    {
        get => _qualityControl;
        set
        {
            if (_qualityControl != value)
            {
                _qualityControl = value;
                OnPropertyChanged();
            }
        }
    }

    public CustomPackageOptions CustomPackageOptions { get; } = new();

    /// <summary>
    /// View model errors.
    /// </summary>
    public List<string> Errors { get; } = new();

    private readonly IPackageTemplatesRepository _packageTemplatesRepository;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IDocumentViewModelFactory _documentViewModelFactory;
    private readonly ILogger<NewViewModel> _logger;

    public NewViewModel(
        AppSettings appSettings,
        IPackageTemplatesRepository packageTemplatesRepository,
        IDocumentViewModelFactory documentViewModelFactory,
        ILoggerFactory loggerFactory)
    {
        _appSettings = appSettings;
        _qualityControl = appSettings.UseQualityControl;
        _packageTemplatesRepository = packageTemplatesRepository;
        _documentViewModelFactory = documentViewModelFactory;
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger<NewViewModel>();

        Templates = CreateStandardTemplates();

        foreach (var template in _packageTemplatesRepository.Templates)
        {
            Templates.Add(template);
        }

        _currentTemplate = Templates[0];

        Create = new SimpleCommand(Create_Executed);
        RemoveTemplate = new SimpleCommand(RemoveTemplate_Executed);
    }

    private static ObservableCollection<PackageTemplate> CreateStandardTemplates() => new()
    {
        new PackageTemplate
        {
            Name = Resources.PackageClassicName,
            Description = Resources.PackageClassicDescription,
            Type = PackageType.Classic
        },
        new PackageTemplate
        {
            Name = Resources.PackageSpecialName,
            Description = Resources.PackageSpecialDescription,
            Type = PackageType.Custom
        },
        new PackageTemplate
        {
            Name = Resources.PackageThemesCollectionName,
            Description = Resources.PackageThemesCollectionDescription,
            Type = PackageType.ThemesCollection
        },
        new PackageTemplate
        {
            Name = Resources.PackageDemoName,
            Description = Resources.PackageDemoDescription,
            Type = PackageType.Demo
        },
        new PackageTemplate
        {
            Name = Resources.PackageEmptyName,
            Description = Resources.PackageEmptyDescription,
            Type = PackageType.Empty
        },
    };

    private void Create_Executed(object? arg)
    {
        try
        {
            SIDocument siDocument;

            if (_currentTemplate.FileName != null)
            {
                siDocument = CreateFromCustomTemplate(_currentTemplate.FileName);
            }
            else
            {
                siDocument = CreateFromStandardTemplate();
            }

            _appSettings.UseQualityControl = _qualityControl;
            OnNewItem(_documentViewModelFactory.CreateViewModelFor(siDocument));
            _logger.LogInformation("New document created. Name: {name}", siDocument.Package.Name);
            OnClosed();
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
    }

    private void RemoveTemplate_Executed(object? arg)
    {
        try
        {
            if (CurrentTemplate.FileName != null
                && PlatformManager.Instance.Confirm(string.Format(Resources.RemoveTemplateConfirm, CurrentTemplate.Name)))
            {
                var templateFile = Path.Combine(IPackageTemplatesRepository.TemplateFolder, CurrentTemplate.FileName);
                File.Delete(templateFile);

                _packageTemplatesRepository.RemoveTemplate(CurrentTemplate);
                Templates.Remove(CurrentTemplate);
            }
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
    }

    private SIDocument CreateFromStandardTemplate()
    {
        var doc = SIDocument.Create(_packageName, _packageAuthor);

        switch (_currentTemplate.Type)
        {
            case PackageType.Classic:
                CreateCustomPackage(doc, new CustomPackageOptions());
                break;

            case PackageType.Custom:
                CreateCustomPackage(doc, CustomPackageOptions);
                break;

            case PackageType.ThemesCollection:
                doc.Package.CreateRound(RoundTypes.Standart, Resources.ThemesCollection);
                break;

            case PackageType.Demo:
                var templateName = AppSettings.Default.Language == "ru-RU" ? "SIGameTestNew.siq" : "SIGameTestEn.siq";
                var customTemplatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates", templateName);
                return CreateFromCustomTemplate(customTemplatePath);

            case PackageType.Empty:
            default:
                break;
        }

        doc.Package.HasQualityControl = _qualityControl;
        doc.Package.Language = _appSettings.Language ?? "";
        return doc;
    }

    private static void CreateCustomPackage(SIDocument doc, CustomPackageOptions options)
    {
        for (int i = 0; i < options.RoundCount; i++)
        {
            var round = doc.Package.CreateRound(RoundTypes.Standart, null);

            for (int j = 0; j < options.ThemeCount; j++)
            {
                var theme = round.CreateTheme(null);

                for (int k = 0; k < options.QuestionCount; k++)
                {
                    theme.CreateQuestion(options.BaseQuestionPrice * (i + 1) * (k + 1));
                }
            }
        }

        if (options.HasFinal)
        {
            var final = doc.Package.CreateRound(RoundTypes.Final, Resources.FinalName);

            for (int j = 0; j < options.FinalThemeCount; j++)
            {
                final.CreateTheme(null).CreateQuestion(0);
            }
        }
    }

    private SIDocument CreateFromCustomTemplate(string filePath)
    {
        FileStream? stream = null;

        try
        {
            stream = File.OpenRead(Path.Combine(IPackageTemplatesRepository.TemplateFolder, filePath));
            var document = SIDocument.Load(stream);

            document.Package.Name = _packageName;
            document.Package.Info.Authors.Clear();
            document.Package.Info.Authors.Add(_packageAuthor);

            return document;
        }
        catch
        {
            stream?.Dispose();
            throw;
        }
    }
}
