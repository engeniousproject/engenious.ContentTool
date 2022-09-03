using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Dialogs;
using Avalonia.Logging;
using engenious.ContentTool;
using engenious.ContentTool.Avalonia;
using engenious.ContentTool.Forms;

[assembly: ShellFactory(typeof(MainShellFactory))]
namespace engenious.ContentTool.Avalonia
{
    public class MainShellFactory : IShellFactory
    {
        public static AppBuilder BuildAvaloniaApp() 
            => AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace(LogEventLevel.Debug, LogArea.Binding);

        private App _app;
        private readonly AutoResetEvent _waitEvent = new AutoResetEvent(false);
        private IPromptShell _promptShell;

        public MainShellFactory()
        {
            BuildAvaloniaApp().Start(AppMain, null);
        }

        private void AppMain(Application app, string[] args)
        {
            _app = app as App;
            _waitEvent.Set();
        }

        public IMainShell CreateMainShell()
        {
            _waitEvent.WaitOne();
            return new MainWindow(_app);
        }

        public IPromptShell CreatePromptShell()
        {
            return _promptShell ??= new PromptShell();
        }
    }
}