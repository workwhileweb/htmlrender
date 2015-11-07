using System;
using System.Collections.Generic;
using System.Drawing;

namespace W0.Ui.HtmlRender.Css
{
    /// <summary>
    ///     Helps on CSS Layout
    /// </summary>
    internal static class LayoutEngine
    {
        #region Fields

        private static BoxWord _lastTreatedWord;

        #endregion

        #region Inline Boxes

        /// <summary>
        ///     Creates line boxes for the specified blockbox
        /// </summary>
        /// <param name="g"></param>
        /// <param name="blockBox"></param>
        public static void CreateLineBoxes(Graphics g, Box blockBox)
        {
            blockBox.LineBoxes.Clear();

            var maxRight = blockBox.ActualRight - blockBox.ActualPaddingRight - blockBox.ActualBorderRightWidth;

            //Get the start x and y of the blockBox
            var startx = blockBox.Location.X + blockBox.ActualPaddingLeft - 0 + blockBox.ActualBorderLeftWidth;
            //TODO: Check for floats
            var starty = blockBox.Location.Y + blockBox.ActualPaddingTop - 0 + blockBox.ActualBorderTopWidth;
            var curx = startx + blockBox.ActualTextIndent;
            var cury = starty;

            //Reminds the maximum bottom reached
            var maxBottom = starty;

            //Extra amount of spacing that should be applied to lines when breaking them.
            var lineSpacing = 0f;

            //First line box
            var line = new LineBox(blockBox);

            //Flow words and boxes
            FlowBox(g, blockBox, blockBox, maxRight, lineSpacing, startx, ref line, ref curx, ref cury, ref maxBottom);

            //Gets the rectangles foreach linebox
            foreach (var linebox in blockBox.LineBoxes)
            {
                BubbleRectangles(blockBox, linebox);
                linebox.AssignRectanglesToBoxes();
                ApplyAlignment(g, linebox);
                if (blockBox.Direction == Constants.Rtl) ApplyRightToLeft(linebox);

                //linebox.DrawRectangles(g);
            }

            blockBox.ActualBottom = maxBottom + blockBox.ActualPaddingBottom + blockBox.ActualBorderBottomWidth;
        }

        /// <summary>
        ///     Recursively flows the content of the box using the inline model
        /// </summary>
        /// <param name="g">Device Info</param>
        /// <param name="blockbox">Blockbox that contains the text flow</param>
        /// <param name="box">Current box to flow its content</param>
        /// <param name="maxright">Maximum reached right</param>
        /// <param name="linespacing">Space to use between rows of text</param>
        /// <param name="startx">x starting coordinate for when breaking lines of text</param>
        /// <param name="line">Current linebox being used</param>
        /// <param name="curx">Current x coordinate that will be the left of the next word</param>
        /// <param name="cury">Current y coordinate that will be the top of the next word</param>
        /// <param name="maxbottom">Maximum bottom reached so far</param>
        private static void FlowBox(Graphics g, Box blockbox, Box box, float maxright, float linespacing,
            float startx, ref LineBox line, ref float curx, ref float cury, ref float maxbottom)
        {
            box.FirstHostingLineBox = line;

            foreach (var b in box.Boxes)
            {
                var leftspacing = b.ActualMarginLeft + b.ActualBorderLeftWidth + b.ActualPaddingLeft;
                var rightspacing = b.ActualMarginRight + b.ActualBorderRightWidth + b.ActualPaddingRight;
                var topspacing = b.ActualBorderTopWidth + b.ActualPaddingTop;
                var bottomspacing = b.ActualBorderBottomWidth + b.ActualPaddingTop;

                b.RectanglesReset();
                b.MeasureWordsSize(g);

                curx += leftspacing;

                if (b.Words.Count > 0)
                {
                    #region Flow words

                    foreach (var word in b.Words)
                    {
                        //curx += word.SpacesBeforeWidth;

                        if ((b.WhiteSpace != Constants.Nowrap && curx + word.Width + rightspacing > maxright) ||
                            word.IsLineBreak)
                        {
                            #region Break line

                            curx = startx;
                            cury = maxbottom + linespacing;

                            line = new LineBox(blockbox);

                            if (word.IsImage || word.Equals(b.FirstWord))
                            {
                                curx += leftspacing;
                            }

                            #endregion
                        }

                        line.ReportExistanceOf(word);

                        word.Left = curx; // -word.LastMeasureOffset.X + 1;
                        word.Top = cury; // - word.LastMeasureOffset.Y;

                        curx = word.Right; // +word.SpacesAfterWidth;
                        maxbottom = Math.Max(maxbottom, word.Bottom);
                        //+ (word.IsImage ? topspacing + bottomspacing : 0));

                        _lastTreatedWord = word;
                    }

                    #endregion
                }
                else
                {
                    FlowBox(g, blockbox, b, maxright, linespacing, startx, ref line, ref curx, ref cury, ref maxbottom);
                }

                curx += rightspacing;
            }

            box.LastHostingLineBox = line;
        }

