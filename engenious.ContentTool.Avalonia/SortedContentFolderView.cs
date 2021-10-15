using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using engenious.Content.Models;

namespace engenious.ContentTool.Avalonia
{
    public class SortedContentFolderView : ObservableCollection<ContentItem>
    {
        
        public SortedContentFolderView(ContentItem item)
        {
            if (item is ContentFolder folder)
            {
            }

            Item = item;
        }

        private static int CompareViews(SortedContentFolderView aView, SortedContentFolderView bView)
        {
            var a = aView.Item;
            var b = bView.Item;

            if (a is ContentFolder)
            {
                if (b is ContentFolder)
                    return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
                return 1;
            }

            if (b is ContentFolder)
            {
                return -1;
            }

            return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
        }
        
        public ContentItem Item { get; }
        
        public SortedCollectionView<SortedContentFolderView> Content { get; }
    }
}