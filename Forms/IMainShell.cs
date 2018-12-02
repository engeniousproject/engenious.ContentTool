using System;
using System.Drawing;
using System.Windows.Forms;
using engenious.ContentTool.Models;
using engenious.ContentTool.Viewer;
using static engenious.ContentTool.Delegates;

namespace engenious.ContentTool.Forms
{
    public interface IMainShell
    {
        IViewer CurrentViewer { get; }
        ContentProject Project { get; set; }
        bool IsRenderingSuspended { get; }

        void Invoke(Delegate d);
        void BeginInvoke(Delegate d);

        bool ShowCloseWithoutSavingConfirmation();
        string ShowOpenDialog();
        string ShowSaveAsDialog();
        string ShowIntegrateDialog();

        void WriteLog(string text, System.Drawing.Color color = default(System.Drawing.Color));
        void WriteLineLog(string text, System.Drawing.Color color = default(System.Drawing.Color));
        void ClearLog();
        void ShowLog();
        void HideLog();

        void ShowLoading(string title = "Please wait...");
        void HideLoading();

        void ShowViewer(IViewer viewer,ContentFile file);
        void HideViewer();

        void RenameItem(ContentItem item);
        void RemoveItem(ContentItem item);

        void ShowAbout();

        bool ShowNotFoundDelete();

        void ReloadView();

        void WaitProgress(int progress);

        void SuspendRendering();
        void ResumeRendering();

        string ShowFolderSelectDialog();
        string[] ShowFileSelectDialog();

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
        event EventHandler OpenProjectClick;
        event Delegates.ItemActionEventHandler CloseProjectClick;
        event Delegates.ItemActionEventHandler SaveProjectClick;
        event Delegates.ItemActionEventHandler SaveProjectAsClick;

        event Delegates.ItemActionEventHandler OnItemSelect;
        
        event EventHandler IntegrateCSClick;

        event EventHandler OnAboutClick;
        event EventHandler OnHelpClick;
    }
}
