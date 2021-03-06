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
            var parentCasted = Expression.Convert(parentParam, parent.ActualType);

            var propInfo = parent.ActualType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance, null, type, Array.Empty<Type>(), Array.Empty<ParameterModifier>());
            
            var prop = Expression.Property(parentCasted, propInfo);

            if (propInfo.CanRead)
                _getValue = Expression.Lambda<Func<object, object>>(Expression.Convert(prop, typeof(object)), parentParam).Compile();
            
            if (propInfo.CanWrite)
                _setValue = Expression.Lambda<Action<object, object>>(Expression.Assign(prop, Expression.Convert(valueParam, prop.Type)), parentParam, valueParam).Compile();
        }

        internal virtual void OnParentChanged()
        {
            RegisterBinding();
        }

        private INotifyPropertyChanged _oldPropChange;
        private void RegisterBinding()
        {
            var newPropChange = Parent?.Value as INotifyPropertyChanged;

            //TODO: deabbo

            if (_oldPropChange != null)
            {
                _oldPropChange.PropertyChanged -= NewPropChangeOnPropertyChanged;
            }
            
            if (newPropChange == null)
            {
                return;
            }
            
            newPropChange.PropertyChanged += NewPropChangeOnPropertyChanged;
            
            _oldPropChange = newPropChange;
        }

        private void NewPropChangeOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Name)
                OnPropertyChanged(new PropertyChangedEventArgs("Value"));
        }

        public virtual void BuildTree(int maxDepth = 1){}

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
                
                BuildTree();
                
                RegisterBinding();
                OnPropertyChanged();
            }
        }
        
        public Type Type { get; protected set; }

        public Type ActualType => Value?.GetType() ?? Type;

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged( new PropertyChangedEventArgs(propertyName));
        }
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }
    }
}