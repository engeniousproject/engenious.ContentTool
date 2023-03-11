using engenious.ContentTool.Forms;

namespace engenious.ContentTool
{
    public interface IShellFactory
    {
        IMainShell CreateMainShell();

        IPromptShell CreatePromptShell();
    }
}