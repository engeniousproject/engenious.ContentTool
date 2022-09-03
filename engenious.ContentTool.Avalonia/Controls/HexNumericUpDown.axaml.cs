using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace engenious.ContentTool.Avalonia.Controls
{
    public partial class HexNumericUpDown : UserControl
    {
        public static readonly DirectProperty<HexNumericUpDown, uint> MaximumProperty =
            AvaloniaProperty.RegisterDirect<HexNumericUpDown, uint>(
                nameof(Maximum),
                o => o.Maximum,
                (o, v) => o.Maximum = v);
        public static readonly DirectProperty<HexNumericUpDown, uint> MinimumProperty =
            AvaloniaProperty.RegisterDirect<HexNumericUpDown, uint>(
                nameof(Minimum),
                o => o.Minimum,
                (o, v) => o.Minimum = v);
        public static readonly DirectProperty<HexNumericUpDown, uint> ValueProperty =
            AvaloniaProperty.RegisterDirect<HexNumericUpDown, uint>(
                nameof(Value),
                o => o.Value,
                (o, v) => o.Value = v);
        public static readonly DirectProperty<HexNumericUpDown, bool> IsHexProperty =
            AvaloniaProperty.RegisterDirect<HexNumericUpDown, bool>(
                nameof(IsHex),
                o => o.IsHex,
                (o, v) => o.IsHex = v);
        public static readonly DirectProperty<HexNumericUpDown, string> ValueParsingProperty =
            AvaloniaProperty.RegisterDirect<HexNumericUpDown, string>(
                nameof(ValueParsing),
                o => o.ValueParsing,
                (o, v) => o.ValueParsing = v);

        private uint _maximum = uint.MaxValue;
        private uint _minimum;
        private uint _value;
        private bool _isHex;
        private string _valueParsing;

        public uint Maximum
        {
            get => _maximum;
            set => SetAndRaise(MaximumProperty, ref _maximum, value);
        }

        public uint Minimum
        {
            get => _minimum;
            set => SetAndRaise(MinimumProperty, ref _minimum, value);
        }

        public uint Value
        {
            get => Math.Min(Math.Max(_value, Minimum), Maximum);
            set
            {
                // if (value > Maximum)
                // {
                //     value = Maximum;
                // }
                // else if (value < Minimum)
                // {
                //     value = Minimum;
                // }

                SetAndRaise(ValueProperty, ref _value, value);
                ValueParsing = ToValueParsing(value, _isHex);
            }
        }
        public bool IsHex
        {
            get => _isHex;
            set
            {
                SetAndRaise(IsHexProperty, ref _isHex, value);
                
                ValueParsing = ToValueParsing(_value, value);
            }
        }

        private static string ToValueParsing(uint val, bool isHex)
        {
            return isHex ? $"0x{val:X}" : val.ToString();
        }

        private bool _isParsing;

        protected string ValueParsing
        {
            get => _valueParsing;
            set
            {
                if (_isParsing)
                    return;
                _isParsing = true;
                var oldHex = IsHex;
                var oldValue = Value;
                var isHex = (value.StartsWith("0x"));
                var span = value.AsSpan(isHex ? 2 : 0);
                var numberStyle = isHex ? NumberStyles.AllowHexSpecifier : NumberStyles.None;
                SetAndRaise(ValueParsingProperty, ref _valueParsing, value);

                if (uint.TryParse(span, numberStyle, null, out var parsed))
                {
                    if (oldValue != parsed)
                    {
                        Value = parsed;
                    }
                    if (isHex != oldHex)
                    {
                        IsHex = isHex;
                    }
                }

                _isParsing = false;
            }
        }

        public HexNumericUpDown()
        {
            InitializeComponent();

            this.FindControl<StackPanel>("StackPanelControl").DataContext = this;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InputElement_OnLostFocus(object sender, RoutedEventArgs e)
        {
            ValueParsing = ToValueParsing(Value, IsHex);
        }
    }
}