using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;  //This is for the registry
using System.Collections.Specialized;

namespace WindowsFormsApplication1
{
    public partial class frmMain : Form
    {
        bool FocusTreeview = false;
        string szSubFolder = "";
        //string szRoot = @"C:\";  This will not work because it is not built to handel that many directories.
        string szRoot = @"C:\A-InProgress\";
        string strParent = "";
        
        private ListViewColumnSorter lvwColumnSorter;
        FileSystemWatcher watcher = new FileSystemWatcher();

        public frmMain()
        {
            InitializeComponent();
            this.treeView1.NodeMouseClick += new TreeNodeMouseClickEventHandler(this.treeView1_NodeMouseClick);

            //contextMenuStrip1.ItemClicked += new ToolStripItemClickedEventHandler(contextMenuStrip1_ItemClicked);

            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);

            listView1.AllowDrop = true;

            // Create an instance of a ListView column sorter and assign it 
            // to the ListView control.
            lvwColumnSorter = new ListViewColumnSorter();
            this.listView1.ListViewItemSorter = lvwColumnSorter;
            PopulateTreeView();
        }

        /* Start here */

        private void PopulateTreeView()
        {
            treeView1.Nodes.Clear();

            if (Directory.Exists(szRoot) == false)
            {
                MessageBox.Show("The root directory does not exist on this computer." + Environment.NewLine + szRoot);
                return;
            }

            TreeNode rootSubNode;
            TreeNode rootNode;
            
            DirectoryInfo info = new DirectoryInfo(szRoot);
            if (info.Exists)
            {
                rootNode = new TreeNode(info.Name);
                rootNode.Tag = info;
                GetDirectories(info.GetDirectories(), rootNode);
                treeView1.Nodes.Add(rootNode);
                treeView1.Nodes[0].Expand();
                NodeSelect(rootNode);
                CreateFileWatcher(szRoot);
            }

            /* If there is currently a subfolder selected, reselect it. */

            if (szSubFolder != "")
            {
                if (Directory.Exists(info + szSubFolder) == true)
                {
                    string nodeDirectory = szSubFolder;
                    DirectoryInfo temp = new DirectoryInfo(info + nodeDirectory);
                    //Get the last directory
                    if (nodeDirectory.EndsWith(@"\") == true)
                    {
                        nodeDirectory = nodeDirectory.Substring(0, nodeDirectory.Length - 1);
                    }
                    int iIndex = nodeDirectory.LastIndexOf(@"\");
                    iIndex++;
                    nodeDirectory = nodeDirectory.Substring(iIndex);

                    rootSubNode = new TreeNode(nodeDirectory);
                    rootSubNode.Tag = temp;
                    NodeSelect(rootSubNode);
                }
                else
                {
                    /* If we get here, the subfolder that we were in got deleted. So clear out the subFolder variable */
                    szSubFolder = "";
                }
            }
            listView1.Columns[1].Width = 0;

        }

        /********************************************/

        private void GetDirectories(DirectoryInfo[] subDirs, TreeNode nodeToAddTo)
        {
            /* Load all the subdirectories under the root node into the treeview */

            TreeNode aNode;
            DirectoryInfo[] subSubDirs;

            if (strParent == "")
            {
                strParent = nodeToAddTo.Text + "\\";
            }

            foreach (DirectoryInfo subDir in subDirs)
            {
                aNode = new TreeNode(subDir.Name, 0, 0);
                aNode.Name = subDir.ToString();
                aNode.Tag = subDir;
                aNode.ImageKey = "Directory";
                try
                {
                    subSubDirs = subDir.GetDirectories();

                    if (subSubDirs.Length != 0)
                    {
                        GetDirectories(subSubDirs, aNode);
                    }
                    nodeToAddTo.Nodes.Add(aNode);
                }
                catch
                { }

            }
        }

        /********************************************/

        private void NodeSelect(TreeNode newSelected)
        {
            try
            {
                /* Load all the files and directories in the selected node from the treeview */

                listView1.Items.Clear();
                DirectoryInfo nodeDirInfo = (DirectoryInfo)newSelected.Tag;
                ListViewItem.ListViewSubItem[] subItems;
                ListViewItem item = null;

                /* Make sure the directory exists */

                if (nodeDirInfo == null)
                {
                    return;
                }

                DirectoryInfo info = null;
                if (szRoot != "")
                {
                    info = new DirectoryInfo(szRoot);
                }

                /* List the directories */

                if (Directory.Exists(info + szSubFolder) == true)
                {
                    foreach (DirectoryInfo dir in nodeDirInfo.GetDirectories())
                    {
                        item = new ListViewItem(dir.Name, 0);

                        int fCount = Directory.GetFiles(dir.FullName, "*", SearchOption.TopDirectoryOnly).Length;

                        subItems = new ListViewItem.ListViewSubItem[]
                            {new ListViewItem.ListViewSubItem(item, "Directory"), 
                                new ListViewItem.ListViewSubItem(item, dir.LastAccessTime.ToShortDateString()),
                                new ListViewItem.ListViewSubItem(item, fCount.ToString() + " Files")};
                        item.SubItems.AddRange(subItems);
                        listView1.Items.Add(item);
                    }

                    /* List the files */

                    foreach (FileInfo file in nodeDirInfo.GetFiles())
                    {
                        item = new ListViewItem(file.Name, 1);

                        /* Get and format the file size */

                        string strFileSize = " KB";
                        if (file.Length >= 1000)
                        {
                            double result = file.Length / 1000;
                            strFileSize = result.ToString() + strFileSize;
                        }
                        else
                        {
                            strFileSize = file.Length.ToString() + " Bytes";
                        }

                        subItems = new ListViewItem.ListViewSubItem[] { new ListViewItem.ListViewSubItem(item, "File"), new ListViewItem.ListViewSubItem(item, file.LastAccessTime.ToShortDateString()), new ListViewItem.ListViewSubItem(item, strFileSize) };

                        item.SubItems.AddRange(subItems);
                        listView1.Items.Add(item);
                    }
                }
                listView1.Visible = false;
                listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                listView1.Columns[1].Width = 0;
                listView1.Visible = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /********************************************/

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            TreeNode newSelected = e.Node;

            /* Record the path that is being navigated */

            if (e.Node.Parent == null)
            {
                szSubFolder = "";
            }
            else
            {
                string strFullPath = e.Node.FullPath.ToString();
                strFullPath = strFullPath.Replace(strParent, "");
                szSubFolder = strFullPath + "\\";
            }

            /* Select the node*/

            NodeSelect(newSelected);
        }

        /********************************************/

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                var item = listView1.SelectedItems[0];

                /* Don't do anything with files.  Those are opened with a double click */

                if (item.SubItems[1].Text == "File")
                {
                    return;
                }

                if (item.SubItems[1].Text == "Directory")
                {

                    TreeNode[] tns = treeView1.Nodes.Find(item.Text, true);
                    if (tns.Length > 0)
                    {

                        /* Record the path that is being navigated */

                        if (szSubFolder == "")
                        {
                            szSubFolder = item.Text + "\\";
                        }
                        else
                        {
                            szSubFolder = szSubFolder + item.Text + "\\";
                        }

                        /* Select the node in the tree */

                        treeView1.Focus();
                        NodeSelect(tns[0]);
                        treeView1.SelectedNode = tns[0];
                        tns[0].Expand();
                        FocusTreeview = true;
                    }
                    return;
                }
            }
        }

        /********************************************/

        private void listView1_MouseUp(object sender, MouseEventArgs e)
        {
            if (FocusTreeview == true)
            {
                treeView1.Focus();
                FocusTreeview = false;
            }
        }

        /********************************************/

        private void listView1_DoubleClick(object sender, EventArgs e)
        {

            if (listView1.SelectedItems.Count > 0)
            {
                var item = listView1.SelectedItems[0];
                
                if (item.SubItems[1].Text == "Directory")
                {

                    TreeNode[] tns = treeView1.Nodes.Find(item.Text, true);
                    if (tns.Length > 0)
                    {

                        /* Record the path that is being navigated */

                        if (szSubFolder == "")
                        {
                            szSubFolder = item.Text + "\\";
                        }
                        else
                        {
                            szSubFolder = szSubFolder + item.Text + "\\";
                        }

                        /* Select the node in the tree */

                        treeView1.Focus();
                        NodeSelect(tns[0]);
                        treeView1.SelectedNode = tns[0];
                        tns[0].Expand();
                        FocusTreeview = true;
                    }
                    return;
                }

                /* This will open the files that are double clicked  in the listview */

                if (item.SubItems[1].Text == "File")
                {
                    Process.Start(szRoot + szSubFolder + item.Text);
                }

            }
        }

        /**********************************************************************************************************/
        public void CreateFileWatcher(string path)
        {
            /* Create a new FileSystemWatcher and set its properties. */

            watcher.Path = path;
            watcher.IncludeSubdirectories = true;

            watcher.SynchronizingObject = this;

            /* Watch for changes in LastAccess and LastWrite times, and the renaming of files or directories. */

            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;

            /* Apply a file mask */

            //watcher.Filter = "*.txt";

            /* Begin watching. */
            
            watcher.EnableRaisingEvents = true;
        }
        /*********************************************************************************************************/
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
            //Console.WriteLine("File: " + e.FullPath + " " + e.ChangeType);
            PopulateTreeView();
            return;
        }
        private void OnRenamed(object source, RenamedEventArgs e)
        {
            // Specify what is done when a file is renamed.
            //Console.WriteLine("File: {0} renamed to {1}", e.OldFullPath, e.FullPath);
            PopulateTreeView();
            return;
        }
        /*********************************************************************************************************/

        private void frmMain_Load(object sender, EventArgs e)
        {
            string szValue = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\matt\Explorer--", "SplitterDistance", "38");
            splitContainer1.SplitterDistance = Convert.ToInt16(szValue);
        }

        /*********************************************************************************************************/

        private void splitContainer1_SplitterMoved_1(object sender, SplitterEventArgs e)
        {
            RegistryKey key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\matt\Explorer--");
            key.SetValue("SplitterDistance", splitContainer1.SplitterDistance.ToString());
            key.Close();
            key.Dispose();
        }

        /*********************************************************************************************************/

        private void listView1_DragEnter(object sender, DragEventArgs e)
        {
            /* You must handle the effect */
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.All;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        /*********************************************************************************************************/

        private void listView1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string filePath in files)
                {

                    if (File.Exists(szRoot + szSubFolder + Path.GetFileName(filePath)) == true)
                    {
                        if (MessageBox.Show("That file already exists in the job folder." + Environment.NewLine + "Do you want to overwrite it?", "Overwrite?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            try
                            {
                                File.Delete(szRoot + szSubFolder + Path.GetFileName(filePath));
                                File.Copy(filePath, szRoot + szSubFolder + Path.GetFileName(filePath));
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Unable to overwrite file " + Environment.NewLine + szRoot + szSubFolder + Path.GetFileName(filePath) + Environment.NewLine + ex.Message);
                            }
                        }
                    }
                    else
                    {
                        File.Copy(filePath, szRoot + szSubFolder + Path.GetFileName(filePath));
                    }
                    
                }
                PopulateTreeView();
            }
        }

