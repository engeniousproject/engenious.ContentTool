using System.Threading.Tasks;
using engenious.Content.Models;

namespace engenious.ContentTool
{
    public class Delegates
    {
        public delegate Task ItemActionEventHandler(ContentItem item);

        public delegate Task FolderAddActionEventHandler(ContentFolder folder);

        public delegate Task AddSpecificFilesEventHandler(ContentFolder fld, string[] files, int destinationIndex = -1);

        public delegate Task AddSpecificFolderEventHandler(ContentFolder fld, string src, int destinationIndex = -1);
    }
}