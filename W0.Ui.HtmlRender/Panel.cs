using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Text;
using System.Reflection;
using System.Windows.Forms;
using W0.Ui.HtmlRender.Css;
using Rectangle = System.Drawing.Rectangle;

namespace W0.Ui.HtmlRender
{
    public class Panel
        : ScrollableControl
    {
        #region Fields

        protected InitialContainer _htmlContainer;

        #endregion

        #region Ctor

        /// <summary>
        ///     Creates a new HtmlPanel
        /// </summary>
        public Panel()
        {
            _htmlContainer = new InitialContainer();

            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Opaque, true);
            //SetStyle(ControlStyles.Selectable, true);

            DoubleBuffered = true;

            BackColor = SystemColors.Window;
            AutoScroll = true;

            Renderer.AddReference(Assembly.GetCallingAssembly());
        }

        #endregion

        #region Properties

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override bool AutoSize
        {
            get { return base.AutoSize; }
            set { base.AutoSize = value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override bool AutoScroll
        {
            get { return base.AutoScroll; }
            set { base.AutoScroll = value; }
        }

        /// <summary>
        ///     Gets the Initial HtmlContainer of this HtmlPanel
        /// </summary>
        public InitialContainer HtmlContainer
        {
            get { return _htmlContainer; }
        }

        /// <summary>
        ///     Gets or sets the text of this panel
        /// </summary>
        [Editor(
            "System.Windows.Forms.Design.ListControlStringCollectionEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            typeof (UITypeEditor)),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Visible), Localizable(true), Browsable(true),
         EditorBrowsable(EditorBrowsableState.Always)]
        public override string Text
        {
            get { return base.Text; }
            set
            {
                base.Text = value;

                CreateFragment();
                MeasureBounds();
                Invalidate();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Creates the fragment of HTML that is rendered
        /// </summary>
        protected virtual void CreateFragment()
        {
            _htmlContainer = new InitialContainer(Text);
        }

        /// <summary>
        ///     Measures the bounds of the container
        /// </summary>
        public virtual void MeasureBounds()
        {
            _htmlContainer.SetBounds(this is Label ? new Rectangle(0, 0, 10, 10) : ClientRectangle);

            using (var g = CreateGraphics())
            {
                _htmlContainer.MeasureBounds(g);
            }

            AutoScrollMinSize = Size.Round(_htmlContainer.MaximumSize);
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);

            Focus();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            MeasureBounds();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!(this is Label)) e.Graphics.Clear(SystemColors.Window);


            _htmlContainer.ScrollOffset = AutoScrollPosition;
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            _htmlContainer.Paint(e.Graphics);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            foreach (var box in _htmlContainer.LinkRegions.Keys)
            {
                var rect = _htmlContainer.LinkRegions[box];
                if (Rectangle.Round(rect).Contains(e.X, e.Y))
                {
                    Cursor = Cursors.Hand;
                    return;
                }
            }

            Cursor = Cursors.Default;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            foreach (var box in _htmlContainer.LinkRegions.Keys)
            {
                var rect = _htmlContainer.LinkRegions[box];
                if (Rectangle.Round(rect).Contains(e.X, e.Y))
                {
                    Value.GoLink(box.GetAttribute("href", string.Empty));
                    return;
                }
            }
        }

        #endregion
    }
}