        /*********************************************************************************************************/

        private void listView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                /* Check if any items are selected */
                if (listView1.SelectedItems.Count == 0)
                {
                    MessageBox.Show("You must select a file to delete.");
                }

                var item = listView1.SelectedItems[0];

                if (MessageBox.Show("Are you sure you want to delete " + Environment.NewLine + item.Text, "Are you sure?", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No)
                {
                    e.Handled = true;
                    return;
                }

                /* Delete the selected item */
                try
                {
                    try
                    {
                        File.Delete(szRoot + szSubFolder + item.Text);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                e.Handled = true;
            }
        }

        /*********************************************************************************************************/

        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Determine if clicked column is already the column that is being sorted.
            if (e.Column == lvwColumnSorter.SortColumn)
            {
                // Reverse the current sort direction for this column.
                if (lvwColumnSorter.Order == SortOrder.Ascending)
                {
                    lvwColumnSorter.Order = SortOrder.Descending;
                }
                else
                {
                    lvwColumnSorter.Order = SortOrder.Ascending;
                }
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                lvwColumnSorter.SortColumn = e.Column;
                lvwColumnSorter.Order = SortOrder.Ascending;
            }

            // Perform the sort with these new sort options.
            this.listView1.Sort();
        }

        /*********************************************************************************************************/

