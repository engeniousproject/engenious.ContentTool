using System;

namespace engenious.ContentTool.Viewer
{
    public abstract class PropertyEditor<T> : IPropertyEditor<T>
    {
        private T _property;
        public event EventHandler PropertyChanged;


        public T Property
        {
            get => _property;
            set
            {
                if (!_property.Equals(value))
                {
                    _property = value;
                    OnPropertyChanged(value);
                }
            }
        }

        protected virtual void OnPropertyChanged(T newValue)
        {
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}