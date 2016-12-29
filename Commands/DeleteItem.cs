using ContentTool.Items;

namespace ContentTool.Commands
{
    public static class DeleteItem
    {
        public static void Execute(ContentItem item)
        {
            item.Parent.Contents.Remove(item);
        }
    }
}