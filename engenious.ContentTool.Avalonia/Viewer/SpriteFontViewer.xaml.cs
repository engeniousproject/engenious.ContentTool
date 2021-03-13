using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using DynamicData;
using engenious.ContentTool.Models;
using engenious.ContentTool.Models.History;
using engenious.ContentTool.Observer;
using engenious.ContentTool.Viewer;
using engenious.Graphics;
using engenious.Pipeline;
using JetBrains.Annotations;
using ReactiveUI;

namespace engenious.ContentTool.Avalonia
{
    [ViewerInfo(".spritefont", false)]
    public class SpriteFontViewer : UserControl, IViewer
    {
        private readonly ListBox list_characterRegions;
        public static readonly DirectProperty<SpriteFontViewer, bool> IsBoldProperty =
            AvaloniaProperty.RegisterDirect<SpriteFontViewer, bool>(
                nameof(IsBold),
                o => o.IsBold,
                (o, v) => o.IsBold = v);
        public static readonly DirectProperty<SpriteFontViewer, bool> IsItalicProperty =
            AvaloniaProperty.RegisterDirect<SpriteFontViewer, bool>(
                nameof(IsItalic),
                o => o.IsItalic,
                (o, v) => o.IsItalic = v);
        public static readonly DirectProperty<SpriteFontViewer, bool> UseKerningProperty =
            AvaloniaProperty.RegisterDirect<SpriteFontViewer, bool>(
                nameof(UseKerning),
                o => o.UseKerning,
                (o, v) => o.UseKerning = v);
        public static readonly DirectProperty<SpriteFontViewer, int> SpriteFontSizeProperty =
            AvaloniaProperty.RegisterDirect<SpriteFontViewer, int>(
                nameof(SpriteFontSize),
                o => o.SpriteFontSize,
                (o, v) => o.SpriteFontSize = v);
        public static readonly DirectProperty<SpriteFontViewer, int> SpacingProperty =
            AvaloniaProperty.RegisterDirect<SpriteFontViewer, int>(
                nameof(Spacing),
                o => o.Spacing,
                (o, v) => o.Spacing = v);
        public static readonly DirectProperty<SpriteFontViewer, FontFamily> SpriteFontFamilyProperty =
            AvaloniaProperty.RegisterDirect<SpriteFontViewer, FontFamily>(
                nameof(SpriteFontFamily),
                o => o.SpriteFontFamily,
                (o, v) => o.SpriteFontFamily = v);
        public static readonly DirectProperty<SpriteFontViewer, SpriteFontType> SpriteFontTypeProperty =
            AvaloniaProperty.RegisterDirect<SpriteFontViewer, SpriteFontType>(
                nameof(SpriteFontType),
                o => o.SpriteFontType,
                (o, v) => o.SpriteFontType = v);

        private int _spacing;
        private bool _isBold,_isItalic, _useKerning;
        private FontFamily _spriteFontFamily;

        private bool _isLoading;

        public FontFamily SpriteFontFamily
        {
            get => _spriteFontFamily;
            set
            {
                var old = _spriteFontFamily?.Name;
                SetAndRaise(SpriteFontFamilyProperty, ref _spriteFontFamily, value);

                if (_isLoading) return;
                _spf.FontName = value.Name;

                History.Push(new HistoryPropertyChange(_spf, nameof(_spf.FontName), old, _spf.FontName));
            }
        }
        
        public List<SpriteFontType> AvailableSpriteFontTypes { get; }
        
        public SpriteFontType SpriteFontType
        {
            get => _spriteFontType;
            set
            {
                var old = _spriteFontFamily?.Name;
                SetAndRaise(SpriteFontTypeProperty, ref _spriteFontType, value);

                if (_isLoading) return;
                _spf.FontType = value;

                History.Push(new HistoryPropertyChange(_spf, nameof(_spf.FontType), old, _spf.FontType));
            }
        }

        public bool UseKerning
        {
            get => _useKerning;
            set
            {
                var old = _useKerning;
                SetAndRaise(UseKerningProperty, ref _useKerning, value);
                if (_isLoading) return;
                _spf.UseKerning = value;
                History.Push(new HistoryPropertyChange(_spf, nameof(_spf.UseKerning), old, _spf.UseKerning));
            }
        }

        public int Spacing
        {
            get => _spacing;
            set
            {
                var old = _spacing;
                SetAndRaise(SpacingProperty, ref _spacing, value);
                if (_isLoading) return;
                _spf.Spacing = value;
                History.Push(new HistoryPropertyChange(_spf, nameof(_spf.Spacing), old, _spf.Spacing));
            }
        }

        public bool IsBold
        {
            get => _isBold;
            set
            {
                SetAndRaise(IsBoldProperty, ref _isBold, value);
                if (value)
                {
                    SpriteFontWeight = FontWeight.Bold;
                }
                else
                {
                    SpriteFontWeight = FontWeight.Normal;
                }

                UpdateFontStyle();
            }
        }
        public bool IsItalic
        {
            get => _isItalic;
            set
            {
                SetAndRaise(IsItalicProperty, ref _isItalic, value);
                if (value)
                {
                    SpriteFontStyle = FontStyle.Italic;
                }
                else
                {
                    SpriteFontStyle = FontStyle.Normal;
                }

                UpdateFontStyle();
            }
        }

