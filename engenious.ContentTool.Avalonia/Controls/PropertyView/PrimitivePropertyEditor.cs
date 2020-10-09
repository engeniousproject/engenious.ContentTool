using System;

namespace engenious.ContentTool.Avalonia
{
    [PropertyEditor(typeof(string))]
    [PropertyEditor(typeof(byte))]
    [PropertyEditor(typeof(sbyte))]
    [PropertyEditor(typeof(short))]
    [PropertyEditor(typeof(ushort))]
    [PropertyEditor(typeof(int))]
    [PropertyEditor(typeof(uint))]
    [PropertyEditor(typeof(long))]
    [PropertyEditor(typeof(ulong))]
    [PropertyEditor(typeof(float))]
    [PropertyEditor(typeof(double))]
    [PropertyEditor(typeof(decimal))]
    public class PrimitivePropertyEditor : PropertyEditorBase
    {
        public override object ConvertFromEditorToProperty(object editorValue)
        {
            if (editorValue == null)
                return null;
            var targetType = Property.ActualType;
            return Convert.ChangeType(editorValue, targetType) ?? editorValue;
        }

        public override object ConvertFromPropertyToEditor(object propertyValue)
        {
            return propertyValue;
        }
    }
}