        /// <summary>
        ///     Recursively creates the rectangles of the blockBox, by bubbling from deep to outside of the boxes
        ///     in the rectangle structure
        /// </summary>
        private static void BubbleRectangles(Box box, LineBox line)
        {
            if (box.Words.Count > 0)
            {
                float x = float.MaxValue, y = float.MaxValue, r = float.MinValue, b = float.MinValue;
                var words = line.WordsOf(box);

                if (words.Count > 0)
                {
                    foreach (var word in words)
                    {
                        x = Math.Min(x, word.Left); // - word.SpacesBeforeWidth);
                        r = Math.Max(r, word.Right); // + word.SpacesAfterWidth);
                        y = Math.Min(y, word.Top);
                        b = Math.Max(b, word.Bottom);
                    }
                    line.UpdateRectangle(box, x, y, r, b);
                }
            }
            else
            {
                foreach (var b in box.Boxes)
                {
                    BubbleRectangles(b, line);
                }
            }
        }

        /// <summary>
        ///     Gets the white space width of the specified box
        /// </summary>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static float WhiteSpace(Graphics g, Box b)
        {
            var space = " .";
            var w = 0f;
            var onError = 5f;

            var sf = new StringFormat();
            sf.SetMeasurableCharacterRanges(new[] {new CharacterRange(0, 1)});
            var regs = g.MeasureCharacterRanges(space, b.ActualFont,
                new RectangleF(0, 0, float.MaxValue, float.MaxValue), sf);

            if (regs == null || regs.Length == 0) return onError;

            w = regs[0].GetBounds(g).Width;

            if (!(string.IsNullOrEmpty(b.WordSpacing) || b.WordSpacing == Constants.Normal))
            {
                w += Value.ParseLength(b.WordSpacing, 0, b);
            }
            return w;
        }

        /// <summary>
        ///     Applies vertical and horizontal alignment to words in lineboxes
        /// </summary>
        /// <param name="g"></param>
        /// <param name="lineBox"></param>
        private static void ApplyAlignment(Graphics g, LineBox lineBox)
        {
            #region Horizontal alignment

            switch (lineBox.OwnerBox.TextAlign)
            {
                case Constants.Right:
                    ApplyRightAlignment(g, lineBox);
                    break;
                case Constants.Center:
                    ApplyCenterAlignment(g, lineBox);
                    break;
                case Constants.Justify:
                    ApplyJustifyAlignment(g, lineBox);
                    break;
                default:
                    ApplyLeftAlignment(g, lineBox);
                    break;
            }

            #endregion

            ApplyVerticalAlignment(g, lineBox);
        }

        /// <summary>
        ///     Applies right to left direction to words
        /// </summary>
        /// <param name="line"></param>
        private static void ApplyRightToLeft(LineBox line)
        {
            var left = line.OwnerBox.ClientLeft;
            var right = line.OwnerBox.ClientRight;

            foreach (var word in line.Words)
            {
                var diff = word.Left - left;
                var wright = right - diff;
                word.Left = wright - word.Width;
            }
        }

        /// <summary>
        ///     Gets the ascent of the font
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        /// <remarks>
        ///     Font metrics from http://msdn.microsoft.com/en-us/library/xwf9s90b(VS.71).aspx
        /// </remarks>
        public static float GetAscent(Font f)
        {
            var mainAscent = f.Size*f.FontFamily.GetCellAscent(f.Style)/f.FontFamily.GetEmHeight(f.Style);
            return mainAscent;
        }

        /// <summary>
        ///     Gets the descent of the font
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        /// <remarks>
        ///     Font metrics from http://msdn.microsoft.com/en-us/library/xwf9s90b(VS.71).aspx
        /// </remarks>
        public static float GetDescent(Font f)
        {
            var mainDescent = f.Size*f.FontFamily.GetCellDescent(f.Style)/f.FontFamily.GetEmHeight(f.Style);
            return mainDescent;
        }

