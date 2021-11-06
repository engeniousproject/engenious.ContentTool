using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
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

        public event EventHandler<(int index, ContentFolder folder, string newFolder)> AddSpecificFolder;
        public event EventHandler<(int index, ContentFolder folder, string[] newFiles)> AddSpecificFiles;
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

        private Pen _pen = new Pen(new SolidColorBrush(Colors.Black));
        public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (_dropPoint.Y == 0)
                return;
            
            context.DrawLine(_pen, _dropPoint, new global::Avalonia.Point(_treeView.Bounds.Width - _treeView.Padding.Right,_dropPoint.Y));
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
            
            
            _treeView.AddHandler(TreeViewItem.PointerPressedEvent,TreeViewOnPointerPressed, RoutingStrategies.Tunnel);
            _treeView.AddHandler(TreeViewItem.PointerMovedEvent,TreeViewOnPointerMoved, RoutingStrategies.Tunnel);
            _treeView.AddHandler(TreeViewItem.PointerReleasedEvent,TreeViewOnPointerReleased, RoutingStrategies.Tunnel);
        }

        private bool _canDrag, _isDragging;

        private void TreeViewOnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _isDragging = _canDrag = false;
        }

        private void TreeViewOnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var p = e.GetCurrentPoint(this);
            if (p.Properties.IsLeftButtonPressed && _treeView.SelectedItems.Count > 0)
            {
                _canDrag = !IsInEditMode;
            }
        }

        private const string DragFormat = "engenious.ContentTool.ContentFiles";

        private async void TreeViewOnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_canDrag && !_isDragging)
            {
                _isDragging = true;

                var d = new DataObject();
                
                d.Set(DragFormat, SelectedItem);

                await DragDrop.DoDragDrop(e, d, DragDropEffects.Move);
            }
        }

        private void DropFiles(List<string> files)
        {
            var dropIndex = _dropIndex;
            for (int i = files.Count - 1; i >= 0; i--)
            {
                if (Directory.Exists(files[i]))
                {
                    AddSpecificFolder?.Invoke(this, (dropIndex++, _dropItemParent, files[i]));
                    files.RemoveAt(i);
                }
            }
            AddSpecificFiles?.Invoke(this, (dropIndex, _dropItemParent, files.ToArray()));
        }

        private void DropContent(ContentItem c)
        {
            if (c.Parent is not ContentFolder parentFolder)
                return;
            var oldParent = c.Parent;
            var oldPath = c.FilePath;
            var oldIndex = parentFolder.Content.IndexOf(c);
            if (oldIndex == -1)
                return;

            parentFolder.Content.RemoveAt(oldIndex);
            var dropIndex = _dropIndex;
            if (dropIndex > oldIndex)
                dropIndex--;
            
            _dropItemParent.Content.Insert(dropIndex, c);
            c.Parent = _dropItemParent;

            if (!ReferenceEquals(oldParent, c.Parent))
            {
                // TODO: error handling if destination file/directory already exists
                if (Directory.Exists(oldPath))
                {
                    Directory.Move(oldPath, c.FilePath);
                }
                else if (File.Exists(oldPath))
                {
                    File.Move(oldPath, c.FilePath);
                }
            }
        }
        
        private void Drop(object sender, DragEventArgs e)
        {
            if (e.Data.Get(DataFormats.FileNames) is List<string> fileNames)
                DropFiles(fileNames);
            else if (e.Data.Get(DragFormat) is ContentItem contentFileMove)
                DropContent(contentFileMove);
            ResetDropPoint();
        }

        private void ResetDropPoint()
        {
            _dropIndex = -1;
            _dropItemParent = null;
            _dropPoint = new global::Avalonia.Point();
            InvalidateVisual();
        }

        private void ApplyDragMatch(DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.FileNames))
                e.DragEffects = DragDropEffects.Copy;
            else if (e.Data.Contains(DragFormat))
                e.DragEffects = DragDropEffects.Move;
            else
                e.DragEffects = DragDropEffects.None;

            e.Handled = true;
        }

        private void DragEnter(object sender, DragEventArgs e)
        {
            if (IsInEditMode)
                return;
            ApplyDragMatch(e);
        }


        private TreeViewItem GetTreeViewItem(IVisual el)
        {
            while (true)
            {
                if (el is TreeViewItem treeViewItem)
                    return treeViewItem;
                if (el.VisualParent == null)
                    return null;
                el = el.VisualParent;
            }
        }

        private int IndexInParent(TreeViewItem item, DragEventArgs e)
        {
            var p = item?.Parent;
            if (p == null || item.DataContext is not ContentItem contentItem)
                return -1;
            if (p is not TreeViewItem parentItem)
                return 0;

            if (parentItem.Items is not ContentItemCollection cI)
                return -1;

            var ind = cI.IndexOf(contentItem);
            if (ind == -1)
                return -1;

            return ind;
        }

        private global::Avalonia.Point _dropPoint;
        private int _dropIndex;
        private ContentFolder _dropItemParent;
        
        private void DragOver(object? sender, DragEventArgs e)
        {
            if (IsInEditMode)
                return;
            ApplyDragMatch(e);
            var p = e.GetPosition(_treeView);
            var el = GetTreeViewItem(_treeView.InputHitTest(p));

            if (el?.DataContext is ContentProject)
            {
                el = el.GetLogicalChildren().OfType<TreeViewItem>().FirstOrDefault();
            }
            
            var i = IndexInParent(el, e);

            if (i == -1 || el is null)
            {
                ResetDropPoint();
                return;
            }
            
            var boundChild = el.GetLogicalChildren().FirstOrDefault() as IVisual;
            var pt = _treeView.PointToClient(boundChild.PointToScreen(new global::Avalonia.Point()));
            var relPt = e.GetPosition(el);

            pt = new global::Avalonia.Point(pt.X, pt.Y - el.Padding.Bottom);

            var dropParent = ((TreeViewItem)el.Parent).DataContext as ContentFolder;
            if (relPt.Y > el.Bounds.Height / 2)
            {
                if (el.DataContext is ContentFolder cfld && relPt.Y < el.Bounds.Height - el.Padding.Bottom)
                {
                    dropParent = cfld;
                    _dropIndex = 0;
                    pt = new global::Avalonia.Point(pt.X + 5, pt.Y + el.Bounds.Height); // TODO: better indent
                }
                else
                {
                    pt = new global::Avalonia.Point(pt.X, pt.Y + el.Bounds.Height);
                    i++;
                }
            }
            if (pt != _dropPoint)
            {
                _dropPoint = pt;
                _dropIndex = i;
                _dropItemParent = dropParent;
                InvalidateVisual();
            }
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