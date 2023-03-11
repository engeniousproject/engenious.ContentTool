using System.Threading.Tasks;
using engenious.Content.Models;

namespace engenious.ContentTool
{
    public class Delegates
    {
        public delegate Task ItemActionEventHandler(ContentItem item);

        public delegate Task FolderAddActionEventHandler(ContentFolder folder);
    }
}