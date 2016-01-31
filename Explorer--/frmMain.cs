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

namespace WindowsFormsApplication1
{
    public partial class frmMain : Form
    {
        bool FocusTreeview = false;
        string szSubFolder = "";
        //string szRoot = @"C:\github\";
        string szRoot = @"C:\A-InProgress\";
        string strParent = "";

        public frmMain()
        {
            InitializeComponent();
            PopulateTreeView();
            this.treeView1.NodeMouseClick += new TreeNodeMouseClickEventHandler(this.treeView1_NodeMouseClick);
        }

        /* Start here */

        private void PopulateTreeView()
        {
            if (Directory.Exists(szRoot) == false)
            {
                MessageBox.Show("The root directory does not exist on this computer." + Environment.NewLine + szRoot);
                return;
            }

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
            /* Load all the files and directories in the selected node from the treeview */

            listView1.Items.Clear();
            DirectoryInfo nodeDirInfo = (DirectoryInfo)newSelected.Tag;
            ListViewItem.ListViewSubItem[] subItems;
            ListViewItem item = null;

            /* List the directories */

            foreach (DirectoryInfo dir in nodeDirInfo.GetDirectories())
            {
                item = new ListViewItem(dir.Name, 0);
                subItems = new ListViewItem.ListViewSubItem[]
                  {new ListViewItem.ListViewSubItem(item, "Directory"), 
                   new ListViewItem.ListViewSubItem(item, 
                dir.LastAccessTime.ToShortDateString())};
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
            listView1.Visible = false;
            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            listView1.Columns[1].Width = 0;
            listView1.Visible = true;
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

        private void frmMain_Load(object sender, EventArgs e)
        {
            string szValue = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\matt\Explorer--", "SplitterDistance", "38");
            splitContainer1.SplitterDistance = Convert.ToInt16(szValue);
        }

        private void splitContainer1_SplitterMoved_1(object sender, SplitterEventArgs e)
        {
            RegistryKey key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\matt\Explorer--");
            key.SetValue("SplitterDistance", splitContainer1.SplitterDistance.ToString());
            key.Close();
            key.Dispose();
        }
    }
}
