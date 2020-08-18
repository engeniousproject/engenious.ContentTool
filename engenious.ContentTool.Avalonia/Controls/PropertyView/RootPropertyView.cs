namespace engenious.ContentTool.Avalonia
{
    public sealed class RootPropertyView : ComplexPropertyView
    {
        public static RootPropertyView Create(string name, object value, int maxDepth = 2)
        {
            var root = new RootPropertyView(name, value);
            root.BuildTree(maxDepth);
            return root;
        }
        private object _value;

        private RootPropertyView(string name, object value) : base(null, name, value.GetType())
        {
            _value = value;
        }

        public override object Value
        {
            get => _value;
            set
            {
                _value = value;
                OnPropertyChanged();
            }
        }
    }
}