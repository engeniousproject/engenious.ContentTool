using engenious.ContentTool.Models;
using engenious.ContentTool.Models.History;

namespace engenious.ContentTool.Viewer
{
    public interface IViewer
    {
        object GetViewerControl(ContentFile file);
        void Save();
        void Discard();
        IHistory History { get; }
        bool UnsavedChanges { get; }
        ContentFile ContentFile { get; }
    }
}
