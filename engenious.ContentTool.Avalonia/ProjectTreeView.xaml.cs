using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using engenious.ContentTool.Models;
using engenious.ContentTool.Observer;
using JetBrains.Annotations;

namespace engenious.ContentTool.Avalonia
{
    public class ProjectTreeView : UserControl
    {
        public event EventHandler<RoutedEventArgs> SelectedItemChanged;
        
        public static readonly DirectProperty<ProjectTreeView, ContentProject> ProjectProperty =
            AvaloniaProperty.RegisterDirect<ProjectTreeView, ContentProject>(
                nameof(Project),
                o => o.Project,
                (o, v) => o.Project = v);

        public static readonly DirectProperty<ProjectTreeView, ContentFolder> ProjectContainerProperty =
            AvaloniaProperty.RegisterDirect<ProjectTreeView, ContentFolder>(
                nameof(ProjectContainer),
                o => o.ProjectContainer, (o, v) => o.ProjectContainer = v);
        private ContentProject _project;
        private List<Tuple<object, NotifyCollectionChangedEventArgs>> _changes;
        private ContentFolder _projectContainer;

        public ContentFolder SelectedFolder =>
            (SelectedItem as ContentFolder) ?? (SelectedItem?.Parent as ContentFolder) ?? Project;
        
        public ContentItem SelectedItem => _treeView.SelectedItem as ContentItem;

        public ContentFolder ProjectContainer
        {
            get => _projectContainer;
            set => SetAndRaise(ProjectContainerProperty, ref _projectContainer, value);
        }

        public ContentProject Project
        {
            get => _project;
            set
            {
                if (_project == value)
                    return;

                if (value != null)
                {
                    var dummy = new ContentFolder("Project Container Dummy", null);
                    dummy.Content.Add(value);
                    ProjectContainer = dummy;
                }
                else
                {
                    ProjectContainer.Content.Clear();
                }
                
                SetAndRaise(ProjectProperty, ref _project, value);
            }
        }

        private readonly TreeView _treeView;
        public ProjectTreeView()
        {
            DataContext = this;
            InitializeComponent();
            _treeView = this.FindControl<TreeView>("treeView");
        }

        private void OnSelectedItemChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedItemChanged?.Invoke(this, e);
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
        }
    }
}