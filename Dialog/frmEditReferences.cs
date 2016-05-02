using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ContentTool.Dialog
{
    public partial class frmEditReferences : Form
    {
        public frmEditReferences()
        {
            InitializeComponent();
        }

        private void frmEditReferences_Load(object sender, EventArgs e)
        {

        }

        public string RootDir { get; internal set; }
        private ObservableCollection<string> references = new ObservableCollection<string>();
        private List<string> original;
        public List<string> References
        {
            get
            {
                return references.ToList();
            }
            internal set
            {
                original = value;
                references = new ObservableCollection<string>(value);
                btnOk.Enabled = Changed;
                references.CollectionChanged += References_CollectionChanged;
                lstReferences.DataSource = references;
            }
        }

        private void References_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            btnOk.Enabled = Changed;
        }

        private bool Changed
        {
            get
            {
                if (original.Count != references.Count)
                    return true;
                for (int i = 0; i < original.Count; i++)
                    if (original[i] != references[i])
                        return true;
                return false;
            }
        }
        private string[] currentFiles;
        private string[] relativePaths;
        private string[] CurrentFiles
        {
            get
            {
                return cbRelative.Checked ? relativePaths : currentFiles;
            }
            set
            {
                Uri root = new Uri(RootDir + System.IO.Path.DirectorySeparatorChar, UriKind.Absolute);
                currentFiles = value;
                int i = 0;
                relativePaths = new string[value.Length];
                foreach (var file in value)
                {
                    Uri uri = new Uri(file, UriKind.Absolute);
                    relativePaths[i] = root.MakeRelativeUri(uri).ToString();
                    i++;
                }
            }
        }
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = ".Net Assembly(.dll)|*.dll";
            ofd.Multiselect = true;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                CurrentFiles = ofd.FileNames;
                txtPath.Text = string.Join(";", CurrentFiles);
            }
        }

        private void cbRelative_CheckedChanged(object sender, EventArgs e)
        {
            txtPath.Text = string.Join(";", CurrentFiles);
        }

        private void txtPath_TextChanged(object sender, EventArgs e)
        {
            /*if (txtPath.Text.Length > 1)
                CurrentFiles = txtPath.Text.Split(';');
            else
                CurrentFiles = new string[] { };*/
            btnAdd.Enabled = txtPath.Text.Length > 1;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            foreach (var path in CurrentFiles)
            {
                references.Add(path);
            }
            txtPath.Text = "";
            lstReferences.DataSource = null;
            lstReferences.DataSource = references;
            CurrentFiles = new string[] { };
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            foreach (var obj in lstReferences.SelectedItems)
                if (obj != null)
                    references.Remove(obj.ToString());
            lstReferences.SelectedItem = null;

            lstReferences.DataSource = null;
            lstReferences.DataSource = references;
            btnRemove.Enabled = lstReferences.SelectedItems.Count > 0;
        }

        private void lstReferences_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnRemove.Enabled = lstReferences.SelectedItems.Count > 0;
        }
    }
}