        private void listView1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (listView1.FocusedItem.Bounds.Contains(e.Location) == true)
                {
                    contextMenuStrip1.Show(Cursor.Position);
                }
            }
        }

        /*********************************************************************************************************/

        private void contextMenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            ToolStripItem menuItem = e.ClickedItem;

            if (menuItem.Text == "Delete")
            {
                /* Check if any items are selected */
                if (listView1.SelectedItems.Count == 0)
                {
                    MessageBox.Show("You must select a file to delete.");
                }

                foreach (ListViewItem item in listView1.SelectedItems)
                {
                    /* Delete the selected item */
                    try
                    {
                        try
                        {
                            File.Delete(szRoot + szSubFolder + item.Text);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
            else if (menuItem.Text == "Copy")
            {
                StringCollection paths = new StringCollection();
                foreach (ListViewItem item in listView1.SelectedItems)
                {
                    paths.Add(szRoot + szSubFolder + item.Text);
                }
                Clipboard.SetFileDropList(paths);
            }
            else if (menuItem.Text == "Paste")
            {
                StringCollection paths = new StringCollection();
                paths = Clipboard.GetFileDropList();
                foreach (string file in paths)
                {
                    if (File.Exists(file) == true)
                    {
                        try
                        {
                            File.Copy(file, szRoot + szSubFolder + Path.GetFileName(file));
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
                }
            }
        }

        /*********************************************************************************************************/

    }
}
