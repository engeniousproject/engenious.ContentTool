using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using DynamicData;
using DynamicData.Kernel;
using engenious.Avalonia;
using engenious.Content.Models;
using engenious.Content.Models.History;
using engenious.ContentTool.Observer;
using engenious.ContentTool.Viewer;
using engenious.Graphics;
using engenious.Pipeline;
using JetBrains.Annotations;
using ReactiveUI;
using FontStyle = Avalonia.Media.FontStyle;

namespace engenious.ContentTool.Avalonia
{
    [ViewerInfo(".spritefont", false)]
    public class SpriteFontViewer : UserControl, IViewer
    {
        private readonly ListBox list_characterRegions;

        private readonly Popup _characterRegionPopup;

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
        
        public static readonly DirectProperty<SpriteFontViewer, string> ExampleTextProperty =
            AvaloniaProperty.RegisterDirect<SpriteFontViewer, string>(
                nameof(ExampleText),
                o => o.ExampleText,
                (o, v) => o.ExampleText = v);
        public static readonly DirectProperty<SpriteFontViewer, string> ErrorTextProperty =
            AvaloniaProperty.RegisterDirect<SpriteFontViewer, string>(
                nameof(ErrorText),
                o => o.ErrorText,
                (o, v) => o.ErrorText = v);
        public static readonly DirectProperty<SpriteFontViewer, bool> UnsavedChangesProperty =
            AvaloniaProperty.RegisterDirect<SpriteFontViewer, bool>(
                nameof(UnsavedChanges),
                o => o.UnsavedChanges,
                (o, v) => o.UnsavedChanges = v);

        private readonly AvaloniaRenderingSurface _avaloniaRenderingSurface;
        private int _spacing;
        private bool _isBold, _isItalic, _useKerning;
        private FontFamily _spriteFontFamily;

        private bool _isLoading;

        private string _exampleText = "Example Text!";
        public string ExampleText
        {
            get => _exampleText;
            set => SetAndRaise(ExampleTextProperty, ref _exampleText, value);
        }
        private string _errorText = "";
        public string ErrorText
        {
            get => _errorText;
            set => SetAndRaise(ErrorTextProperty, ref _errorText, value);
        }

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
            var fontStyle = engenious.Pipeline.FontStyle.Regular;
            if (IsBold)
                fontStyle |= engenious.Pipeline.FontStyle.Bold;
            if (IsItalic)
                fontStyle |= engenious.Pipeline.FontStyle.Italic;

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
        public static readonly DirectProperty<SpriteFontViewer, CharacterRegionView> SelectedCharacterRegionProperty =
            AvaloniaProperty.RegisterDirect<SpriteFontViewer, CharacterRegionView>(
                nameof(SelectedCharacterRegion),
                o => o.SelectedCharacterRegion,
                (o, v) => o.SelectedCharacterRegion = v);

        public CharacterRegionView SelectedCharacterRegion
        {
            get => _selectedCharacterRegion;
            set => SetAndRaise(SelectedCharacterRegionProperty, ref _selectedCharacterRegion, value);
        }

        private SpriteFontContent _spf;
        private SpriteFontType _spriteFontType;

        public ObservableCollection<FontFamily> FamilyNames { get; }

        public ObservableCollection<CharacterRegionView> CharacterRegions { get; }


        private void LoadFonts()
        {
            var tmp = FontManager.Current.GetInstalledFontFamilyNames(true).Select(x => new FontFamily(x)).ToArray();
            FamilyNames.AddRange(tmp);
        }


        public SpriteFontViewer()
        {
            AvailableSpriteFontTypes = Enum.GetValues<SpriteFontType>().Distinct().ToList();

            CharacterRegions = new ObservableCollection<CharacterRegionView>();
            FamilyNames = new ObservableCollection<FontFamily>();
            LoadFonts();

            DataContext = this;
            InitializeComponent();

            _avaloniaRenderingSurface = this.FindControl<AvaloniaRenderingSurface>("renderingSurface");
            _characterRegionPopup = this.FindControl<Popup>("CharacterRegionPopup");

            var res = GetResource("ThemeForegroundBrush");
            if (res is SolidColorBrush solidColorBrush)
            {
                _exampleTextColor = new Color(solidColorBrush.Color.R, solidColorBrush.Color.G,
                    solidColorBrush.Color.B, solidColorBrush.Color.A);
            }
        }

