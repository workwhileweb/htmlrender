using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace W0.Ui.HtmlRender.Demo
{
    public partial class MainForm : Form
    {
        #region Ctor

        public MainForm()
        {
            InitializeComponent();

            //Rtf font
            rtf.Font = new Font(FontFamily.GenericMonospace, 10);

            //Window bounds
            StartPosition = FormStartPosition.Manual;
            Bounds = Screen.GetWorkingArea(Point.Empty);

            //Load samples
            LoadSamples();
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Loads the tree of document samples
        /// </summary>
        private void LoadSamples()
        {
            var root = new TreeNode("Sample Documents");
            tv.Nodes.Add(root);
            var names = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            var extensionFilter = ".htm";

            Array.Sort(names);

            foreach (var name in names)
            {
                var extPos = name.LastIndexOf('.');
                var namePos = extPos > 0 && name.Length > 1 ? name.LastIndexOf('.', extPos - 1) : 0;
                var ext = name.Substring(extPos >= 0 ? extPos : 0);
                var shortName = namePos > 0 && name.Length > 2
                    ? name.Substring(namePos + 1, name.Length - namePos - ext.Length - 1)
                    : name;

                if (string.IsNullOrEmpty(extensionFilter)
                    || extensionFilter.IndexOf(ext, StringComparison.Ordinal) >= 0)
                {
                    var node = new TreeNode(shortName);
                    root.Nodes.Add(node);
                    node.Tag = name;
                }
            }

            root.Expand();

            if (root.Nodes.Count > 0)
            {
                tv.SelectedNode = root.Nodes[0];
                tv_NodeMouseClick(this, new TreeNodeMouseClickEventArgs(root.Nodes[0], MouseButtons.None, 0, 0, 0));
            }
        }

        #endregion

        private void rtf_TextChanged(object sender, EventArgs e)
        {
            if (updateLock) return;

            tmrCount = 0;
            tmrRepaint.Enabled = true;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
        }

        private void tv_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            var resource = Convert.ToString(e.Node.Tag);

            if (string.IsNullOrEmpty(resource)) return;

            using (
                var sreader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(resource),
                    Encoding.Default))
            {
                var html = sreader.ReadToEnd();
                SyntaxHilight.AddColoredText(html, rtf);

                updateLock = true;
                Application.UseWaitCursor = true;
                panel.Text = html;
                Application.UseWaitCursor = false;
                updateLock = false;
            }
        }

        private void colorPicker1_ColorSelected(object sender, EventArgs e)
        {
            var c = colorPicker1.SelectedColor;
            rtf.SelectedText = string.Format("#{0}{1}{2}",
                (c.R < 10 ? "0" : string.Empty) + c.R.ToString("x"),
                (c.G < 10 ? "0" : string.Empty) + c.G.ToString("x"),
                (c.B < 10 ? "0" : string.Empty) + c.B.ToString("x"));
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Alt && e.KeyCode == Keys.C)
            {
                colorPicker1.Visible = !colorPicker1.Visible;
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void tmrRepaint_Tick(object sender, EventArgs e)
        {
            tmrCount++;

            if (tmrCount >= MAX)
            {
                tmrCount = 0;
                tmrRepaint.Enabled = false;
                panel.Text = rtf.Text;
                Console.WriteLine("Parse");
            }
        }

        #region Fields

        private const int MAX = 5;
        private int tmrCount;
        private bool updateLock;

        #endregion
    }
}