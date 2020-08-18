namespace engenious.ContentTool.Avalonia
{
    [PropertyEditor(typeof(bool))]
    public class BooleanPropertyEditor : ChoicePropertyEditor
    {
        public BooleanPropertyEditor()
        {
            Choices.Add(true);
            Choices.Add(false);
        }
    }
}