        [CanBeNull]
        private static object GetResource(object key)
        {
            return GetResource(key, Application.Current.Styles);
        }
        [CanBeNull]
        private static object GetResource(object key, IReadOnlyList<IStyle> styles)
        {
            object? res = null;
            foreach (var s in styles)
            {
                if (s is Style style)
                {
                    if (style.TryGetResource(key, out res))
                        return res;
                }

                var tmp = GetResource(key, s.Children);
                res = tmp ?? res;
            }

            return res;
        }

        private readonly Color _exampleTextColor;
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public class CharacterRegionView : INotifyPropertyChanged
        {
            private static readonly Dictionary<CharacterRegion, string> _specialRegions;
            static CharacterRegionView()
            {
                
                _specialRegions = new Dictionary<CharacterRegion, string>
                                  {
                                      { new CharacterRegion(32, 126), "latin alphabet" },
                                  };
            }
            private CharacterRegion _region;
            private string _specialName;

            public CharacterRegionView(CharacterRegion region)
            {
                Region = region;
            }

            public CharacterRegion Region
            {
                get => _region;
                set
                {
                    _region = value;
                    UpdateRegion();
                }
            }

            private void UpdateRegion()
            {
                if (_region.Start == _region.End)
                {
                    SpecialName = $"character \"{char.ConvertFromUtf32(_region.Start)}\"";
                }
                else
                {
                    _specialRegions.TryGetValue(_region, out var spec);
                    SpecialName = spec;
                }
                
                OnPropertyChanged(nameof(Start));
                OnPropertyChanged(nameof(End));
                OnPropertyChanged(nameof(DisplayString));
            }

            private static CharacterRegion Create(int first, int second)
            {
                int min = Math.Min(first, second);
                int max = Math.Max(first, second);
                return new CharacterRegion(min, max);
            }
            
            public int Start
            {
                get => _region.Start;
                set => Region = Create(value, _region.End);
            }
            public int End
            {
                get => _region.End;
                set => Region = Create(_region.Start, value);
            }

            public string SpecialName
            {
                get => _specialName;
                private set
                {
                    if (value == _specialName) return;
                    _specialName = value;
                    OnPropertyChanged();
                }
            }

            private string RegionString()
            {
                if (Region.Start == Region.End)
                    return "0x" + Region.Start.ToString("X");
                
                return $"0x{Region.Start:X} - 0x{Region.End:X}";
            }

            public string DisplayString => ToString();
            public override string ToString()
            {
                if (string.IsNullOrEmpty(SpecialName))
                    return RegionString();
                return $"{SpecialName} ({RegionString()})";
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
            {
                if (EqualityComparer<T>.Default.Equals(field, value)) return false;
                field = value;
                OnPropertyChanged(propertyName);
                return true;
            }
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
                CharacterRegions.Add(new CharacterRegionView(region));
            }
            //list_characterRegions.SelectedIndex = sel;


            ReInit(file);

            _isLoading = false;

            return this;
        }

        public void Save()
        {
            _spf.Save(ContentFile.FilePath);
            UnsavedChanges = false;
        }

        public void Refresh()
        {
            ErrorText = "";
            ReInit(ContentFile);
        }

        public void Discard()
        {
            UnsavedChanges = false;
        }

        public IHistory History { get; private set; }
        private bool _unsavedChanges;

        public bool UnsavedChanges
        {
            get => _unsavedChanges;
            private set
            {
                SetAndRaise(UnsavedChangesProperty, ref _unsavedChanges, value);
                if (value)
                    ErrorText = "Font settings changed: Recompile required!";
            }
        }

        public ContentFile ContentFile { get; private set; }

        public void Dispose()
        {
            _avaloniaRenderingSurface?.Dispose();
            _game?.Dispose();
            _game = null;
        }

        private SimpleGame _game;
        private SpriteFont _font;
        private uint _characterRegionEditStart, _characterRegionEditEnd;
        private CharacterRegionView _selectedCharacterRegion;


        private void SetFont(string outputDir, string assetPath)
        {
            try
            {
                _game.Content.RootDirectory = outputDir;
                _font = _game.Content.Load<SpriteFont>(assetPath);

            }
            catch (Exception ex)
            {
                ErrorText = $"Font could not be loaded: {ex.Message}";
            }
        }

