using System;
using System.IO;
using System.Windows.Forms;
using engenious.ContentTool.Models;
using engenious.ContentTool.Models.History;
using engenious.Graphics;
using engenious.Pipeline.Collada;
using engenious.WinForms;

namespace engenious.ContentTool.Viewer.Viewers
{
    [ViewerInfo(".fbx", true)]
    public class ModelViewer : EffectModelViewer
    {
        public ModelViewer()
            : base(false)
        {
        }
    }
}