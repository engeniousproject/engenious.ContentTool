namespace engenious.ContentTool.Observer
{
    public delegate void NotifyPropertyValueChangedHandler(object sender,PropertyValueChangedEventArgs args);
    public interface INotifyPropertyValueChanged
    {
        event NotifyPropertyValueChangedHandler PropertyValueChanged;
    }
}