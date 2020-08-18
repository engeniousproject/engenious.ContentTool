using engenious.ContentTool.Models;
using engenious.ContentTool.Models.History;
using engenious.ContentTool.Viewer;

namespace engenious.ContentTool.Avalonia
{
    [ViewerInfo(".fbx", true)]
    public class ModelViewer : ModelEffectViewer
    {
        public ModelViewer() : base(false)
        {
        }
    }
}