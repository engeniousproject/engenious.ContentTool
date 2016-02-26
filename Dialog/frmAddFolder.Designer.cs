using System;

namespace ContentTool
{
    public partial class frmAddFolder
    {
        public void InitializeComponent()
        {
            lblCaption = new System.Windows.Forms.Label();
            lblCaption.Text = "Folder name:";
            lblCaption.Left = 6;
            lblCaption.Top = 6;
            lblCaption.AutoSize = true;

            txtName = new System.Windows.Forms.TextBox();
            txtName.MaxLength = 128;
            txtName.Top = 6;
            txtName.Left = lblCaption.Right + 6;
            txtName.Width = this.ClientSize.Width - txtName.Left - 6;
            txtName.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            txtName.TextChanged += TxtName_TextChanged;


            btnOk = new System.Windows.Forms.Button();
            btnOk.Text = "Add";
            btnOk.Left = txtName.Right - btnOk.Width;
            btnOk.Top = this.ClientSize.Height - 6 - btnOk.Height;
            btnOk.Anchor = System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Bottom;
            btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnOk.Enabled = false;

            btnCancel = new System.Windows.Forms.Button();
            btnCancel.Text = "Cancel";
            btnCancel.Left = btnOk.Left - btnCancel.Width - 6;
            btnCancel.Top = btnOk.Top;
            btnCancel.Anchor = System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Bottom;

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;


            int titleHeight = this.Height - this.ClientSize.Height;
            this.MinimumSize = new System.Drawing.Size(lblCaption.Width + 18 + 175, txtName.Bottom + btnOk.Height + titleHeight + 12);
            this.MaximumSize = new System.Drawing.Size(short.MaxValue, this.MinimumSize.Height);
            this.Controls.Add(lblCaption);
            this.Controls.Add(txtName);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);
            this.Text = "Add folder";
        }


        private System.Windows.Forms.Label lblCaption;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
    }
}

