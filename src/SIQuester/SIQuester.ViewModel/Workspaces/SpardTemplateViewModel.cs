using SIQuester.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace SIQuester.ViewModel
{
    public sealed class SpardTemplateViewModel : ModelViewBase
    {
        public string Name { get; set; }
        public bool NonStandartOnly { get; set; }
        public Dictionary<string, EditAlias> Aliases { get; private set; }

        private bool _enabled = true;

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _transform = "";

        public string Transform
        {
            get => _transform;
            set { if (_transform != value) { _transform = value; OnPropertyChanged(); } }
        }

        private bool _canChange;

        public bool CanChange
        {
            get { return _canChange; }
            set { if (_canChange != value) { _canChange = value; OnPropertyChanged(); } }
        }

        private ObservableCollection<string> _variants;

        public ObservableCollection<string> Variants
        {
            get { return _variants; }
            set { _variants = value; OnPropertyChanged(); }
        }

        public ICommand Cut { get; private set; }
        public ICommand Copy { get; private set; }
        public ICommand Paste { get; private set; }

        public ICommand InsertAlias { get; private set; }
        public ICommand InsertOptional { get; private set; }

        public ICommand ChangeTemplate { get; private set; }

        public event Action<string> AliasInserted;
        public event Action OptionalInserted;

        public SpardTemplateViewModel()
        {
            Aliases = new Dictionary<string, EditAlias>();

            Cut = new SimpleCommand(
                arg =>
                {
                    if (_transform != null)
                    {
                        Clipboard.SetData(DataFormats.UnicodeText, _transform);
                        Transform = "";
                    }
                });

            Copy = new SimpleCommand(
                arg =>
                {
                    if (_transform != null)
                        Clipboard.SetData(DataFormats.UnicodeText, _transform);
                });

            Paste = new SimpleCommand(
                arg =>
                {
                    Transform = (string)Clipboard.GetData(DataFormats.UnicodeText);
                });

            InsertAlias = new SimpleCommand(
                arg =>
                {
                    AliasInserted?.Invoke(arg.ToString());
                });

            InsertOptional = new SimpleCommand(
                arg =>
                {
                    OptionalInserted?.Invoke();
                });

            ChangeTemplate = new SimpleCommand(
                template =>
                {
                    Transform = template.ToString();
                });
        }
    }
}
