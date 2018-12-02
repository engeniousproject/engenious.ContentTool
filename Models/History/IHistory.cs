using System;

namespace engenious.ContentTool.Models.History
{
    public interface IHistory : IHistoryItem
    {
        event EventHandler HistoryChanged;
        event EventHandler HistoryItemAdded;

        void Push(IHistoryItem item);
        
        bool CanUndo { get; }
        bool CanRedo { get; }
    }
}