        private void UpdateFontStyle()
        {
            if (_isLoading) return;
            var old = _spf.Style;
            var fontStyle = System.Drawing.FontStyle.Regular;
            if (IsBold)
                fontStyle |= System.Drawing.FontStyle.Bold;
            if (IsItalic)
                fontStyle |= System.Drawing.FontStyle.Italic;

            _spf.Style = fontStyle;
            History.Push(new HistoryPropertyChange(_spf, nameof(_spf.Style), old, _spf.Style));
        }
        
        private int _spriteFontSize;

        public int SpriteFontSize
        {
            get => _spriteFontSize;
            set
            {
                var old = _spriteFontSize;
                SetAndRaise(SpriteFontSizeProperty, ref _spriteFontSize, value);
                if (_isLoading) return;
                _spf.Size = value;
                History.Push(new HistoryPropertyChange(_spf, nameof(_spf.Size), old, _spf.Size));
            }
        }
        public static readonly DirectProperty<SpriteFontViewer, FontWeight> SpriteFontWeightProperty =
            AvaloniaProperty.RegisterDirect<SpriteFontViewer, FontWeight>(
                nameof(SpriteFontWeight),
                o => o.SpriteFontWeight,
                (o, v) => o.SpriteFontWeight = v);
        public static readonly DirectProperty<SpriteFontViewer, FontStyle> SpriteFontStyleProperty =
            AvaloniaProperty.RegisterDirect<SpriteFontViewer, FontStyle>(
                nameof(SpriteFontStyle),
                o => o.SpriteFontStyle,
                (o, v) => o.SpriteFontStyle = v);
        private FontWeight _spriteFontWeight = FontWeight.Normal;

        private FontStyle _spriteFontStyle = FontStyle.Normal;
        public FontStyle SpriteFontStyle
        {
            get => _spriteFontStyle;
            set => SetAndRaise(SpriteFontStyleProperty, ref _spriteFontStyle, value);
        }



        public FontWeight SpriteFontWeight
        {
            get => _spriteFontWeight;
            set => SetAndRaise(SpriteFontWeightProperty, ref _spriteFontWeight, value);
        }
        
        private SpriteFontContent _spf;
        private readonly Dictionary<CharacterRegion, string> _specialRegions;
        private SpriteFontType _spriteFontType;

        public ObservableCollection<FontFamily> FamilyNames { get; }
        
        public ObservableCollection<string> CharacterRegions { get; }


        private void LoadFonts()
        {
            var tmp = FontManager.Current.GetInstalledFontFamilyNames(true).Select(x => new FontFamily(x)).ToArray();
            FamilyNames.AddRange(tmp);
        }
        

        public SpriteFontViewer()
        {
            AvailableSpriteFontTypes = Enum.GetValues<SpriteFontType>().Distinct().ToList();
                
            CharacterRegions = new ObservableCollection<string>();
            FamilyNames = new ObservableCollection<FontFamily>();
            LoadFonts();

            DataContext = this;
            InitializeComponent();
            
            _specialRegions = new Dictionary<CharacterRegion, string>
            {
                {new CharacterRegion(32, 126), "latin alphabet"},
                {new CharacterRegion(228, 228), "character \"ä\""},
                {new CharacterRegion(246, 246), "character \"ö\""},
                {new CharacterRegion(252, 252), "character \"ü\""},
                {new CharacterRegion(196, 196), "character \"Ä\""},
                {new CharacterRegion(214, 214), "character \"Ö\""},
                {new CharacterRegion(220, 220), "character \"Ü\""},
                {new CharacterRegion(223, 223), "character \"ß\""}
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
        }

        public object GetViewerControl(ContentFile file)
        {
            History = new History();
            UnsavedChanges = false;
            History.HistoryChanged += (sender, args) => UnsavedChanges = History.CanUndo || History.CanRedo;
            
            ContentFile = file;
            _spf = new SpriteFontContent(file.FilePath);

            _isLoading = true;

            SpriteFontFamily = FontFamily.Parse(_spf.FontName);
            SpriteFontType = _spf.FontType;
            IsItalic = _spf.Style.HasFlag(System.Drawing.FontStyle.Italic);
            IsBold = _spf.Style.HasFlag(System.Drawing.FontStyle.Bold);
            SpriteFontSize = _spf.Size;
            Spacing = _spf.Spacing;
            UseKerning = _spf.UseKerning;
            //var sel = list_characterRegions.SelectedIndex;
            CharacterRegions.Clear();
            foreach (var region in _spf.CharacterRegions)
            {
                if (_specialRegions.TryGetValue(region, out var spec))
                    CharacterRegions.Add($"{spec} ({region.Start} - {region.End})");
                else
                    CharacterRegions.Add($"{region.Start} - {region.End}");
            }
            //list_characterRegions.SelectedIndex = sel;
            
            _isLoading = false;
            
            return this;
        }

        public void Save()
        {
            _spf.Save(ContentFile.FilePath);
        }

        public void Discard()
        {
            UnsavedChanges = false;
        }

        public IHistory History { get; private set; }
        public bool UnsavedChanges { get; private set; }
        public ContentFile ContentFile { get; private set; }

        public void Dispose()
        {
        }
    }
}