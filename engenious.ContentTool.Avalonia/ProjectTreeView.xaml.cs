using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using engenious.ContentTool.Models;
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
        public event EventHandler<RoutedEventArgs> RenameItemClicked;
        public event EventHandler<RoutedEventArgs> RemoveItemClicked;
        public event EventHandler<RoutedEventArgs> BuildItemClicked;
        
        public event EventHandler<RoutedEventArgs> AddNewFileClicked;
        public event EventHandler<RoutedEventArgs> AddExistingFileClicked;
        public event EventHandler<RoutedEventArgs> AddNewFolderClicked;
        public event EventHandler<RoutedEventArgs> AddExistingFolderClicked;
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
            var editField = (TextBox) sender;
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
            var editField = (TextBox) sender;
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
            AddNewFileClicked?.Invoke(sender, e);
        }

        private void AddExistingFile_OnClick(object? sender, RoutedEventArgs e)
        {
            AddExistingFileClicked?.Invoke(sender, e);
        }

        private void AddNewFolder_OnClick(object? sender, RoutedEventArgs e)
        {
            AddNewFolderClicked?.Invoke(sender, e);
        }

        private void AddExistingFolder_OnClick(object? sender, RoutedEventArgs e)
        {
            AddExistingFolderClicked?.Invoke(sender, e);
        }

        private void RemoveItem_OnClick(object? sender, RoutedEventArgs e)
        {
            RemoveItemClicked?.Invoke(sender, e);
        }

        private void RenameItem_OnClick(object? sender, RoutedEventArgs e)
        {
            GetParentMenu((MenuItem)sender)?.Close();
            RenameItemClicked?.Invoke(sender, e);
        }

        private void BuildItem_OnClick(object? sender, RoutedEventArgs e)
        {
            BuildItemClicked?.Invoke(sender, e);
        }
    }
}