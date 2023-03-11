using System;
using System.Threading.Tasks;

namespace engenious.ContentTool.Forms
{
    public enum MessageBoxType
    {
        None,
        Warning,
        Error,
        Question,
        Info
    }

    public enum MessageBoxResult
    {
        Ok,
        Yes,
        No,
        Cancel
    }
    
    public enum MessageBoxButtons
    {
        Ok,
        OkCancel,
        YesNo,
        YesNoCancel,
    }
    
    public interface IPromptShell
    {
        Task<MessageBoxResult> ShowMessageBox(string text, string title, MessageBoxButtons buttons = MessageBoxButtons.Ok,
            MessageBoxType type = MessageBoxType.None, object? parent = null);
        
        Task<FileAction> ShowOverwriteDialog(string fileName);

        void Invoke(Action action);
    }
}