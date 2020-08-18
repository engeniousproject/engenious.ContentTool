using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace engenious.ContentTool.Avalonia
{
    public abstract class ChoicePropertyEditor: PropertyEditorBase
    {
        public ChoicePropertyEditor()
        {
            Choices = new ObservableCollection<object>();
        }

        public ObservableCollection<object> Choices { get; }
    }
}