        /// <summary>
        ///     Gets the line spacing of the font
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        /// <remarks>
        ///     Font metrics from http://msdn.microsoft.com/en-us/library/xwf9s90b(VS.71).aspx
        /// </remarks>
        public static float GetLineSpacing(Font f)
        {
            var s = f.Size*f.FontFamily.GetLineSpacing(f.Style)/f.FontFamily.GetEmHeight(f.Style);
            return s;
        }

        /// <summary>
        ///     Applies vertical alignment to the linebox
        /// </summary>
        /// <param name="g"></param>
        /// <param name="lineBox"></param>
        private static void ApplyVerticalAlignment(Graphics g, LineBox lineBox)
        {
            var isTableCell = lineBox.OwnerBox.Display == Constants.TableCell;
            var baseline = lineBox.GetMaxWordBottom() - GetDescent(lineBox.OwnerBox.ActualFont) - 2;
            var boxes = new List<Box>(lineBox.Rectangles.Keys);

            foreach (var b in boxes)
            {
                var ascent = GetAscent(b.ActualFont);
                var descent = GetDescent(b.ActualFont);

                //Important notes on http://www.w3.org/TR/CSS21/tables.html#height-layout
                switch (b.VerticalAlign)
                {
                    case Constants.Sub:
                        lineBox.SetBaseLine(g, b, baseline + lineBox.Rectangles[b].Height*.2f);
                        break;
                    case Constants.Super:
                        lineBox.SetBaseLine(g, b, baseline - lineBox.Rectangles[b].Height*.2f);
                        break;
                    case Constants.TextTop:

                        break;
                    case Constants.TextBottom:

                        break;
                    case Constants.Top:

                        break;
                    case Constants.Bottom:

                        break;
                    case Constants.Middle:

                        break;
                    default:
                        //case: baseline
                        lineBox.SetBaseLine(g, b, baseline);
                        break;
                }

                ////Graphic cues
                //g.FillRectangle(Brushes.Aqua, r.Left, r.Top, r.Width, ascent);
                //g.FillRectangle(Brushes.Yellow, r.Left, r.Top + ascent, r.Width, descent);
                //g.DrawLine(Pens.Fuchsia, r.Left, baseline, r.Right, baseline);
            }
        }

        /// <summary>
        ///     Applies special vertical alignment for table-cells
        /// </summary>
        /// <param name="g"></param>
        /// <param name="cell"></param>
        public static void ApplyCellVerticalAlignment(Graphics g, Box cell)
        {
            if (cell.VerticalAlign == Constants.Top || cell.VerticalAlign == Constants.Baseline) return;

            var celltop = cell.ClientTop;
            var cellbot = cell.ClientBottom;
            var bottom = cell.GetMaximumBottom(cell, 0f);
            var dist = 0f;

            if (cell.VerticalAlign == Constants.Bottom)
            {
                dist = cellbot - bottom;
            }
            else if (cell.VerticalAlign == Constants.Middle)
            {
                dist = (cellbot - bottom)/2;
            }

            foreach (var b in cell.Boxes)
            {
                b.OffsetTop(dist);
            }

            //float top = cell.ClientTop;
            //float bottom = cell.ClientBottom;
            //bool middle = cell.VerticalAlign == CssConstants.Middle;

            //foreach (LineBox line in cell.LineBoxes)
            //{
            //    for (int i = 0; i < line.RelatedBoxes.Count; i++)
            //    {

            //        float diff = bottom - line.RelatedBoxes[i].Rectangles[line].Bottom;
            //        if (middle) diff /= 2f;
            //        RectangleF r = line.RelatedBoxes[i].Rectangles[line];
            //        line.RelatedBoxes[i].Rectangles[line] = new RectangleF(r.X, r.Y + diff, r.Width, r.Height);

            //    }

            //    foreach (BoxWord word in line.Words)
            //    {
            //        float gap = word.Top - top;
            //        word.Top = bottom - gap - word.Height;
            //    }
            //}
        }

