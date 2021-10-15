using engenious.Content.Models;
using engenious.Content.Models.History;
using engenious.ContentTool.Viewer;

namespace engenious.ContentTool.Avalonia
{
    [ViewerInfo(".glsl", true)]
    public class EffectViewer : ModelEffectViewer
    {
        public EffectViewer() : base(true)
        {
        }
    }
}