using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace W0.Ui.HtmlRender
{
    /// <summary>
    ///     Provides HTML rendering on the tooltips
    /// </summary>
    public class ToolTip
        : System.Windows.Forms.ToolTip
    {
        #region Fields

        private InitialContainer container;

        #endregion

        #region Ctor

        public ToolTip()
        {
            OwnerDraw = true;

            Popup += HtmlToolTip_Popup;
            Draw += HtmlToolTip_Draw;
        }

        #endregion

        private void HtmlToolTip_Popup(object sender, PopupEventArgs e)
        {
            var text = GetToolTip(e.AssociatedControl);
            var font = string.Format(NumberFormatInfo.InvariantInfo, "font: {0}pt {1}", e.AssociatedControl.Font.Size,
                e.AssociatedControl.Font.FontFamily.Name);

            //Create fragment container
            container =
                new InitialContainer("<table class=htmltooltipbackground cellspacing=5 cellpadding=0 style=\"" + font +
                                     "\"><tr><td style=border:0px>" + text + "</td></tr></table>");
            container.SetBounds(new Rectangle(0, 0, 10, 10));
            container.AvoidGeometryAntialias = true;

            //Measure bounds of the container
            using (var g = e.AssociatedControl.CreateGraphics())
            {
                container.MeasureBounds(g);
            }

            //Set the size of the tooltip
            e.ToolTipSize = Size.Round(container.MaximumSize);
        }

        private void HtmlToolTip_Draw(object sender, DrawToolTipEventArgs e)
        {
            e.Graphics.Clear(Color.White);

            if (container != null)
            {
                //Draw HTML!
                container.Paint(e.Graphics);
            }
        }
    }
}