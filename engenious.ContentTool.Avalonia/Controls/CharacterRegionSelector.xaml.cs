using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using engenious.Content.Models;
using ReactiveUI;

namespace engenious.ContentTool.Avalonia
{
    public class CodepointItem : INotifyPropertyChanged
    {
        private readonly CodePointList _parent;
        private bool _isSelected;
        public uint Codepoint { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetField(ref _isSelected, value);
        }

        public CodepointItem(CodePointList parent, uint codepoint)
        {
            _parent = parent;
            Codepoint = codepoint;
            IsSelected = CheckIsSelected();
            _parent.PropertyChanged += (sender, args) =>
                                       {
                                           if (args.PropertyName is "Start" or "End")
                                           {
                                               IsSelected = CheckIsSelected();
                                           }
                                       };
        }

        private bool CheckIsSelected() => (Codepoint >= _parent.Start && Codepoint <= _parent.End);



        public override string ToString()
        {
            return char.ConvertFromUtf32((int)Codepoint);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
    public class CodePointList : IReadOnlyCollection<CodepointItem>, INotifyPropertyChanged
    {
        private const int MaxSize = 112956;
        private const int MaxCodepoint = 0x1FFFF;
        private readonly GlyphTypeface _typeface;
        private uint _start;
        private uint _end;
        public event PropertyChangedEventHandler PropertyChanged;

        public CodePointList(GlyphTypeface typeface)
        {
            _typeface = typeface;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public uint Start
        {
            get => _start;
            set => SetField(ref _start, value);
        }

        public uint End
        {
            get => _end;
            set => SetField(ref _end, value);
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public struct Enumerator : IEnumerator<CodepointItem>
        {
            private readonly CodePointList _parent;
            private readonly GlyphTypeface _typeface;
            private uint _currentCodePoint;
            
            public Enumerator(CodePointList parent, GlyphTypeface typeface)
            {
                _parent = parent;
                _typeface = typeface;
                _currentCodePoint = 0;
                Current = null;
            }
            public bool MoveNext()
            {
                while (!_typeface.TryGetGlyph(++_currentCodePoint, out _) && _currentCodePoint < MaxCodepoint)
                {
                    
                }

                if (_currentCodePoint >= MaxCodepoint)
                    return false;

                Current = new CodepointItem(_parent, _currentCodePoint);

                return true;
            }

            public void Reset()
            {
                _currentCodePoint = 0;
                Current = null;
            }

            public CodepointItem Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this, _typeface);
        }

        IEnumerator<CodepointItem> IEnumerable<CodepointItem>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => MaxSize; // estimate of maximum code points
    }
    public class CharacterRegionBackground : IValueConverter
    {
        private static readonly SolidColorBrush MarkedBrush = new SolidColorBrush(Colors.DarkGray);
        private static readonly SolidColorBrush NotMarkedBrush = new SolidColorBrush(Colors.Transparent);

        static CharacterRegionBackground()
        {
            //ListBoxItemSelectedBackgroundThemeBrush
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool val)
            {
                return val ? MarkedBrush : NotMarkedBrush;
            }
            return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
    public class CharacterRegionSelector : UserControl
    {
        private readonly IDisposable _fontFamilyChanged;
        private CodePointList _codepoints;

        public static readonly DirectProperty<CharacterRegionSelector, CodePointList> CodepointsProperty =
            AvaloniaProperty.RegisterDirect<CharacterRegionSelector, CodePointList>(
                nameof(Codepoints),
                o => o.Codepoints,
                (o, v) => o.Codepoints = v);
        
        public CharacterRegionSelector()
        {
            InitializeComponent();

            _fontFamilyChanged = this.GetObservable(FontFamilyProperty).Subscribe(value =>
            {
                if (value.Name == "$Default")
                    return;
                Codepoints = new CodePointList(FontManager.Current.GetOrAddGlyphTypeface(new Typeface(value.Name)));
                Codepoints.Start = _start;
                Codepoints.End = _end;
            });
        }

        public CodePointList Codepoints
        {
            get => _codepoints;
            private set => SetAndRaise(CodepointsProperty, ref _codepoints, value);
        }
        
        public static readonly DirectProperty<CharacterRegionSelector, uint> StartProperty =
            AvaloniaProperty.RegisterDirect<CharacterRegionSelector, uint>(
                nameof(Start),
                o => o.Start,
                (o, v) => o.Start = v);        
        public static readonly DirectProperty<CharacterRegionSelector, uint> EndProperty =
            AvaloniaProperty.RegisterDirect<CharacterRegionSelector, uint>(
                nameof(End),
                o => o.End,
                (o, v) => o.End = v);

        private uint _start, _end;
        public uint Start
        {
            get => (uint)_start;
            set
            {
                if (Codepoints is not null)
                    Codepoints.Start = (uint)value;
                SetAndRaise(StartProperty, ref _start, value);
            }
        }

        public uint End
        {
            get => (uint)_end;
            set
            {
                if (Codepoints is not null)
                    Codepoints.End = (uint)value;
                SetAndRaise(EndProperty, ref _end, value);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            this.FindControl<ItemsRepeater>("itemsRepeater").DataContext = this;
        }

        private void InputElement_OnPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (sender is Control { DataContext: CodepointItem item })
            {
                var (newStart, newEnd) = e.InitialPressMouseButton switch
                {
                    MouseButton.Left => (Math.Min(item.Codepoint, End), Math.Max(item.Codepoint, End)),
                    MouseButton.Right => (Math.Min(item.Codepoint, Start), Math.Max(item.Codepoint, Start)),
                    MouseButton.Middle => (item.Codepoint, item.Codepoint),
                    _ => (Start, End)
                };
                Start = newStart;
                End = newEnd;
            }
        }
    }
}