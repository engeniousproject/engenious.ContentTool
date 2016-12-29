using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ContentTool.Builder;
using ContentTool.Items;

namespace ContentTool.Commands
{
    public static class BuildItem
    {
        public static void Execute(ContentItem selectedItem, ContentBuilder builder)
        {
            builder.Build(selectedItem);
        }
    }
}
