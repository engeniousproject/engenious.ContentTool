using engenious.ContentTool.Models;
using engenious.ContentTool.Models.History;
using engenious.ContentTool.Viewer;

namespace engenious.ContentTool.Avalonia
{
    [ViewerInfo(".fbx", true)]
    [ViewerInfo(".glft", true)]
    [ViewerInfo(".glb", true)]
    [ViewerInfo(".blend", true)]
    public class ModelViewer : ModelEffectViewer
    {
        public ModelViewer() : base(false)
        {
        }
    }
}