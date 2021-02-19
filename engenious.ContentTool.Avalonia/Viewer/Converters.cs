using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Avalonia.Data.Converters;
using engenious.ContentTool.Viewer;
using engenious.Graphics;
using EffectTechnique = engenious.Graphics.EffectTechnique;

namespace engenious.ContentTool.Avalonia
{
    internal class ParameterConverter : IValueConverter
    {
        private readonly Dictionary<ModelEffectViewer, Dictionary<EffectTechnique, List<IEffectParameterBinding>>>
            _caches;

        public ParameterConverter()
        {
            _caches =
                new Dictionary<ModelEffectViewer, Dictionary<EffectTechnique, List<IEffectParameterBinding>>>();
        }

        private Dictionary<EffectTechnique, List<IEffectParameterBinding>> GetCache(ModelEffectViewer viewer)
        {
            if (_caches.TryGetValue(viewer, out var cache))
                return cache;
            cache = new Dictionary<EffectTechnique, List<IEffectParameterBinding>>();
            _caches.Add(viewer, cache);
            return cache;
        }
        public List<IEffectParameterBinding> Convert(EffectTechnique technique, ModelEffectViewer effectViewer)
        {
            var cache = GetCache(effectViewer);

            if (cache.TryGetValue(technique, out var availableParams))
                return availableParams;

            availableParams = new List<IEffectParameterBinding>();
            cache.Add(technique, availableParams);

            foreach (var p in technique.Passes)
            {
                foreach (var param in p.Parameters)
                {
                    var type = GetParamType(param.Type);
                    var binding = CreateBinding(param, type);
                    effectViewer._bindings.Add(binding);
                    // cont.SelectedIndexChanged += (o, args) => binding.BindTo(_game, cont.SelectedItem.ToString(),
                    //     ((BindingItem) cont.SelectedItem).IsField);
                    availableParams.Add(binding);
                }
            }

            return availableParams;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is EffectTechnique technique) || !(parameter is ModelEffectViewer effectViewer))
                return null;

            return Convert(technique, effectViewer);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static IEffectParameterBinding CreateBinding(EffectPassParameter p, Type type)
        {
            return (IEffectParameterBinding) Activator.CreateInstance(
                typeof(EffectParameterBinding<>).MakeGenericType(type), p);
        }
        class EffectParameterBinding<T> : IEffectParameterBinding
        {
            public Type UnderlyingType => typeof(T);
            private Func<T> _getValue;
            private readonly EffectPassParameter _p;
            private Action<EffectPassParameter, T> _setValue;

            public string Name => _p.Name;

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
                    _getValue = () => (T) baseParam.GetType().GetField(name).GetValue(baseParam);
                else
                    _getValue = () => (T) baseParam.GetType().GetProperty(name)?.GetValue(baseParam);

                var setValueMeth = typeof(EffectPassParameter).GetMethods().First(x =>
                    x.Name == "SetValue" && !x.IsStatic && x.GetParameters().Length == 1 &&
                    x.GetParameters()[0].ParameterType.IsAssignableFrom(typeof(T)));

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
        private static Type GetParamType(EffectParameterType type)
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
                case EffectParameterType.Sampler2DShadow:
                    t = typeof(Texture2D);
                    break;
                case EffectParameterType.Sampler2DArray:
                case EffectParameterType.Sampler2DArrayShadow:
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
    }

    public class BindingConverter : IValueConverter
    {
        public class BindingUpdatable
        {
            public BindingUpdatable()
            {
                Bindings = new List<ModelEffectViewer.BindingItem>();
            }

            public List<ModelEffectViewer.BindingItem> Bindings { get; }

            public void Add(ModelEffectViewer.BindingItem i) => Bindings.Add(i);
        }

        private readonly Dictionary<ModelEffectViewer, Dictionary<IEffectParameterBinding, BindingUpdatable>> _caches;

        public BindingConverter()
        {
            _caches =
                new Dictionary<ModelEffectViewer, Dictionary<IEffectParameterBinding, BindingUpdatable>>();
        }

        private Dictionary<IEffectParameterBinding, BindingUpdatable> GetCache(ModelEffectViewer viewer)
        {
            if (_caches.TryGetValue(viewer, out var cache))
                return cache;
            cache = new Dictionary<IEffectParameterBinding, BindingUpdatable>();
            _caches.Add(viewer, cache);
            return cache;
        }
        public BindingUpdatable Convert(IEffectParameterBinding parameterBinding, ModelEffectViewer effectViewer)
        {
            var cache = GetCache(effectViewer);

            if (cache.TryGetValue(parameterBinding, out var bindingItems))
                return bindingItems;

            bindingItems = new BindingUpdatable();

            cache.Add(parameterBinding, bindingItems);

            if (effectViewer._properties.TryGetValue(parameterBinding.UnderlyingType, out var props))
            {
                foreach (var p in props)
                {
                    bindingItems.Add(new ModelEffectViewer.BindingItem(p, false));
                }
            }

            if (effectViewer._fields.TryGetValue(parameterBinding.UnderlyingType, out var fields))
            {
                foreach (var p in fields)
                {
                    bindingItems.Add(new ModelEffectViewer.BindingItem(p, true));
                }
            }

            return bindingItems;
            // parameterBinding.BindTo();
            // var binding = CreateBinding(param, type);
            // _effectViewer._bindings.Add(binding);
            // cont.SelectedIndexChanged += (o, args) => binding.BindTo(_game, cont.SelectedItem.ToString(),
            //     ((BindingItem) cont.SelectedItem).IsField);
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is IEffectParameterBinding parameterBinding) || !(parameter is ModelEffectViewer effectViewer))
                return null;

            return Convert(parameterBinding, effectViewer);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}