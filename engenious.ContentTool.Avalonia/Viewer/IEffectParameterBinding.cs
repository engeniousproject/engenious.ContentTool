using System;
using System.Collections.ObjectModel;

namespace engenious.ContentTool.Avalonia
{
    public interface IEffectParameterBinding
    {
        void BindTo(object baseParam, string name, bool isField);
        void Update();
        public Type UnderlyingType { get; }
        public string Name { get; }
        
        public ObservableCollection<BindingItem> PossibleValues { get; }
    }
}