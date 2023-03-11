using System;
using System.Threading.Tasks;
using engenious.Content.Models;
using engenious.ContentTool.Viewer;
using engenious.Graphics;

namespace engenious.ContentTool.Forms
{
    public interface IMainShell : IDisposable
    {
        void Run();
        
        IViewer CurrentViewer { get; }
        ContentProject Project { get; set; }
        bool IsRenderingSuspended { get; }

        void Invoke(Action d);
        void BeginInvoke(Action d);

        Task<bool> ShowCloseWithoutSavingConfirmation();
        Task<string> ShowOpenDialog();
        Task<string> ShowSaveAsDialog();
        Task<string> ShowIntegrateDialog();

        Task WriteLog(string text, LogType logType = LogType.Information, engenious.Color color = default(engenious.Color));
        Task WriteLineLog(string text, LogType logType = LogType.Information, engenious.Color color = default(engenious.Color));
        Task ClearLog();
        Task ShowLog();
        Task HideLog();

        Task ShowLoading(string title = "Please wait...");
        Task HideLoading();

        Task ShowViewer(IViewer viewer,ContentFile file);
        Task HideViewer();

        Task RenameItem(ContentItem item);
        Task RemoveItem(ContentItem item);

        Task ShowAbout();

        Task<bool> ShowNotFoundDelete();

        Task ReloadView();

        Task WaitProgress(int progress);

        Task SuspendRendering();
        Task ResumeRendering();

        Task<string> ShowFolderSelectDialog();
        Task<string[]> ShowFileSelectDialog();

        event EventHandler ViewReloaded;

        event EventHandler UndoClick;
        event EventHandler RedoClick;

        event Delegates.ItemActionEventHandler BuildItemClick;
        event Delegates.ItemActionEventHandler ShowInExplorerItemClick;
        event Delegates.ItemActionEventHandler RemoveItemClick;
        event Delegates.ItemActionEventHandler RenameItemClick;

        event EventHandler OnShellLoad;

        event EventHandler RebuildClick;
        event EventHandler CleanClick;

        event Delegates.FolderAddActionEventHandler AddExistingFolderClick;
        event Delegates.FolderAddActionEventHandler AddNewFolderClick;
        event Delegates.FolderAddActionEventHandler AddExistingItemClick;

        event EventHandler NewProjectClick;
        event Func<string, Task> OpenProjectClick;
        event Delegates.ItemActionEventHandler CloseProjectClick;
        event Delegates.ItemActionEventHandler SaveProjectClick;
        event Delegates.ItemActionEventHandler SaveProjectAsClick;

        event Delegates.ItemActionEventHandler OnItemSelect;
        
        event EventHandler IntegrateCSClick;

        event EventHandler OnAboutClick;
        event EventHandler OnHelpClick;
    }
}
