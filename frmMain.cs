using System;
using System.Windows.Forms;
using System.Collections.Generic;
using engenious.Graphics;
using System.Linq;
using System.Collections.Specialized;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace ContentTool
{
    public partial class frmMain : Form
    {

        //TODO: architecture
        private string currentFile;
        private ContentProject currentProject;

        private List<string> toClean;



        public frmMain()
        {
            InitializeComponent();

            toClean = new List<string>();
            treeMap = new Dictionary<ContentItem, TreeNode>();
        }

        void FrmMain_Load(object sender, System.EventArgs e)
        {
            foreach (string file in System.Environment.GetCommandLineArgs().Skip(1))
            {
                if (System.IO.Path.GetExtension(file) == ".ecp" && System.IO.File.Exists(file))
                {
                    OpenFile(file);
                    return;
                }
            }
        }

        private bool CloseFile(string message = "Do you really want to close?")
        {
            if (isBuilding)
            {
                if (MessageBox.Show("Your Project is currently Building." + message, "Close file", MessageBoxButtons.YesNo) == DialogResult.No)
                    return false;

                AbortBuilding();
            }


            currentFile = null;
            currentProject = null;
            RecalcTreeView();
            return true;
        }

        private void SaveFile(string file)
        {
            currentFile = file;
            currentProject.Name = file;
            ContentProject.Save(file, currentProject);
        }

        private void OpenFile(string file)
        {
            currentFile = file;
            currentProject = ContentProject.Load(file);
            currentProject.CollectionChanged += CurrentProject_CollectionChanged;
            currentProject.PropertyChanged += CurrentProject_PropertyChanged;
            RecalcTreeView();
        }

        private Dictionary<ContentItem,TreeNode> treeMap;

        void CurrentProject_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            ContentItem item = sender as ContentItem;
            if (item == null)
                return;
            if (e.PropertyName == "Name")
            {
                TreeNode node = null;
                treeMap.TryGetValue(item, out node);
                if (node == null)
                    return;
                node.Text = item.Name;
            }
        }

        void CurrentProject_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ContentItem item = (e.NewItems == null || e.NewItems.Count == 0) ? null : (e.NewItems[0] as ContentItem);
            if (item == null)
                item = (e.OldItems == null || e.OldItems.Count == 0) ? null : (e.OldItems[0] as ContentItem);
            TreeNode parentNode = null;
            if (!treeMap.TryGetValue(item.Parent, out parentNode))
                return;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        var node = new TreeNode(item.Name){ Tag = item };
                        node.SelectedImageKey = node.ImageKey = getImageKey(item);
                        treeMap.Add(item, node);
                        if (parentNode == null)
                            treeContentFiles.Nodes.Add(node);
                        else
                        {
                            parentNode.Nodes.Add(node);
                            parentNode.Expand();
                        }
                        break;
                    }
                case NotifyCollectionChangedAction.Remove:
                    {
                        TreeNode node = null;
                        treeMap.TryGetValue(item, out node);
                        if (parentNode == null)
                            treeContentFiles.Nodes.Remove(node);
                        else
                            parentNode.Nodes.Remove(node);
                        break;
                    }
                case NotifyCollectionChangedAction.Replace:
                    {
                        ContentItem oldItem = (e.OldItems == null || e.OldItems.Count == 0) ? null : (e.OldItems[0] as ContentItem);
                        TreeNode node = null;
                        treeMap.TryGetValue(oldItem, out node);
                        if (parentNode == null)
                            treeContentFiles.Nodes.Remove(node);
                        else
                            parentNode.Nodes.Remove(node);

                        node = new TreeNode(item.Name){ Tag = item };
                        node.SelectedImageKey = node.ImageKey = getImageKey(item);
                        treeMap.Add(item, node);
                        if (parentNode == null)
                            treeContentFiles.Nodes.Add(node);
                        else
                        {
                            parentNode.Nodes.Add(node);
                            parentNode.Expand();
                        }
                        break;
                    }
                case NotifyCollectionChangedAction.Reset:
                    {
                        treeContentFiles.Nodes[0].Nodes.Clear();
                        treeMap.Clear();
                        break;
                    }
            }
        }

        private void RecalcTreeView()
        {
            treeMap.Clear();
            treeContentFiles.Nodes.Clear();
            if (currentProject == null)
            {
                buildMainMenuItem.Enabled = false;
                return;
            }
            buildMainMenuItem.Enabled = true;

            var rootNode = new TreeNode(currentProject.Name){ Tag = currentProject };
            treeContentFiles.Nodes.Add(rootNode);
            treeMap.Add(currentProject, rootNode);
            AddTreeNode(currentProject, rootNode);
        }

        private string getImageKey(ContentItem item)
        {
            if (item is ContentProject)
                return "project";
            if (item is ContentFolder)
                return "folder";
            if (item is ContentFile)
            {
                string ext = System.IO.Path.GetExtension(item.Name);
                if (!imgList.Images.ContainsKey(ext))
                {
                    string filePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(currentFile), item.getPath());
                    if (System.IO.File.Exists(filePath))
                        imgList.Images.Add(ext, System.Drawing.Icon.ExtractAssociatedIcon(filePath));
                }
                return ext;
                //return "file";
            }
            
            return "error";
        }

        private void AddTreeNode(ContentFolder content, TreeNode node)
        {
            if (content == null)
                return;
            node.Expand();

            foreach (var item in content.Contents)
            {
                var treeNode = new TreeNode(item.Name){ Tag = item };
                treeNode.SelectedImageKey = treeNode.ImageKey = getImageKey(item);
                node.Nodes.Add(treeNode);
                treeMap.Add(item, treeNode);
                AddTreeNode(item as ContentFolder, treeNode);
            }
        }


        void FileMenuItem_DropDownOpening(object sender, System.EventArgs e)
        {
            bool fileOpened = !string.IsNullOrEmpty(currentFile) || currentProject != null;
            this.importMenuItem.Enabled = false;
            this.closeMenuItem.Enabled = this.saveAsMenuItem.Enabled = fileOpened;
            this.saveMenuItem.Enabled = fileOpened;//TODO änderungen?
            
        }


        void SaveAsMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Engenious Content Project(.ecp)|*.ecp";
            sfd.FileName = currentFile;
            sfd.OverwritePrompt = true;
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                SaveFile(sfd.FileName);
            }
        }

        void SaveMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentFile) || !System.IO.File.Exists(currentFile))
            {
                SaveAsMenuItem_Click(sender, e);
                return;
            }
            SaveFile(currentFile);
        }

        void ImportMenuItem_Click(object sender, EventArgs e)
        {

        }

        void CloseMenuItem_Click(object sender, EventArgs e)
        {
            currentFile = "";
            CloseFile();

        }

        void OpenMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Engenious Content Project(.ecp)|*.ecp";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                OpenFile(ofd.FileName);
            }
        }

        void NewMenuItem_Click(object sender, EventArgs e)
        {
            CloseMenuItem_Click(sender, e);

            currentProject = new ContentProject();
            currentProject.CollectionChanged += CurrentProject_CollectionChanged;
            currentProject.PropertyChanged += CurrentProject_PropertyChanged;


            RecalcTreeView();

            if (!SaveFirst())
                currentProject = null;
            RecalcTreeView();
        }

        private void ExitMenuItem_Click(object sender, EventArgs args)
        {
            Close();
        }


        void CancelMenuItem_Click(object sender, EventArgs e)
        {
            AbortBuilding();
        }

        private void Log(string message, bool error = false)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new MethodInvoker(delegate()
                        {
                            Log(message, error);
                        }));
                return;
            }
            txtLog.AppendText(message + "\n");
        }

        private void Clean()
        {
            StartBuilding();
            Log("Cleaning started");
            buildingThread = new System.Threading.Thread(new System.Threading.ThreadStart(delegate()
                    {
                        foreach (string item in toClean)
                        {
                            Log("Cleaning " + item);
                            System.IO.File.Delete(item);
                        }
                        Log("Cleaning complete");
                        EndBuilding();
                    }));
            buildingThread.Start();
        }



        private void Build()
        {
            StartBuilding();
            Log("Building started");
            buildingThread = new System.Threading.Thread(new System.Threading.ThreadStart(delegate()
                    {
                        string relOut = string.Format(currentProject.OutputDir.Replace("{Configuration}", "{0}"), currentProject.Configuration.ToString());
                        string outputDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(currentFile), relOut);
                        CreateFolderIfNeeded(outputDir);
                        PipelineHelper.PreBuilt(currentProject);
                        using (engenious.Content.Pipeline.ContentImporterContext importerContext = new engenious.Content.Pipeline.ContentImporterContext())
                        using (engenious.Content.Pipeline.ContentProcessorContext processorContext = new engenious.Content.Pipeline.ContentProcessorContext())
                        {
                            foreach (var item in treeMap.Keys)
                            {
                                if (item is ContentFile)
                                {
                                    ContentFile contentFile = item as ContentFile;
                                    string importFile = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(currentFile), item.getPath());
                                    string destFile = System.IO.Path.Combine(outputDir, System.IO.Path.GetDirectoryName(item.getPath()), System.IO.Path.GetFileNameWithoutExtension(item.Name) + ".ego");
                                    CreateFolderIfNeeded(destFile);

                                    var importer = PipelineHelper.CreateImporter(System.IO.Path.GetExtension(item.Name));
                                    if (importer == null)
                                        continue;

                                    object importerOutput = importer.Import(importFile, importerContext);
                                    if (importerOutput == null)
                                        continue;

                                    engenious.Content.Pipeline.IContentProcessor processor = PipelineHelper.CreateProcessor(importer.GetType(), contentFile.Processor);
                                    if (processor == null)
                                        continue;

                                    object processedData = processor.Process(importerOutput, processorContext);

                                    if (processedData == null)
                                        continue;

                                    engenious.Content.Serialization.IContentTypeWriter typeWriter = engenious.Content.Serialization.SerializationManager.Instance.GetWriter(processedData.GetType());
                                    engenious.Content.ContentFile outputFileWriter = new engenious.Content.ContentFile(typeWriter.RuntimeReaderName);

                                    BinaryFormatter formatter = new BinaryFormatter();
                                    using (FileStream fs = new FileStream(destFile, FileMode.Create, FileAccess.Write))
                                    {
                                        formatter.Serialize(fs, outputFileWriter);
                                        engenious.Content.Serialization.ContentWriter writer = new engenious.Content.Serialization.ContentWriter(fs);
                                        writer.WriteObject(processedData, typeWriter);
                                    }

                                    toClean.Add(destFile);
                                    Log("Built " + item.getPath());
                                }
                            }
                        }
                        //System.Threading.Thread.Sleep(8000);


                        Log("Building complete");



                        EndBuilding();
                    }));
            buildingThread.Start();
        }

        public static void CreateFolderIfNeeded(string filename)
        {
            string folder = System.IO.Path.GetDirectoryName(filename);
            if (!System.IO.Directory.Exists(folder))
            {
                CreateFolderIfNeeded(folder);
                System.IO.Directory.CreateDirectory(folder);
            }
        }

        private bool isBuilding = false;
        private System.Threading.Thread buildingThread;

        private void StartBuilding()
        {
            isBuilding = true;
            this.buildMenuItem.Enabled = false;
            this.rebuildMenuItem.Enabled = false;
            this.cleanMenuItem.Enabled = false;
            this.cancelMenuItem.Enabled = true;
        }

        private void AbortBuilding()
        {
            if (!isBuilding)
                return;
            buildingThread.Abort();//TODO: better solution?
            Log("Aborted", true);
            EndBuilding();
        }

        private void EndBuilding()
        {
            
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(delegate()
                        {
                            EndBuilding();
                        }));
                return;
            }
            isBuilding = false;
            this.buildMenuItem.Enabled = true;
            this.rebuildMenuItem.Enabled = true;
            this.cleanMenuItem.Enabled = true;
            this.cancelMenuItem.Enabled = false;
        }

        void CleanMenuItem_Click(object sender, EventArgs e)
        {
            txtLog.Text = "";

            Clean();
        }

        void RebuildMenuItem_Click(object sender, EventArgs e)
        {
            txtLog.Text = "";
            Clean();
            Build();
        }

        void BuildMenuItem_Click(object sender, EventArgs e)
        {
            if (!SaveFirst())
                return;
            txtLog.Text = "";
            Build();
        }

        private bool SaveFirst()
        {
            if (string.IsNullOrEmpty(currentFile))
            {
                SaveMenuItem_Click(saveMenuItem, new EventArgs());
            }
            return System.IO.File.Exists(currentFile);
        }

        void BuildMainMenuItem_DropDownOpening(object sender, System.EventArgs e)
        {
            cleanMenuItem.Enabled = toClean.Count > 0;
            rebuildMenuItem.Enabled = System.IO.File.Exists(currentFile) && cleanMenuItem.Enabled;
        }

        void DeleteMenuItem_Click(object sender, EventArgs e)
        {
            ContentItem item = getSelectedItem();
            var parent = item.Parent as ContentFolder;
            parent.Contents.Remove(item);
        }

        void RenameMenuItem_Click(object sender, EventArgs e)
        {
            ContentItem item = getSelectedItem();
            if (item is ContentProject)
                return;
            TreeNode node = null;
            if (treeMap.TryGetValue(item, out node))
            {
                
                node.BeginEdit();
            }
        }

        void TreeContentFiles_BeforeLabelEdit(object sender, System.Windows.Forms.NodeLabelEditEventArgs e)
        {
            ContentItem item = null;
            if (e.Node != null && e.Node.Tag != null)
                item = e.Node.Tag as ContentItem;
            if (item == null)
            {
                e.CancelEdit = true;
                return;
            }
            if (item is ContentProject)
                e.CancelEdit = true;
        }

        void TreeContentFiles_AfterLabelEdit(object sender, System.Windows.Forms.NodeLabelEditEventArgs e)
        {
            e.CancelEdit = true;
            return;
            //TODO: implement
            /*ContentItem item = null;
            if (e.Node != null && e.Node.Tag != null)
                item = e.Node.Tag as ContentItem;
            if (item == null)
            {
                e.CancelEdit = true;
                return;
            }
            string absoluteFolder = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(currentFile), item.Parent.getPath());
            string absoluteItem = System.IO.Path.Combine(absoluteFolder, item.Name);
            string newAbsItem = System.IO.Path.Combine(absoluteFolder, e.Label);
            if (System.IO.Directory.Exists(absoluteItem))
            {
                System.IO.Directory.Move(absoluteItem, newAbsItem);
                item.Name = e.Label;
            }
            else if (System.IO.File.Exists(absoluteItem))
            {
                System.IO.File.Move(absoluteItem, newAbsItem);

                item.Name = e.Label;
            }
            else
            {
                e.CancelEdit = true;
            }*/

        }

        void ExistingFolderMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void makeDirectory(string relativePath, string directoryName)
        {
            string[] splt = directoryName.Split(new char[]{ System.IO.Path.DirectorySeparatorChar }, 2);

            string curPath = System.IO.Path.Combine(relativePath, splt[0]);
            System.IO.Directory.CreateDirectory(curPath);
            if (splt.Length > 1)
                makeDirectory(curPath, splt[1]);
        }

        void ExistingItemMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = true;
            ofd.Filter = "All files|*.*|Image files(.png;.bmp;.jpg)|*.png;*.bmp;*.jpg";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                ContentItem item = getSelectedItem();
                ContentFolder curFolder = item as ContentFolder;
                if (curFolder == null)
                    curFolder = item.Parent as ContentFolder;
                if (curFolder == null)
                    return;
                string absolutePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(currentFile), curFolder.getPath());
                makeDirectory(System.IO.Path.GetDirectoryName(currentFile), curFolder.getPath());
                foreach (string file in ofd.FileNames)
                {
                    string destination = System.IO.Path.Combine(absolutePath, System.IO.Path.GetFileName(file));
                    if (destination != file)
                    {
                        System.IO.File.Copy(file, destination);
                    }
                    curFolder.Contents.Add(new ContentFile(System.IO.Path.GetFileName(file), curFolder));
                }
            }
        }

        private ContentItem getSelectedItem()
        {
            var node = treeContentFiles.SelectedNode;
            if (node == null)
            {
                if (treeContentFiles.Nodes.Count == 0)
                    return null;
                node = treeContentFiles.Nodes[0];
            }
            if (node.Tag == null)
                return null;
            return node.Tag as ContentItem;
        }

        void NewFolderMenuItem_Click(object sender, EventArgs e)
        {
            frmAddFolder folder = new frmAddFolder();
            if (folder.ShowDialog() == DialogResult.OK)
            {
                ContentItem selectedItem = getSelectedItem();
                ContentFolder currentFolder = selectedItem as ContentFolder;
                if (currentFolder == null)
                    currentFolder = selectedItem.Parent as ContentFolder;
                if (currentFolder == null)
                    return;
                currentFolder.Contents.Add(new ContentFolder(folder.FolderName, currentFolder));
            }
        }

        void NewItemMenuItem_Click(object sender, EventArgs e)
        {

        }

        void EditMenuItem_DropDownOpening(object sender, System.EventArgs e)
        {
            bool fileOpened = !string.IsNullOrEmpty(currentFile) || currentProject != null;
            addMenuItem.Enabled = fileOpened;
            renameMenuItem.Enabled = deleteMenuItem.Enabled = fileOpened && treeContentFiles.SelectedNode != null && treeContentFiles.SelectedNode != treeContentFiles.Nodes[0];
        }

        void RedoMenuItem_Click(object sender, EventArgs e)
        {

        }

        void UndoMenuItem_Click(object sender, EventArgs e)
        {

        }


        void FrmMain_FormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
        {
            e.Cancel = !CloseFile();

        }
    }
}

