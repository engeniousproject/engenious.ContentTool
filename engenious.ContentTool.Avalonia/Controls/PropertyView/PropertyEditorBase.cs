using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace engenious.ContentTool.Avalonia
{
    public abstract class PropertyEditorBase : IPropertyEditor
    {
        private object _value;
        public event PropertyChangedEventHandler PropertyChanged;

        public PropertyEditorBase()
        {

        }

        public virtual PropertyViewBase CreatePropertyView(ComplexPropertyView parent, string name)
        {
            return new PropertyView(parent, name, this);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool _pauseUpdate = false;
        private PropertyViewBase _property;

        public bool IsReadOnly
        {
            get
            {
                if (_property == null)
                    return true;
                return !_property.HasSetter;
            }
        }

        public PropertyViewBase Property
        {
            get => _property;
            set
            {
                if (_property != null)
                {
                    _property.PropertyChanged -= OnPropertyOnPropertyChanged;
                }
                _property = value;
                Value = _property?.Value;
                
                if (_property != null)
                    _property.PropertyChanged += OnPropertyOnPropertyChanged;
            }
        }

        private void OnPropertyOnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(_property.Value))
            {
                Value = ConvertFromPropertyToEditor(_property.Value);
            }
        }

        public object Value
        {
            get => _value;
            set
            {
                _value = value;
                if (_pauseUpdate)
                    return;
                _pauseUpdate = true;
                Property.Value = ConvertFromEditorToProperty(value);
                OnPropertyChanged();
                _pauseUpdate = false;
            }
        }

        public virtual object ConvertFromEditorToProperty(object editorValue) => editorValue;

        public virtual object ConvertFromPropertyToEditor(object propertyValue) => propertyValue;
    }
}