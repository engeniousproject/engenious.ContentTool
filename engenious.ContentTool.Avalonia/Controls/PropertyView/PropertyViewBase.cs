using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace engenious.ContentTool.Avalonia
{
    public abstract class PropertyViewBase : INotifyPropertyChanged
    {
        private Func<object, object> _getValue;
        private Action<object, object> _setValue;
        public PropertyViewBase(ComplexPropertyView parent, string name, Type type)
        {
            Parent = parent;
            Name = name;
            Type = type;

            if (Parent == null)
                return;

            var parentParam = Expression.Parameter(typeof(object), "parent");
            var valueParam = Expression.Parameter(typeof(object), "value");
            var parentCasted = Expression.Convert(parentParam, parent.Type);

            var propInfo = parent.Type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance, null, type, Array.Empty<Type>(), Array.Empty<ParameterModifier>());
            
            var prop = Expression.Property(parentCasted, propInfo);

            if (propInfo.CanRead)
                _getValue = Expression.Lambda<Func<object, object>>(Expression.Convert(prop, typeof(object)), parentParam).Compile();
            
            if (propInfo.CanWrite)
                _setValue = Expression.Lambda<Action<object, object>>(Expression.Assign(prop, Expression.Convert(valueParam, prop.Type)), parentParam, valueParam).Compile();
        }
        public virtual void BuildTree(){}

        public string Name { get; }
        
        public ComplexPropertyView Parent { get; }

        public bool HasSetter => _setValue != null;
        public bool HasGetter => _getValue != null;

        public bool IsNull => HasGetter || Value == null;

        public virtual object Value
        {
            get => Parent?.Value == null ? null : _getValue?.Invoke(Parent.Value);
            set
            {
                if (_setValue == null || Parent?.Value == null)
                    return;
                _setValue(Parent.Value, value);
                OnPropertyChanged();
            }
        }
        
        public Type Type { get; protected set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}