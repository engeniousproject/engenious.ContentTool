using engenious.ContentTool.Models;

namespace engenious.ContentTool
{
    public class Delegates
    {
        public delegate void ItemActionEventHandler(ContentItem item);

        public delegate void FolderAddActionEventHandler(ContentFolder folder);
    }
}