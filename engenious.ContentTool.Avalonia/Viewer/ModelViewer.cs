using engenious.Content.Models;
using engenious.Content.Models.History;
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