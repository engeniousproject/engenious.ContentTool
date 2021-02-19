using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using engenious.ContentTool.Models;
using engenious.ContentTool.Observer;
using JetBrains.Annotations;
using ReactiveUI;

namespace engenious.ContentTool.Avalonia
{
    public class SingleRootTreeView : TreeView, IStyleable
    {
        public static readonly DirectProperty<SingleRootTreeView, object> RootProperty =
            AvaloniaProperty.RegisterDirect<SingleRootTreeView, object>(
                nameof(Root),
                o => o.Root,
                (o, v) => o.Root = v);

        [CanBeNull] private object _root;
        private readonly ObservableCollection<object> _items;
        
        static SingleRootTreeView()
        {
            RootProperty.Changed.AddClassHandler<SingleRootTreeView>(x => x.RootPropertyChanged);
        }

        private void RootPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (_items.Count == 1)
                _items.RemoveAt(0);
            if (e.NewValue == null)
                return;
            _items.Add(e.NewValue);
            
            if (this.ItemContainerGenerator.ContainerFromIndex(0) is TreeViewItem itemContainer)
                itemContainer.IsExpanded = true;
        }

        public SingleRootTreeView()
        {
            _items = new ObservableCollection<object>();
            Items = _items;
            InitializeComponent();
        }


        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
        }
        
        Type IStyleable.StyleKey => typeof(TreeView);

        [CanBeNull]
        public object Root
        {
            get => _root;
            set => SetAndRaise(RootProperty, ref _root, value);
        }
        
    }
}