        /// <summary>
        ///     Applies centered alignment to the text on the linebox
        /// </summary>
        /// <param name="g"></param>
        /// <param name="lineBox"></param>
        private static void ApplyJustifyAlignment(Graphics g, LineBox lineBox)
        {
            if (lineBox.Equals(lineBox.OwnerBox.LineBoxes[lineBox.OwnerBox.LineBoxes.Count - 1])) return;

            var indent = lineBox.Equals(lineBox.OwnerBox.LineBoxes[0]) ? lineBox.OwnerBox.ActualTextIndent : 0f;
            var textSum = 0f;
            var words = 0f;
            var availWidth = lineBox.OwnerBox.ClientRectangle.Width - indent;

            #region Gather text sum

            foreach (var w in lineBox.Words)
            {
                textSum += w.Width;
                words += 1f;
            }

            #endregion

            if (words <= 0f) return; //Avoid Zero division
            var spacing = (availWidth - textSum)/words; //Spacing that will be used
            var curx = lineBox.OwnerBox.ClientLeft + indent;

            foreach (var word in lineBox.Words)
            {
                word.Left = curx;
                curx = word.Right + spacing;

                if (word == lineBox.Words[lineBox.Words.Count - 1])
                {
                    word.Left = lineBox.OwnerBox.ClientRight - word.Width;
                }

                //TODO: Background rectangles are being deactivated when justifying text.
            }
        }

        /// <summary>
        ///     Applies centered alignment to the text on the linebox
        /// </summary>
        /// <param name="g"></param>
        /// <param name="line"></param>
        private static void ApplyCenterAlignment(Graphics g, LineBox line)
        {
            if (line.Words.Count == 0) return;

            var lastWord = line.Words[line.Words.Count - 1];
            var right = line.OwnerBox.ActualRight - line.OwnerBox.ActualPaddingRight -
                        line.OwnerBox.ActualBorderRightWidth;
            var diff = right - lastWord.Right - lastWord.LastMeasureOffset.X - lastWord.OwnerBox.ActualBorderRightWidth -
                       lastWord.OwnerBox.ActualPaddingRight;
            diff /= 2;

            if (diff <= 0) return;

            foreach (var word in line.Words)
            {
                word.Left += diff;
            }

            foreach (var b in line.Rectangles.Keys)
            {
                var r = b.Rectangles[line];
                b.Rectangles[line] = new RectangleF(r.X + diff, r.Y, r.Width, r.Height);
            }
        }

        /// <summary>
        ///     Applies right alignment to the text on the linebox
        /// </summary>
        /// <param name="g"></param>
        private static void ApplyRightAlignment(Graphics g, LineBox line)
        {
            if (line.Words.Count == 0) return;


            var lastWord = line.Words[line.Words.Count - 1];
            var right = line.OwnerBox.ActualRight - line.OwnerBox.ActualPaddingRight -
                        line.OwnerBox.ActualBorderRightWidth;
            var diff = right - lastWord.Right - lastWord.LastMeasureOffset.X - lastWord.OwnerBox.ActualBorderRightWidth -
                       lastWord.OwnerBox.ActualPaddingRight;


            if (diff <= 0) return;

            //if (line.OwnerBox.Direction == CssConstants.Rtl)
            //{

            //}

            foreach (var word in line.Words)
            {
                word.Left += diff;
            }

            foreach (var b in line.Rectangles.Keys)
            {
                var r = b.Rectangles[line];
                b.Rectangles[line] = new RectangleF(r.X + diff, r.Y, r.Width, r.Height);
            }
        }

        /// <summary>
        ///     Simplest alignment, just arrange words.
        /// </summary>
        /// <param name="g"></param>
        /// <param name="line"></param>
        private static void ApplyLeftAlignment(Graphics g, LineBox line)
        {
            //No alignment needed.

            //foreach (LineBoxRectangle r in line.Rectangles)
            //{
            //    float curx = r.Left + (r.Index == 0 ? r.OwnerBox.ActualPaddingLeft + r.OwnerBox.ActualBorderLeftWidth / 2 : 0);

            //    if (r.SpaceBefore) curx += r.OwnerBox.ActualWordSpacing;

            //    foreach (BoxWord word in r.Words)
            //    {
            //        word.Left = curx;
            //        word.Top = r.Top;// +r.OwnerBox.ActualPaddingTop + r.OwnerBox.ActualBorderTopWidth / 2;

            //        curx = word.Right + r.OwnerBox.ActualWordSpacing;
            //    }
            //}
        }

        #endregion
    }
}