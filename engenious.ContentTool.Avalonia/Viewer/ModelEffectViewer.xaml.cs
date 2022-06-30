using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using engenious.Avalonia;
using engenious.Content.Models;
using engenious.Content.Models.History;
using engenious.ContentTool.Viewer;
using OpenTK.Windowing.Common;

namespace engenious.ContentTool.Avalonia
{
    public abstract class ModelEffectViewer : UserControl, IViewer
    {
        internal readonly Dictionary<Type, List<string>> Properties = new Dictionary<Type, List<string>>();
        internal readonly Dictionary<Type, List<string>> Fields = new Dictionary<Type, List<string>>();
        private readonly bool _isEffectView;
        private readonly AvaloniaRenderingSurface _avaloniaRenderingSurface;

        public static readonly DirectProperty<ModelEffectViewer, EffectModelViewerGame> GameProperty =
            AvaloniaProperty.RegisterDirect<ModelEffectViewer, EffectModelViewerGame>(
                nameof(Game),
                o => o.Game);
        
        public static readonly DirectProperty<ModelEffectViewer, ObservableCollection<IEffectParameterBinding>> ParameterBindingsProperty =
            AvaloniaProperty.RegisterDirect<ModelEffectViewer, ObservableCollection<IEffectParameterBinding>>(
                nameof(ParameterBindings),
                o => o.ParameterBindings);
        
        public ObservableCollection<IEffectParameterBinding> ParameterBindings { get; }
        private EffectModelViewerGame _game;

        public EffectModelViewerGame Game
        {
            get => _game;
            set
            {
                if (_game == value) return;
                SetAndRaise(GameProperty, ref _game, value);
            }
        }
        
        private void UpdateEffectParameters()
        {
            ParameterBindings.Clear();

            var currentTechnique = Game?.Effect?.CurrentTechnique;
            if (currentTechnique == null)
                return;
            
            var parameters = ParameterConverter.Instance.Convert(currentTechnique, this);
            foreach (var p in parameters)
                ParameterBindings.Add(p);
        }


        public ModelEffectViewer()
            : this(false)
        {
        }

        public ModelEffectViewer(bool effectView)
        {
            ParameterBindings = new ObservableCollection<IEffectParameterBinding>();
            _isEffectView = effectView;
            DataContext = this;
            InitializeComponent();

            _avaloniaRenderingSurface = this.FindControl<AvaloniaRenderingSurface>("renderingSurface");
        }

        private void GameOnUpdateBindings(object? sender, EventArgs e)
        {
            foreach (var b in ParameterBindings)
                b.Update();
        }


        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void ReInit()
        {
            Game?.Dispose();
            Game = new EffectModelViewerGame(_avaloniaRenderingSurface, _isEffectView);
            
            Game.UpdateBindings += GameOnUpdateBindings;

            foreach (var p in typeof(EffectModelViewerGame).GetProperties())
            {
                if (p.GetMethod == null || p.GetMethod.IsStatic)
                    continue;
                if (!Properties.TryGetValue(p.PropertyType, out var lst))
                {
                    lst = new List<string>();
                    Properties.Add(p.PropertyType, lst);
                }

                lst.Add(p.Name);
            }

            foreach (var p in typeof(EffectModelViewerGame).GetFields())
            {
                if (p.IsStatic)
                    continue;
                if (!Fields.TryGetValue(p.FieldType, out var lst))
                {
                    lst = new List<string>();

                    Fields.Add(p.FieldType, lst);
                }

                lst.Add(p.Name);
            }

            _avaloniaRenderingSurface.InitializeContext();
        }

        public object GetViewerControl(ContentFile file)
        {
            History = new History();
            ContentFile = file;
            try
            {
                ReInit();
                var egoFileName = Path.GetFileNameWithoutExtension(file.RelativePath);
                var egoPathRel = Path.Combine(Path.GetDirectoryName(file.RelativePath), egoFileName);
                var outputDir = Path.Combine(file.Project.FilePath, file.Project.ConfiguredOutputDirectory);

                if (_isEffectView)
                    Game.SetEffect(outputDir, egoPathRel);
                else
                    Game.SetModel(outputDir, egoPathRel);
            }
            catch (FileNotFoundException)
            {
            }

            return this;
        }

        public void Save()
        {
            throw new NotImplementedException();
        }

        public void Refresh()
        {
            ReInit();
        }
        public void Discard()
        {
            throw new NotImplementedException();
        }

        public IHistory History { get; private set; }
        public bool UnsavedChanges { get; }
        public ContentFile ContentFile { get; private set; }

        public void Dispose()
        {
            _avaloniaRenderingSurface?.Dispose();
            Game?.Dispose();
        }

        private bool _isMouseDown;
        private global::Avalonia.Point _oldPos;

        private void RenderingSurface_OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isMouseDown)
                return;

            var point = e.GetCurrentPoint(this);


            if (point.Properties.IsLeftButtonPressed)
            {
                var diff = _oldPos - point.Position;

                Game.RotationY += (float) (diff.X / 15);
                Game.RotationX += (float) (diff.Y / 15);

                _oldPos = point.Position;
            }
        }

        private void RenderingSurface_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var point = e.GetCurrentPoint(this);
            if (point.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
            {
                _isMouseDown = true;
                _oldPos = point.Position;
            }
        }

        private void RenderingSurface_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            var point = e.GetCurrentPoint(this);
            if (point.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
            {
                _isMouseDown = false;
            }
        }

        private void RenderingSurface_OnMouseWheel(MouseWheelEventArgs obj)
        {
            var currentScaleT = MathF.Log(Game.Scaling);

            currentScaleT = Math.Clamp(currentScaleT + obj.OffsetY/10f, -10, 10);

            Game.Scaling = MathF.Pow(MathF.E, currentScaleT);
        }

        private void Parameter_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count >= 1 && e.AddedItems[0] is BindingItem bindingItem && sender is ComboBox cmb && cmb.DataContext is IEffectParameterBinding paramBinding)
            {
                paramBinding.BindTo(_game, bindingItem.ToString(), bindingItem.IsField);
            }
        }

        private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            UpdateEffectParameters();
        }
    }
}