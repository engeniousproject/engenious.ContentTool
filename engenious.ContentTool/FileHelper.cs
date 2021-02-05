using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using engenious.ContentTool.Forms;
using engenious.ContentTool.Models;

namespace engenious.ContentTool
{
    [Flags]
    public enum FileAction
    {
        Ask = 1,
        Overwrite = 2,
        Repeat = 4,
        Skip = 8
    }

    class FileHelper
    {
        public static string GetRelativePath(string parentPath, string subPath)
        {
            try
            {
                var parentUri = new Uri(parentPath);
                var subUri = new Uri(subPath);
                var relUri = parentUri.MakeRelativeUri(subUri);
                return relUri.ToString();
            }
            catch (Exception ex)
            {
                return subPath;
            }
        }
        public static async Task<FileAction> CopyDirectory(string src, string dest, ContentFolder fld, IPromptShell promptShell,
            FileAction action = FileAction.Ask, Action<int> progress=null)
        {
            var foldername = Path.GetFileName(src);
            dest = Path.Combine(dest, foldername);
            Directory.CreateDirectory(dest);

            var newfld = (ContentFolder) (fld.Content.FirstOrDefault(x => x.Name == foldername && x is ContentFolder));
            if (newfld == null)
            {
                newfld = new ContentFolder(foldername, fld);
                fld.Content.Add(newfld);
            }
            fld = newfld;

            action = await CopyFiles(Directory.GetFiles(src), dest, fld, promptShell, action);
            var dirs = Directory.GetDirectories(src);
            for (int i = 0; i < dirs.Length; i++)
            {
                var dir = dirs[i];
                action = await CopyDirectory(dir, dest, fld, promptShell, action, null);
                progress?.Invoke((int) (i * 100.0f / dirs.Length));
            }
            return action;
        }

        public static async Task<FileAction> CopyFiles(string[] files, string dir, ContentFolder fld, IPromptShell promptShell,
            FileAction action = FileAction.Ask)
        {
            foreach (var src in files)
            {
                var filename = Path.GetFileName(src);
                var dest = Path.Combine(dir, filename);
                if (Path.GetFullPath(dest) != Path.GetFullPath(src))
                {
                    //TODO: warning file exists

                    bool repeat = true, next = false;
                    while (repeat)
                    {
                        repeat = false;
                        if (File.Exists(dest))
                        {
                            if (action.HasFlag(FileAction.Ask))
                            {
                                action = await promptShell.ShowOverwriteDialog(filename);
                            }
                            switch ((FileAction) ((int) action & 0xE))
                            {
                                case FileAction.Overwrite:
                                    File.Copy(src, dest, true);
                                    break;
                                case FileAction.Skip:
                                    next = true;
                                    break;
                                case FileAction.Repeat:
                                    repeat = true;
                                    break;
                            }
                        }
                        else
                        {
                            File.Copy(src, dest);
                        }
                    }
                    if (next)
                        continue;
                }
                promptShell.Invoke(() =>
                {
                    if (!fld.Content.Any(x => x.Name == filename && x is ContentFile))
                        fld.Content.Add(new ContentFile(filename, fld));
                });
            }
            return action;
        }
    }
}