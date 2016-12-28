using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ContentTool.Dialog;

namespace ContentTool.Commands
{
    public static class AddNewFolder
    {
        public static void Execute(ContentItem selectedItem, string currentFile)
        {
            var addFolderForm = new NewFolderDialog();

            var curFolder = selectedItem as ContentFolder ?? selectedItem.Parent;


            if (addFolderForm.ShowDialog() == DialogResult.OK)
            {
                var name = addFolderForm.Name;
                var itemPath = Path.Combine(Path.GetDirectoryName(currentFile), selectedItem.getPath());
                var folderPath = Path.Combine(itemPath, name);

                //Check if directory already exists
                if (Directory.Exists(folderPath))
                {
                    MessageBox.Show("Directory already exists!", "Directory exists", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    Directory.CreateDirectory(folderPath);
                    curFolder.AddSubFolder(folderPath, folderPath);
                }
                catch (Exception e)
                {
                    MessageBox.Show("An Error occured during the operation.", "Error", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }
    }
}
