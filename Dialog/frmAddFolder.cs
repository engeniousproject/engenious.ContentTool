using System;
using System.Windows.Forms;
using System.Linq;

namespace ContentTool
{
    public partial class frmAddFolder:Form
    {
        public frmAddFolder()
        {
            InitializeComponent();
        }

        void TxtName_TextChanged(object sender, EventArgs e)
        {
            //TODO: valid chars on linux are invalid on win
            btnOk.Enabled = txtName.TextLength > 0 && !System.IO.Path.GetInvalidFileNameChars().Any(x => txtName.Text.Contains(x));
        }

        public string FolderName{ get { return txtName.Text; } }
    }
}

