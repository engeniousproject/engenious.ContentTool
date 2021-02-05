using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using engenious.ContentTool.Forms;
using MessageBox.Avalonia.Enums;

namespace engenious.ContentTool.Avalonia
{
    public class PromptShell : IPromptShell
    {
        private static ButtonEnum TranslateMessageBoxButtons(MessageBoxButtons buttons)
        {
            return buttons switch
            {
                MessageBoxButtons.Ok => ButtonEnum.Ok,
                MessageBoxButtons.OkCancel => ButtonEnum.OkCancel,
                MessageBoxButtons.YesNo => ButtonEnum.YesNo,
                MessageBoxButtons.YesNoCancel => ButtonEnum.YesNoCancel,
                _ => throw new InvalidEnumArgumentException(nameof(buttons))
            };
        }

        private static Icon TranslateMessageBoxType(MessageBoxType type)
        {
            return type switch
            {
                MessageBoxType.Error => Icon.Error,
                MessageBoxType.Info => Icon.Info,
                MessageBoxType.None => Icon.None,
                MessageBoxType.Warning => Icon.Warning,
                MessageBoxType.Question => Icon.Battery,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        private static MessageBoxResult TranslateMessageBoxResult(ButtonResult result)
        {
            return result switch
            {
                ButtonResult.Ok => MessageBoxResult.Ok,
                ButtonResult.Cancel => MessageBoxResult.Cancel,
                ButtonResult.Yes => MessageBoxResult.Yes,
                _ => MessageBoxResult.Cancel
            };
        }
        public async Task<MessageBoxResult> ShowMessageBox(string text, string title, MessageBoxButtons buttons = MessageBoxButtons.Ok,
            MessageBoxType type = MessageBoxType.None, object parent = null)
        {
            var msg = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(title, text,
                TranslateMessageBoxButtons(buttons), TranslateMessageBoxType(type));
            var res = (parent is global::Avalonia.Controls.Window parentWindow) ? await msg.ShowDialog(parentWindow).ConfigureAwait(false) : await msg.Show().ConfigureAwait(false);
            return TranslateMessageBoxResult(res);
        }

        public async Task<FileAction> ShowOverwriteDialog(string fileName)
        {
            throw new NotImplementedException();
        }

        public void Invoke(Action action)
        {
            Dispatcher.UIThread.Post(action);
        }
    }
}