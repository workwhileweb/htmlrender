using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using W0.Ui.HtmlRender.Css;

namespace W0.Ui.HtmlRender
{
    public class Tag
    {
        #region Fields

        #endregion

        #region Ctor

        private Tag()
        {
            Attributes = new Dictionary<string, string>();
        }

        public Tag(string tag)
            : this()
        {
            tag = tag.Substring(1, tag.Length - 2);

            var spaceIndex = tag.IndexOf(" ", StringComparison.Ordinal);

            //Extract tag name
            if (spaceIndex < 0)
            {
                TagName = tag;
            }
            else
            {
                TagName = tag.Substring(0, spaceIndex);
            }

            //Check if is end tag
            if (TagName.StartsWith("/"))
            {
                IsClosing = true;
                TagName = TagName.Substring(1);
            }

            TagName = TagName.ToLower();

            //Extract attributes
            var atts = Parser.Match(Parser.HmlTagAttributes, tag);

            foreach (Match att in atts)
            {
                //Extract attribute and value
                var chunks = att.Value.Split('=');

                if (chunks.Length == 1)
                {
                    if (!Attributes.ContainsKey(chunks[0]))
                        Attributes.Add(chunks[0].ToLower(), string.Empty);
                }
                else if (chunks.Length == 2)
                {
                    var attname = chunks[0].Trim();
                    var attvalue = chunks[1].Trim();

                    if (attvalue.StartsWith("\"") && attvalue.EndsWith("\"") && attvalue.Length > 2)
                    {
                        attvalue = attvalue.Substring(1, attvalue.Length - 2);
                    }

                    if (!Attributes.ContainsKey(attname))
                        Attributes.Add(attname, attvalue);
                }
            }
        }

        #endregion

        #region Props

        /// <summary>
        ///     Gets the dictionary of attributes in the tag
        /// </summary>
        public Dictionary<string, string> Attributes { get; }


        /// <summary>
        ///     Gets the name of this tag
        /// </summary>
        public string TagName { get; }

        /// <summary>
        ///     Gets if the tag is actually a closing tag
        /// </summary>
        public bool IsClosing { get; }

        /// <summary>
        ///     Gets if the tag is single placed; in other words it doesn't need a closing tag;
        ///     e.g. &lt;br&gt;
        /// </summary>
        public bool IsSingle
        {
            get
            {
                return TagName.StartsWith("!")
                       || (new List<string>(
                           new[]
                           {
                               "area", "base", "basefont", "br", "col",
                               "frame", "hr", "img", "input", "isindex",
                               "link", "meta", "param"
                           }
                           )).Contains(TagName)
                    ;
            }
        }

        internal void TranslateAttributes(Box box)
        {
            var t = TagName.ToUpper();

            foreach (var att in Attributes.Keys)
            {
                var value = Attributes[att];

                switch (att)
                {
                    case Constants.align:
                        if (value == Constants.left || value == Constants.center || value == Constants.right ||
                            value == Constants.justify)
                            box.TextAlign = value;
                        else
                            box.VerticalAlign = value;
                        break;
                    case Constants.background:
                        box.BackgroundImage = value;
                        break;
                    case Constants.bgcolor:
                        box.BackgroundColor = value;
                        break;
                    case Constants.border:
                        box.BorderWidth = TranslateLength(value);

                        if (t == Constants.TABLE)
                        {
                            ApplyTableBorder(box, value);
                        }
                        else
                        {
                            box.BorderStyle = Css.Constants.Solid;
                        }
                        break;
                    case Constants.bordercolor:
                        box.BorderColor = value;
                        break;
                    case Constants.cellspacing:
                        box.BorderSpacing = TranslateLength(value);
                        break;
                    case Constants.cellpadding:
                        ApplyTablePadding(box, value);
                        break;
                    case Constants.color:
                        box.Color = value;
                        break;
                    case Constants.dir:
                        box.Direction = value;
                        break;
                    case Constants.face:
                        box.FontFamily = value;
                        break;
                    case Constants.height:
                        box.Height = TranslateLength(value);
                        break;
                    case Constants.hspace:
                        box.MarginRight = box.MarginLeft = TranslateLength(value);
                        break;
                    case Constants.nowrap:
                        box.WhiteSpace = Css.Constants.Nowrap;
                        break;
                    case Constants.size:
                        if (t == Constants.HR)
                            box.Height = TranslateLength(value);
                        break;
                    case Constants.valign:
                        box.VerticalAlign = value;
                        break;
                    case Constants.vspace:
                        box.MarginTop = box.MarginBottom = TranslateLength(value);
                        break;
                    case Constants.width:
                        box.Width = TranslateLength(value);
                        break;
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Converts an HTML length into a Css length
        /// </summary>
        /// <param name="htmlLength"></param>
        /// <returns></returns>
        private string TranslateLength(string htmlLength)
        {
            var len = new Length(htmlLength);

            if (len.HasError)
            {
                return htmlLength + "px";
            }

            return htmlLength;
        }

        /// <summary>
        ///     Cascades to the TD's the border spacified in the TABLE tag.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="border"></param>
        private void ApplyTableBorder(Box table, string border)
        {
            foreach (var box in table.Boxes)
            {
                foreach (var cell in box.Boxes)
                {
                    cell.BorderWidth = TranslateLength(border);
                }
            }
        }

        /// <summary>
        ///     Cascades to the TD's the border spacified in the TABLE tag.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="border"></param>
        private void ApplyTablePadding(Box table, string padding)
        {
            foreach (var box in table.Boxes)
            {
                foreach (var cell in box.Boxes)
                {
                    cell.Padding = TranslateLength(padding);
                }
            }
        }

        /// <summary>
        ///     Gets a boolean indicating if the attribute list has the specified attribute
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public bool HasAttribute(string attribute)
        {
            return Attributes.ContainsKey(attribute);
        }

        public override string ToString()
        {
            return string.Format("<{1}{0}>", TagName, IsClosing ? "/" : string.Empty);
        }

        #endregion
    }
}