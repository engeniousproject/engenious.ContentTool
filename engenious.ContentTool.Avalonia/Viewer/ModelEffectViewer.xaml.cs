using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using engenious.Avalonia;
using engenious.ContentTool.Models;
using engenious.ContentTool.Models.History;
using engenious.ContentTool.Viewer;
using engenious.Graphics;
using OpenToolkit;
using OpenToolkit.Graphics.OpenGL4;

namespace engenious.ContentTool.Avalonia
{
    public abstract class ModelEffectViewer : UserControl, IViewer
    {
        private readonly Dictionary<Type, List<string>> _properties = new Dictionary<Type, List<string>>();
        private readonly Dictionary<Type, List<string>> _fields = new Dictionary<Type, List<string>>();
        private readonly bool _isEffectView;
        private readonly AvaloniaRenderingSurface _avaloniaRenderingSurface;
        private readonly EffectModelViewerGame _game;

        public ModelEffectViewer()
            : this(false)
        {
            
        }
        public ModelEffectViewer(bool effectView)
        {
            _isEffectView = effectView;
            InitializeComponent();

            _avaloniaRenderingSurface = this.FindControl<AvaloniaRenderingSurface>("renderingSurface");

            _game = new EffectModelViewerGame(_avaloniaRenderingSurface, effectView);

            _game.EffectLoaded += GameOnEffectLoaded;
            _game.UpdateBindings += GameOnUpdateBindings;

            foreach (var p in typeof(EffectModelViewerGame).GetProperties())
            {
                if (p.GetMethod.IsStatic)
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
        }

        private void GameOnUpdateBindings(object? sender, EventArgs e)
        {
            foreach (var b in _bindings)
                b.Update();
        }


        private static Type GetType(EffectParameterType type)
        {
            Type t;
            switch (type)
            {
                case EffectParameterType.Bool:
                    t = typeof(bool);
                    break;
                case EffectParameterType.Double:
                    t = typeof(double);
                    break;
                case EffectParameterType.Float:
                    t = typeof(float);
                    break;
                case EffectParameterType.FloatMat4:
                    t = typeof(Matrix);
                    break;
                case EffectParameterType.FloatVec2:
                    t = typeof(Vector2);
                    break;
                case EffectParameterType.FloatVec3:
                    t = typeof(Vector3);
                    break;
                case EffectParameterType.FloatVec4:
                    t = typeof(Vector4);
                    break;
                case EffectParameterType.Sampler2D:
                    t = typeof(Texture2D);
                    break;
                case EffectParameterType.Sampler2DArray:
                    t = typeof(Texture2DArray);
                    break;
                case EffectParameterType.Int:
                    t = typeof(int);
                    break;
                case EffectParameterType.IntVec2:
                    t = typeof(Point);
                    break;
                case EffectParameterType.UnsignedInt:
                    t = typeof(uint);
                    break;
                default:
                    t = typeof(EffectPassParameter);
                    break;
            }
            return t;
        }

        private readonly List<Control> _dynamicControls = new List<Control>();

        interface IEffectParameterBinding
        {
            void BindTo(object baseParam, string name, bool isField);
            void Update();
        }
        private static IEffectParameterBinding CreateBinding(EffectPassParameter p, Type type)
        {
            return (IEffectParameterBinding)Activator.CreateInstance(typeof(EffectParameterBinding<>).MakeGenericType(type), p);
        }
        class EffectParameterBinding<T> : IEffectParameterBinding
        {

            private Func<T> _getValue;
            private readonly EffectPassParameter _p;
            private Action<EffectPassParameter, T> _setValue;
            public EffectParameterBinding(EffectPassParameter p)
            {
                _p = p;
            }

            private void BindToPropertyProvider(IPropertyProvider propertyProvider, string name)
            {
                
            }

            public void BindTo(object baseParam, string name, bool isField)
            {
                if (isField)
                    _getValue = () => (T)baseParam.GetType().GetField(name).GetValue(baseParam);
                else
                    _getValue = () => (T)baseParam.GetType().GetProperty(name)?.GetValue(baseParam);

                var setValueMeth = typeof(EffectPassParameter).GetMethods().First(x => x.Name == "SetValue" && !x.IsStatic && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType.IsAssignableFrom(typeof(T)));

                var p1 = Expression.Parameter(typeof(EffectPassParameter));
                var p2 = Expression.Parameter(typeof(T));
                _setValue = Expression.Lambda<Action<EffectPassParameter, T>>
                    (
                    Expression.Call(p1, setValueMeth, p2)
                    , p1, p2).Compile();
            }

            public void Update()
            {
                if (_getValue == null || _setValue == null)
                    return;
                _setValue(_p, _getValue());
            }
        }

        private struct BindingItem
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
        private void AddItems(ComboBox comboBox,Type type)
        {
            if (_properties.TryGetValue(type, out var props))
            {
                foreach (var p in props)
                {
                    //comboBox.Items.Add(new BindingItem(p, false));
                }
            }
            if (_fields.TryGetValue(type, out var fields))
            {
                foreach (var p in fields)
                {
                    //comboBox.Items.Add(new BindingItem(p, true));
                }
            }

            if (_properties.TryGetValue(typeof(IPropertyProvider), out var propProviders))
            {
                foreach (var p in propProviders)
                {
                    
                }
            }
        }
        private readonly List<IEffectParameterBinding> _bindings = new List<IEffectParameterBinding>();
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

        public object GetViewerControl(ContentFile file)
        {
            History = new History();
            ContentFile = file;
            try
            {
                var egoFileName = Path.GetFileNameWithoutExtension(file.RelativePath);
                var egoPathRel = Path.Combine(Path.GetDirectoryName(file.RelativePath), egoFileName);
                var outputDir = Path.Combine(file.Project.FilePath, file.Project.ConfiguredOutputDirectory);

                if (_isEffectView)
                    _game.SetEffect(outputDir, egoPathRel);
                else
                    _game.SetModel(outputDir, egoPathRel);
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
    }
}