using System.Buffers;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using engenious.ContentTool.Models;
using engenious.ContentTool.Observer;
using ReactiveUI;

namespace engenious.ContentTool.Avalonia
{
    public class PropertyGrid : UserControl
    {
        public static readonly DirectProperty<PropertyGrid, object> SelectedObjectProperty =
            AvaloniaProperty.RegisterDirect<PropertyGrid, object>(
                nameof(SelectedObject),
                o => o.SelectedObject,
                (o, v) => o.SelectedObject = v);
        
        public static readonly DirectProperty<PropertyGrid, GridLength> Column1WidthProperty =
            AvaloniaProperty.RegisterDirect<PropertyGrid, GridLength>(
                nameof(Column1Width),
                o => o.Column1Width,
                (o, v) => o.Column1Width = v);
        public static readonly DirectProperty<PropertyGrid, PropertyViewBase> PropertyViewProperty =
            AvaloniaProperty.RegisterDirect<PropertyGrid, PropertyViewBase>(
                nameof(PropertyView),
                o => o.PropertyView,
                (o, v) => o.PropertyView = v);

        public GridLength Column1Width
        {
            get => _column1Width;
            set => SetAndRaise(Column1WidthProperty, ref _column1Width, new GridLength(value.Value, GridUnitType.Pixel));
        }


        private object _selectedObject;
        private GridLength _column1Width = new GridLength(0.5, GridUnitType.Star);
        private PropertyViewBase _propertyView;

        public object SelectedObject
        {
            get => _selectedObject;
            set => SetAndRaise(SelectedObjectProperty, ref _selectedObject, value);
        }

        public PropertyViewBase PropertyView
        {
            get => _propertyView;
            set => SetAndRaise(PropertyViewProperty, ref _propertyView, value);
        }

        public PropertyGrid()
        {
            InitializeComponent();
            this.FindControl<ItemsControl>("itemsControl").DataContext = this;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
        }
    }
}