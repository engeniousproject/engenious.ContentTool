using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using engenious.Avalonia;
using engenious.ContentTool.Models;
using engenious.ContentTool.Models.History;
using engenious.ContentTool.Viewer;
using engenious.Graphics;
using engenious.Pipeline.Collada;
using JetBrains.Annotations;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;

namespace engenious.ContentTool.Avalonia
{
    public abstract class ModelEffectViewer : UserControl, IViewer
    {
        internal readonly Dictionary<Type, List<string>> _properties = new Dictionary<Type, List<string>>();
        internal readonly Dictionary<Type, List<string>> _fields = new Dictionary<Type, List<string>>();
        private readonly bool _isEffectView;
        private readonly AvaloniaRenderingSurface _avaloniaRenderingSurface;

        private readonly List<Control> _dynamicControls = new List<Control>();

        internal readonly List<IEffectParameterBinding> _bindings = new List<IEffectParameterBinding>();

        public static readonly DirectProperty<ModelEffectViewer, EffectModelViewerGame> GameProperty =
            AvaloniaProperty.RegisterDirect<ModelEffectViewer, EffectModelViewerGame>(
                nameof(Game),
                o => o.Game);
        public static readonly DirectProperty<ModelEffectViewer, EffectTechnique> CurrentTechniqueProperty =
            AvaloniaProperty.RegisterDirect<ModelEffectViewer, EffectTechnique>(
                nameof(CurrentTechnique),
                o => o.CurrentTechnique, (viewer, technique) => viewer.CurrentTechnique = technique);
        private EffectModelViewerGame _game;

        public EffectModelViewerGame Game
        {
            get => _game;
            set => SetAndRaise(GameProperty, ref _game, value);
        }

        public EffectTechnique CurrentTechnique
        {
            get => _currentTechnique;
            private set
            {
                if (_currentTechnique == null && value != null)
                    SetAndRaise(CurrentTechniqueProperty, ref _currentTechnique, value);
            }
        }


        public ModelEffectViewer()
            : this(false)
        {
        }

        public ModelEffectViewer(bool effectView)
        {
            _isEffectView = effectView;

            InitializeComponent();

            _avaloniaRenderingSurface = this.FindControl<AvaloniaRenderingSurface>("renderingSurface");
            
            
        }

        private void GameOnUpdateBindings(object? sender, EventArgs e)
        {
            foreach (var b in _bindings)
                b.Update();
        }


        private string[] bla = new[] {"asdf", "bbq"};
        private void TechniqueCombo_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count > 0 && e.AddedItems[0] is EffectTechnique technique)
            {
                var paramList = this.FindControl<ItemsControl>("paramList");
                CurrentTechnique = technique;
                paramList.Items = new ParameterConverter().Convert(CurrentTechnique, this);
                // paramList.ItemContainerGenerator.ContainerFromIndex(0).FindCon
                //RaisePropertyChanged(CurrentTechniqueProperty, Optional<EffectTechnique>.Empty, CurrentTechnique);
            }
        }

        public struct BindingItem
        {
            public string Name { get; }
            public bool IsField { get; }

            public BindingItem(string name, bool isField)
            {
                Name = name;
                IsField = isField;
            }

            public override string ToString()
            {
                return Name;
            }
        }

        private void GameOnEffectLoaded(object sender, EventArgs e)
        {
            /*techniquesComboBox.Items.Clear();
            foreach (var t in _game.Effect.Techniques)
                techniquesComboBox.Items.Add(t.Name);

            techniquesComboBox.SelectedItem = _game.Effect.CurrentTechnique.Name;

            foreach (var c in _dynamicControls)
                c.Parent.Controls.Remove(c);
            _dynamicControls.Clear();

            _bindings.Clear();
            int i = 1;
            var curTech = _game.Effect.CurrentTechnique;
            foreach (var p in curTech.Passes)
            {
                foreach (var param in p.Parameters)
                {
                    var label = new Label { Text = param.Name };
                    var cont = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList};
                    var type = GetType(param.Type);
                    var binding = CreateBinding(param, type);
                    _bindings.Add(binding);
                    cont.SelectedIndexChanged += (o, args) => binding.BindTo(_game, cont.SelectedItem.ToString(),
                        ((BindingItem) cont.SelectedItem).IsField);
                    AddItems(cont, type);
                    _dynamicControls.Add(label);
                    _dynamicControls.Add(cont);
                    tableLayout.Controls.Add(label, 0, i);
                    tableLayout.Controls.Add(cont, 1, i++);
                }
            }*/
        }


        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void ReInit()
        {
            Game?.Dispose();
            Game = new EffectModelViewerGame(_avaloniaRenderingSurface, _isEffectView);


            Game.EffectLoaded += GameOnEffectLoaded;
            Game.UpdateBindings += GameOnUpdateBindings;

            foreach (var p in typeof(EffectModelViewerGame).GetProperties())
            {
                if (p.GetMethod == null || p.GetMethod.IsStatic)
                    continue;
                if (!_properties.TryGetValue(p.PropertyType, out var lst))
                {
                    lst = new List<string>();
                    _properties.Add(p.PropertyType, lst);
                }

                lst.Add(p.Name);
            }

            foreach (var p in typeof(EffectModelViewerGame).GetFields())
            {
                if (p.IsStatic)
                    continue;
                if (!_fields.TryGetValue(p.FieldType, out var lst))
                {
                    lst = new List<string>();

                    _fields.Add(p.FieldType, lst);
                }

                lst.Add(p.Name);
            }

            var nodeTree = this.FindControl<SingleRootTreeView>("nodeTree");
            var techniqueCombo = this.FindControl<ComboBox>("techniqueCombo");

            Game.PropertyChanged += (sender, args) =>
            {
                nodeTree.Root = Game.Model?.RootNode;
                techniqueCombo.Items = Game.Effect?.Techniques;
                techniqueCombo.SelectedItem = Game.Effect?.CurrentTechnique;
                CurrentTechnique = Game.Effect?.CurrentTechnique;
            };
            this.PropertyChanged += (sender, args) =>
            {
                CurrentTechnique = Game.Effect?.CurrentTechnique;
            };
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
        private EffectTechnique _currentTechnique;

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
            if (e.AddedItems != null && e.AddedItems.Count > 1 && e.AddedItems[0] is BindingItem bindingItem && sender is ComboBox cmb && cmb.DataContext is IEffectParameterBinding paramBinding)
            {
                paramBinding.BindTo(_game, bindingItem.ToString(), bindingItem.IsField);
            }
        }
    }
}