        private void ReInit(ContentFile file)
        {
            _font?.Dispose();
            _font = null;
            _game?.Dispose();
            _game = new SimpleGame(_avaloniaRenderingSurface);
            _game.Render += GameOnRender;
            ErrorText = "";
            var egoFileName = Path.GetFileNameWithoutExtension(file.RelativePath);
            var egoPathRel = Path.Combine(Path.GetDirectoryName(file.RelativePath), egoFileName);
            var outputDir = Path.Combine(file.Project.FilePath, file.Project.ConfiguredOutputDirectory);

            if (!File.Exists(Path.Combine(outputDir, egoPathRel + ".ego")))
                ErrorText = "File not found! Do not forget to compile font!";
            else
                _game.Init += () => SetFont(outputDir, egoPathRel);
            _avaloniaRenderingSurface.InitializeContext();

        }

        private void GameOnRender(GameTime e, SpriteBatch batch)
        {
            if (_game is null || _font is null)
                return;
            
            _game.GraphicsDevice.Clear(Color.Transparent);
            batch.Begin();
            batch.DrawString(_font, ExampleText, new Vector2(0, 0), _exampleTextColor);
            batch.End();
        }

        public static readonly DirectProperty<SpriteFontViewer, uint> CharacterRegionEditStartProperty =
            AvaloniaProperty.RegisterDirect<SpriteFontViewer, uint>(
                nameof(CharacterRegionEditStart),
                o => o.CharacterRegionEditStart,
                (o, v) => o.CharacterRegionEditStart = v);
        public static readonly DirectProperty<SpriteFontViewer, uint> CharacterRegionEditEndProperty =
            AvaloniaProperty.RegisterDirect<SpriteFontViewer, uint>(
                nameof(CharacterRegionEditEnd),
                o => o.CharacterRegionEditEnd,
                (o, v) => o.CharacterRegionEditEnd = v);
        public static readonly DirectProperty<SpriteFontViewer, bool> ShowAsHexProperty =
            AvaloniaProperty.RegisterDirect<SpriteFontViewer, bool>(
                nameof(ShowAsHex),
                o => o.ShowAsHex,
                (o, v) => o.ShowAsHex = v);

        public bool ShowAsHex
        {
            get => _showAsHex;
            set => SetAndRaise(ShowAsHexProperty, ref _showAsHex, value);
        }

        public uint CharacterRegionEditStart
        {
            get => _characterRegionEditStart;
            set => SetAndRaise(CharacterRegionEditStartProperty, ref _characterRegionEditStart, value);
        }
        
        public uint CharacterRegionEditEnd
        {
            get => _characterRegionEditEnd;
            set => SetAndRaise(CharacterRegionEditEndProperty, ref _characterRegionEditEnd, value);
        }

        private int _editIndex;
        private bool _showAsHex;

        private void AddCharacterRegion_OnClick(object sender, RoutedEventArgs e)
        {
            _editIndex = -1;
            CharacterRegionEditStart = CharacterRegionEditEnd = 0;
            _characterRegionPopup.Open();
        }

        private void RemoveCharacterRegion_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedCharacterRegion is null)
                return;
            if (!_spf.CharacterRegions.Remove(SelectedCharacterRegion.Region))
            {
                throw new ArgumentException();
            }

            CharacterRegions.Remove(SelectedCharacterRegion);
            UnsavedChanges = true;
        }

        private void InputElement_OnDoubleTapped(object sender, RoutedEventArgs e)
        {
            if (SelectedCharacterRegion is null)
                return;
            _editIndex = CharacterRegions.IndexOf(SelectedCharacterRegion);
            if (_editIndex == -1)
                return;
            CharacterRegionEditStart = (uint)SelectedCharacterRegion.Region.Start;
            CharacterRegionEditEnd = (uint)SelectedCharacterRegion.Region.End;
            _characterRegionPopup.Open();
            
        }

        private void PopupCancel_OnClick(object sender, RoutedEventArgs e)
        {
            _characterRegionPopup.Close();
        }
        private void PopupOk_OnClick(object sender, RoutedEventArgs e)
        {
            _characterRegionPopup.Close();
            var newRegion = new CharacterRegion((int)CharacterRegionEditStart, (int)CharacterRegionEditEnd);
            if (_editIndex == -1)
            {
                _spf.CharacterRegions.Add(newRegion);
                CharacterRegions.Add(new CharacterRegionView(newRegion));
                CharacterRegionEditStart = CharacterRegionEditEnd = 0;
            }
            else
            {
                _spf.CharacterRegions[_editIndex] = newRegion;
                CharacterRegions[_editIndex].Region = newRegion;
            }
            UnsavedChanges = true;
        }
    }
}