using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace W0.Ui.HtmlRender.Demo
{
    public partial class SampleForm : Form
    {
        public SampleForm()
        {
            InitializeComponent();
        }

        private void htmlLabel1_Click(object sender, EventArgs e)
        {
            pGrid.SelectedObject = htmlLabel1;
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            var bmp = new Bitmap(10, 10);

            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.FillRectangle(SystemBrushes.Control, new Rectangle(0, 0, 5, 5));
                g.FillRectangle(SystemBrushes.Control, new Rectangle(5, 5, 5, 5));
            }

            e.Graphics.DrawImage(bmp, PointF.Empty);

            using (var b = new TextureBrush(bmp, WrapMode.Tile))
            {
                e.Graphics.FillRectangle(b, panel1.ClientRectangle);
            }

            bmp.Dispose();
        }

        private void htmlPanel1_Click(object sender, EventArgs e)
        {
            pGrid.SelectedObject = htmlPanel1;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            pGrid.SelectedObject = button1;
            htmlToolTip1.SetToolTip(button1, htmlLabel1.Text);
        }
    }
}