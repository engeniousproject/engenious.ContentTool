using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ContentTool.Dialog
{
    public partial class NewFolderDialog : Form
    {
        public string Name { get; set; }

        public NewFolderDialog()
        {
            InitializeComponent();
            button_ok.Enabled = false;
        }

        private void button_ok_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Name = textBox_name.Text;
            Close();
        }

        private void button_cancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Name = textBox_name.Text;
            Close();
        }

        private void textBox_name_TextChanged(object sender, EventArgs e)
        {
            if (textBox_name.Text == "")
                button_ok.Enabled = false;
            else
                button_ok.Enabled = true;
        }
    }
}
