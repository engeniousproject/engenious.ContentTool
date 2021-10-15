using System;
using engenious.Content.Models;
using engenious.Content.Models.History;

namespace engenious.ContentTool.Viewer
{
    public interface IViewer : IDisposable
    {
        object GetViewerControl(ContentFile file);
        void Save();
        void Discard();
        IHistory History { get; }
        bool UnsavedChanges { get; }
        ContentFile ContentFile { get; }
    }
}
