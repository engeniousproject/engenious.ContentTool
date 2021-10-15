using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using engenious.Content.Models;

namespace engenious.ContentTool.Avalonia
{
    public class GroupBox : UserControl
    {
        public static readonly DirectProperty<GroupBox, object> HeaderProperty =
            AvaloniaProperty.RegisterDirect<GroupBox, object>(
                nameof(Header),
                o => o.Header,
                (o, v) => o.Header = v);

        private object _header;

        public object Header
        {
            get => _header;
            set => SetAndRaise(HeaderProperty, ref _header, value);
        }
        
        public GroupBox()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
        }
    }
}