using System;
using ContentTool.Items;

namespace ContentTool.Builder
{
    public class ItemProgressEventArgs
    {
        public ItemProgressEventArgs(BuildStep buildStep,ContentItem item)
        {
            this.BuildStep = buildStep;
            this.Item = item;
        }
        public BuildStep BuildStep{get;private set;}
        public ContentItem Item{get;private set;}
    }
}

