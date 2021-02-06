using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using engenious.ContentTool.Models;

namespace engenious.ContentTool.Viewer
{
    public class ViewerManager : IDisposable
    {

        private readonly Dictionary<string, (Type type, bool needsCompilation)> _viewerTypes = new Dictionary<string, (Type, bool)>();
        private readonly Dictionary<string, (IViewer viewer, bool needsCompilation)> _viewers = new Dictionary<string, (IViewer, bool)>();

        public ViewerManager()
        {
            LoadViewers();
        }

        private void LoadViewers()
        {
            foreach (var assembly in ReferenceManager.References)
            {
                var types = assembly.GetTypes().Where(p => typeof(IViewer).IsAssignableFrom(p)).Where(c => c.GetCustomAttributes(typeof(ViewerInfo), true).Length > 0);//TODO load from project references as well

                foreach(var type in types)
                {
                    foreach(var attr in type.GetCustomAttributes(typeof(ViewerInfo), true).Where(x=>x != null))
                        _viewerTypes.Add(((ViewerInfo)attr).Extension, (type, ((ViewerInfo)attr).NeedsCompilation));
                }
            }
        }

        public IViewer GetViewer(ContentFile item)
        {
            bool needsCompilation;
            IViewer view;
            if (_viewers.TryGetValue(Path.GetExtension(item.FilePath), out var viewer))
            {
                view = viewer.viewer;
                needsCompilation = viewer.needsCompilation;
            }
            else if (_viewerTypes.TryGetValue(Path.GetExtension(item.FilePath), out var viewerInfo))
            {
                view = (IViewer)Activator.CreateInstance(viewerInfo.type);
                needsCompilation = viewerInfo.needsCompilation;
                _viewers.Add(Path.GetExtension(item.FilePath), (view, viewerInfo.needsCompilation));
            }
            else
                return null;

            if (needsCompilation)
            {
                
            }
            return view;
        }


        public void Dispose()
        {
            foreach (var (key, viewer) in _viewers)
            {
                viewer.viewer?.Dispose();
            }
        }
    }
}
