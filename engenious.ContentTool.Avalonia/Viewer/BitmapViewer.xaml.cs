using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using engenious.ContentTool.Models;
using engenious.ContentTool.Models.History;
using engenious.ContentTool.Observer;
using engenious.ContentTool.Viewer;
using JetBrains.Annotations;
using ReactiveUI;

namespace engenious.ContentTool.Avalonia
{
    [ViewerInfo(".png", false)]
    [ViewerInfo(".bmp", false)]
    [ViewerInfo(".jpg", false)]
    public class BitmapViewer : UserControl, IViewer
    {
        public static readonly DirectProperty<BitmapViewer, IBitmap> ImageSourceProperty =
            AvaloniaProperty.RegisterDirect<BitmapViewer, IBitmap>(
                nameof(ImageSource),
                o => o.ImageSource,
                (o, v) => o.ImageSource = v);
        public static readonly DirectProperty<BitmapViewer, global::Avalonia.Size> MaxImageSizeProperty =
            AvaloniaProperty.RegisterDirect<BitmapViewer, global::Avalonia.Size>(
                nameof(MaxImageSize),
                o => o.MaxImageSize,
                (o, v) => o.MaxImageSize = v);
        
        private IBitmap _imageSource;
        private global::Avalonia.Size _maxImageSize;
        public global::Avalonia.Size MaxImageSize
        {
            get => _maxImageSize;
            set => SetAndRaise(MaxImageSizeProperty, ref _maxImageSize, value);
        }
        public IBitmap ImageSource
        {
            get => _imageSource;
            set => SetAndRaise(ImageSourceProperty, ref _imageSource, value);
        }


        public BitmapViewer()
        {
            DataContext = this;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
        }

        public object GetViewerControl(ContentFile file)
        {
            History = new History();
            ContentFile = file;
            try
            {
                ImageSource = new Bitmap(file.FilePath);
                MaxImageSize = ImageSource.Size;
            }
            catch (FileNotFoundException)
            {
                ImageSource = null;
            }
            return this;
        }

        public void Save()
        {
            throw new NotImplementedException();
        }

        public void Discard()
        {
            throw new NotImplementedException();
        }

        public IHistory History { get; private set; }
        public bool UnsavedChanges => false;
        public ContentFile ContentFile { get; private set; }

        public void Dispose()
        {
            _imageSource?.Dispose();
        }
    }
}