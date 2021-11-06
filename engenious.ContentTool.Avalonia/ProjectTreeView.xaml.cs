using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using engenious.Content.Models;
using engenious.ContentTool.Observer;
using JetBrains.Annotations;

namespace engenious.ContentTool.Avalonia
{
    internal class IsContentProjectConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool))
                return null;
            return value is not ContentProject;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ProjectTreeView : UserControl
    {
        public static readonly DirectProperty<ProjectTreeView, ICommand> RenameItemCommandProperty =
            AvaloniaProperty.RegisterDirect<ProjectTreeView, ICommand>(
                nameof(RenameItemCommand),
                o => o.RenameItemCommand,
                (o, v) => o.RenameItemCommand = v);

        public static readonly DirectProperty<ProjectTreeView, ICommand> RemoveItemCommandProperty =
            AvaloniaProperty.RegisterDirect<ProjectTreeView, ICommand>(
                nameof(RemoveItemCommand),
                o => o.RemoveItemCommand,
                (o, v) => o.RemoveItemCommand = v);

        public static readonly DirectProperty<ProjectTreeView, ICommand> BuildItemCommandProperty =
            AvaloniaProperty.RegisterDirect<ProjectTreeView, ICommand>(
                nameof(BuildItemCommand),
                o => o.BuildItemCommand,
                (o, v) => o.BuildItemCommand = v);

        public static readonly DirectProperty<ProjectTreeView, ICommand> AddNewFileCommandProperty =
            AvaloniaProperty.RegisterDirect<ProjectTreeView, ICommand>(
                nameof(AddNewFileCommand),
                o => o.AddNewFileCommand,
                (o, v) => o.AddNewFileCommand = v);

        public static readonly DirectProperty<ProjectTreeView, ICommand> AddExistingFileCommandProperty =
            AvaloniaProperty.RegisterDirect<ProjectTreeView, ICommand>(
                nameof(AddExistingFileCommand),
                o => o.AddExistingFileCommand,
                (o, v) => o.AddExistingFileCommand = v);

        public static readonly DirectProperty<ProjectTreeView, ICommand> AddNewFolderCommandProperty =
            AvaloniaProperty.RegisterDirect<ProjectTreeView, ICommand>(
                nameof(AddNewFolderCommand),
                o => o.AddNewFolderCommand,
                (o, v) => o.AddNewFolderCommand = v);

        public static readonly DirectProperty<ProjectTreeView, ICommand> AddExistingFolderCommandProperty =
            AvaloniaProperty.RegisterDirect<ProjectTreeView, ICommand>(
                nameof(AddExistingFolderCommand),
                o => o.AddExistingFolderCommand,
                (o, v) => o.AddExistingFolderCommand = v);

        private ICommand _renameItemCommand, _removeItemCommand, _buildItemCommand, _addNewFileCommand;
        private ICommand _addExistingFileCommand, _addNewFolderCommand, _addExistingFolderCommand;

        public ICommand RenameItemCommand
        {
            get => _renameItemCommand;
            set => SetAndRaise(RenameItemCommandProperty, ref _renameItemCommand, value);
        }

        public ICommand RemoveItemCommand
        {
            get => _removeItemCommand;
            set => SetAndRaise(RemoveItemCommandProperty, ref _removeItemCommand, value);
        }
        public ICommand BuildItemCommand
        {
            get => _buildItemCommand;
            set => SetAndRaise(BuildItemCommandProperty, ref _buildItemCommand, value);
        }

        public ICommand AddNewFileCommand
        {
            get => _addNewFileCommand;
            set => SetAndRaise(AddNewFileCommandProperty, ref _addNewFileCommand, value);
        }

        public ICommand AddExistingFileCommand
        {
            get => _addExistingFileCommand;
            set => SetAndRaise(AddExistingFileCommandProperty, ref _addExistingFileCommand, value);
        }

        public ICommand AddNewFolderCommand
        {
            get => _addNewFolderCommand;
            set => SetAndRaise(AddNewFolderCommandProperty, ref _addNewFolderCommand, value);
        }

        public ICommand AddExistingFolderCommand
        {
            get => _addExistingFolderCommand;
            set => SetAndRaise(AddExistingFolderCommandProperty, ref _addExistingFolderCommand, value);
        }
        public event EventHandler<RoutedEventArgs> SelectedItemChanged;
        public event EventHandler<PropertyValueChangedEventArgs> SelectedItemPropertyChanged;

        public static readonly DirectProperty<ProjectTreeView, ContentProject> ProjectProperty =
            AvaloniaProperty.RegisterDirect<ProjectTreeView, ContentProject>(
                nameof(Project),
                o => o.Project,
                (o, v) => o.Project = v);
        public static readonly DirectProperty<ProjectTreeView, bool> IsInEditModeProperty =
            AvaloniaProperty.RegisterDirect<ProjectTreeView, bool>(
                nameof(IsInEditMode),
                o => o.IsInEditMode,
                (o, v) => o.IsInEditMode = v);

        private readonly TreeView _treeView;
        private readonly ContextMenu _projectContextMenu;

        private ContentProject _project;
        private List<Tuple<object, NotifyCollectionChangedEventArgs>> _changes;


        private bool _isInEditMode;

        public bool IsInEditMode
        {
            get => _isInEditMode;
            set
            {
                if (_isInEditMode == value)
                    return;
                SetAndRaise(IsInEditModeProperty, ref _isInEditMode, value);
            }
        }

        public ContentFolder SelectedFolder =>
            (SelectedItem as ContentFolder) ?? (SelectedItem?.Parent as ContentFolder) ?? Project;

        public ContentItem SelectedItem
        {
            get => _treeView.SelectedItem as ContentItem;
            set => _treeView.SelectedItem = value;
        }

        public ContentProject Project
        {
            get => _project;
            set => SetAndRaise(ProjectProperty, ref _project, value);
        }

        public ProjectTreeView()
        {
            DataContext = this;
            InitializeComponent();
            _treeView = this.FindControl<TreeView>("treeView");
            _projectContextMenu = this.FindControl<ContextMenu>("ProjectContextMenu");

            _treeView.AddHandler(DragDrop.DragOverEvent, DragOver);
            _treeView.AddHandler(DragDrop.DragEnterEvent, DragEnter);
            _treeView.AddHandler(DragDrop.DropEvent, Drop);
            DragDrop.SetAllowDrop(_treeView, true);
        }

        private void Drop(object sender, DragEventArgs e)
        {
            var fileNames = e.Data.Get(DataFormats.FileNames) as List<string>;

            if (fileNames is null)
                return;

            ;
        }

        private void DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.FileNames))
            {
                e.DragEffects = DragDropEffects.Copy;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private void DragOver(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.FileNames))
            {
                e.DragEffects = DragDropEffects.Copy;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private INotifyPropertyValueChanged _oldSelectedItem;
        private void OnSelectedItemChanged(object sender, SelectionChangedEventArgs e)
        {
            IsInEditMode = false;
            SelectedItemChanged?.Invoke(this, e);

            if (_oldSelectedItem != null)
                _oldSelectedItem.PropertyValueChanged -= SelectedItemOnPropertyChanged;

            if (SelectedItem != null)
                ((INotifyPropertyValueChanged)SelectedItem).PropertyValueChanged += SelectedItemOnPropertyChanged;

            _oldSelectedItem = SelectedItem;
        }

        private void SelectedItemOnPropertyChanged(object sender, PropertyValueChangedEventArgs args)
        {
            SelectedItemPropertyChanged?.Invoke(SelectedItem, args);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

        }

        private void TreeView_OnDoubleTapped(object? sender, RoutedEventArgs e)
        {
            IsInEditMode = SelectedItem != null;
        }

        private void UpdateValue(TextBox editField, ContentItem contentItem)
        {
            if (_commitEdit)
                contentItem.Name = editField.Text;
            else
                editField.Text = contentItem.Name;
            IsInEditMode = false;
            _commitEdit = true;
        }

        private void EditField_OnLostFocus(object? sender, RoutedEventArgs e)
        {
            if (sender == null)
                return;
            var editField = (TextBox)sender;
            var contentItem = (ContentItem)editField.DataContext;
            if (contentItem == null)
                return;
            UpdateValue(editField, contentItem);
        }

        private bool _commitEdit = true;
        private void EditField_OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (sender == null || e.Property.Name != "IsVisible")
                return;
            var editField = (TextBox)sender;
            if (editField.IsVisible)
                editField.Focus();
            else
            {
                var contentItem = (ContentItem)editField.DataContext;

                UpdateValue(editField, contentItem);
            }
        }

        private void EditField_OnKeyUp(object? sender, KeyEventArgs e)
        {
            if (sender == null)
                return;
            if (e.Key == Key.Escape)
            {
                _commitEdit = false;
                IsInEditMode = false;
            }
            else if (e.Key == Key.Enter)
            {
                IsInEditMode = false;
            }
            //var editField = (TextBox) sender;

        }

        private ContextMenu GetParentMenu(MenuItem item)
        {
            while (item != null)
            {
                var parent = item.Parent;
                if (parent is ContextMenu contextMenu)
                    return contextMenu;
                item = parent as MenuItem;
            }

            return null;
        }

        private void AddNewFile_OnClick(object? sender, RoutedEventArgs e)
        {
            AddNewFileCommand?.Execute(null);
        }

        private void AddExistingFile_OnClick(object? sender, RoutedEventArgs e)
        {
            AddExistingFileCommand?.Execute(null);
        }

        private void AddNewFolder_OnClick(object? sender, RoutedEventArgs e)
        {
            AddNewFolderCommand?.Execute(null);
        }

        private void AddExistingFolder_OnClick(object? sender, RoutedEventArgs e)
        {
            AddExistingFolderCommand?.Execute(null);
        }

        private void RemoveItem_OnClick(object? sender, RoutedEventArgs e)
        {
            RemoveItemCommand?.Execute(null);
        }

        private void RenameItem_OnClick(object? sender, RoutedEventArgs e)
        {
            GetParentMenu((MenuItem)sender)?.Close();
            RenameItemCommand?.Execute(null);
        }

        private void BuildItem_OnClick(object? sender, RoutedEventArgs e)
        {
            BuildItemCommand?.Execute(null);
        }
    }
}