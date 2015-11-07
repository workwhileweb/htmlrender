using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;

namespace W0.Ui.HtmlRender.Css
{
    /// <summary>
    ///     Represents a CSS Box of text or replaced elements.
    /// </summary>
    /// <remarks>
    ///     The Box can contains other boxes, that's the way that the CSS Tree
    ///     is composed.
    ///     To know more about boxes visit CSS spec:
    ///     http://www.w3.org/TR/CSS21/box.html
    /// </remarks>
    public class Box
    {
        #region Static

        /// <summary>
        ///     An empty box with empty values.
        /// </summary>
        internal static readonly Box Empty;

        /// <summary>
        ///     Table of 'css-property' => .NET property
        /// </summary>
        internal static Dictionary<string, PropertyInfo> _properties;

        /// <summary>
        ///     Dictionary of default values
        /// </summary>
        private static readonly Dictionary<string, string> _defaults;

        /// <summary>
        ///     Hosts all inhertiable properties
        /// </summary>
        private static readonly List<PropertyInfo> _inheritables;

        /// <summary>
        ///     Hosts css properties
        /// </summary>
        private static readonly List<PropertyInfo> _cssproperties;

        /// <summary>
        ///     Static constructor and initialization
        /// </summary>
        static Box()
        {
            #region Initialize _properties, _inheritables and _defaults Dictionaries

            _properties = new Dictionary<string, PropertyInfo>();
            _defaults = new Dictionary<string, string>();
            _inheritables = new List<PropertyInfo>();
            _cssproperties = new List<PropertyInfo>();

            var props = typeof (Box).GetProperties();
            for (var i = 0; i < props.Length; i++)
            {
                var att = Attribute.GetCustomAttribute(props[i], typeof (PropertyAttribute)) as PropertyAttribute;

                if (att != null)
                {
                    _properties.Add(att.Name, props[i]);
                    _defaults.Add(att.Name, GetDefaultValue(props[i]));
                    _cssproperties.Add(props[i]);

                    var inh =
                        Attribute.GetCustomAttribute(props[i], typeof (PropertyInheritedAttribute)) as
                            PropertyInheritedAttribute;

                    if (inh != null)
                    {
                        _inheritables.Add(props[i]);
                    }
                }
            }

            #endregion

            Empty = new Box();
        }

        /// <summary>
        ///     Gets the default value of the specified css property
        /// </summary>
        /// <param name="prop"></param>
        /// <returns></returns>
        private static string GetDefaultValue(PropertyInfo prop)
        {
            var att = Attribute.GetCustomAttribute(prop, typeof (DefaultValueAttribute)) as DefaultValueAttribute;

            if (att == null) return string.Empty;

            var s = Convert.ToString(att.Value);

            return string.IsNullOrEmpty(s) ? string.Empty : s;
        }

        #endregion

        #region CSS Fields

        private string _borderWidth;
        private string _borderColor;
        private string _borderStyle;
        private string _borderBottom;
        private string _borderLeft;
        private string _borderRight;
        private string _borderTop;
        private string _border;
        //private string _bottom;
        private string _color;
        private string _cornerRadius;
        private string _font;
        private string _fontFamily;
        private string _fontSize;
        private string _margin;
        private string _lineHeight;
        private string _paddingLeft;
        private string _paddingBottom;
        private string _paddingRight;
        private string _paddingTop;
        private string _padding;
        //private string _right;
        private string _text;
        private string _textIndent;
        private string _wordSpacing;

        #endregion

        #region Fields

        /// <summary>
        ///     Do not use or alter this flag
        /// </summary>
        /// <remarks>
        ///     Flag that indicates that CssTable algorithm already made fixes on it.
        /// </remarks>
        internal bool TableFixed;

        private Box _parentBox;
        private bool _wordsSizeMeasured;
        private float _fontAscent = float.NaN;
        private float _fontDescent = float.NaN;
        private float _fontLineSpacing = float.NaN;
        protected InitialContainer _initialContainer;

        #endregion

        #region Ctor

        protected Box()
        {
            Words = new List<BoxWord>();
            Boxes = new List<Box>();
            LineBoxes = new List<LineBox>();
            ParentLineBoxes = new List<LineBox>();
            Rectangles = new Dictionary<LineBox, RectangleF>();

            #region Initialize properties with default values

            foreach (var prop in _properties.Keys)
            {
                _properties[prop].SetValue(this, _defaults[prop], null);
            }

            #endregion
        }

        public Box(Box parentBox)
            : this()
        {
            ParentBox = parentBox;
        }

        internal Box(Box parentBox, Tag tag)
            : this(parentBox)
        {
            HtmlTag = tag;
        }

        #endregion

        #region CSS Properties

        #region Visual Formatting

        #region Border

        #region Border Width

        [Property("border-bottom-width")]
        [DefaultValue("medium")]
        public string BorderBottomWidth { get; set; }

        [Property("border-left-width")]
        [DefaultValue("medium")]
        public string BorderLeftWidth { get; set; }

        [Property("border-right-width")]
        [DefaultValue("medium")]
        public string BorderRightWidth { get; set; }

        [Property("border-top-width")]
        [DefaultValue("medium")]
        public string BorderTopWidth { get; set; }

        [Property("border-width")]
        [DefaultValue("")]
        public string BorderWidth
        {
            get { return _borderWidth; }
            set
            {
                _borderWidth = value;

                var values = Value.SplitValues(value);

                switch (values.Length)
                {
                    case 1:
                        BorderTopWidth = BorderLeftWidth = BorderRightWidth = BorderBottomWidth = values[0];
                        break;
                    case 2:
                        BorderTopWidth = BorderBottomWidth = values[0];
                        BorderLeftWidth = BorderRightWidth = values[1];
                        break;
                    case 3:
                        BorderTopWidth = values[0];
                        BorderLeftWidth = BorderRightWidth = values[1];
                        BorderBottomWidth = values[2];
                        break;
                    case 4:
                        BorderTopWidth = values[0];
                        BorderRightWidth = values[1];
                        BorderBottomWidth = values[2];
                        BorderLeftWidth = values[3];
                        break;
                    default:
                        break;
                }
            }
        }

        #endregion

        #region Border Style

        [Property("border-bottom-style")]
        [DefaultValue("none")]
        public string BorderBottomStyle { get; set; }


        [Property("border-left-style")]
        [DefaultValue("none")]
        public string BorderLeftStyle { get; set; }

        [Property("border-right-style")]
        [DefaultValue("none")]
        public string BorderRightStyle { get; set; }

        [Property("border-style")]
        [DefaultValue("")]
        public string BorderStyle
        {
            get { return _borderStyle; }
            set
            {
                _borderStyle = value;

                var values = Value.SplitValues(value);

                switch (values.Length)
                {
                    case 1:
                        BorderTopStyle = BorderLeftStyle = BorderRightStyle = BorderBottomStyle = values[0];
                        break;
                    case 2:
                        BorderTopStyle = BorderBottomStyle = values[0];
                        BorderLeftStyle = BorderRightStyle = values[1];
                        break;
                    case 3:
                        BorderTopStyle = values[0];
                        BorderLeftStyle = BorderRightStyle = values[1];
                        BorderBottomStyle = values[2];
                        break;
                    case 4:
                        BorderTopStyle = values[0];
                        BorderRightStyle = values[1];
                        BorderBottomStyle = values[2];
                        BorderLeftStyle = values[3];
                        break;
                    default:
                        break;
                }
            }
        }

        [Property("border-top-style")]
        [DefaultValue("none")]
        public string BorderTopStyle { get; set; }

        #endregion

        #region Border Color

        [Property("border-color")]
        [DefaultValue("black")]
        public string BorderColor
        {
            get { return _borderColor; }
            set
            {
                _borderColor = value;

                var colors = Parser.Match(Parser.CssColors, value);

                var values = new string[colors.Count];

                for (var i = 0; i < values.Length; i++)
                    values[i] = colors[i].Value;

                switch (values.Length)
                {
                    case 1:
                        BorderTopColor = BorderLeftColor = BorderRightColor = BorderBottomColor = values[0];
                        break;
                    case 2:
                        BorderTopColor = BorderBottomColor = values[0];
                        BorderLeftColor = BorderRightColor = values[1];
                        break;
                    case 3:
                        BorderTopColor = values[0];
                        BorderLeftColor = BorderRightColor = values[1];
                        BorderBottomColor = values[2];
                        break;
                    case 4:
                        BorderTopColor = values[0];
                        BorderRightColor = values[1];
                        BorderBottomColor = values[2];
                        BorderLeftColor = values[3];
                        break;
                    default:
                        break;
                }
            }
        }

        [Property("border-bottom-color")]
        [DefaultValue("black")]
        public string BorderBottomColor { get; set; }

        [Property("border-left-color")]
        [DefaultValue("black")]
        public string BorderLeftColor { get; set; }

        [Property("border-right-color")]
        [DefaultValue("black")]
        public string BorderRightColor { get; set; }

        [Property("border-top-color")]
        [DefaultValue("black")]
        public string BorderTopColor { get; set; }

        #endregion

        #region Border ShortHands

        [Property("border")]
        [DefaultValue("")]
        public string Border
        {
            get { return _border; }
            set
            {
                _border = value;

                var borderWidth = Parser.Search(Parser.CssBorderWidth, value);
                var borderStyle = Parser.Search(Parser.CssBorderStyle, value);
                var borderColor = Parser.Search(Parser.CssColors, value);

                if (borderWidth != null) BorderWidth = borderWidth;
                if (borderStyle != null) BorderStyle = borderStyle;
                if (borderColor != null) BorderColor = borderColor;
            }
        }

        [Property("border-bottom")]
        [DefaultValue("")]
        public string BorderBottom
        {
            get { return _borderBottom; }
            set
            {
                _borderBottom = value;

                var borderWidth = Parser.Search(Parser.CssBorderWidth, value);
                var borderStyle = Parser.Search(Parser.CssBorderStyle, value);
                var borderColor = Parser.Search(Parser.CssColors, value);

                if (borderWidth != null) BorderBottomWidth = borderWidth;
                if (borderStyle != null) BorderBottomStyle = borderStyle;
                if (borderColor != null) BorderBottomColor = borderColor;
            }
        }

        [Property("border-left")]
        [DefaultValue("")]
        public string BorderLeft
        {
            get { return _borderLeft; }
            set
            {
                _borderLeft = value;

                var borderWidth = Parser.Search(Parser.CssBorderWidth, value);
                var borderStyle = Parser.Search(Parser.CssBorderStyle, value);
                var borderColor = Parser.Search(Parser.CssColors, value);

                if (borderWidth != null) BorderLeftWidth = borderWidth;
                if (borderStyle != null) BorderLeftStyle = borderStyle;
                if (borderColor != null) BorderLeftColor = borderColor;
            }
        }

        [Property("border-right")]
        [DefaultValue("")]
        public string BorderRight
        {
            get { return _borderRight; }
            set
            {
                _borderRight = value;

                var borderWidth = Parser.Search(Parser.CssBorderWidth, value);
                var borderStyle = Parser.Search(Parser.CssBorderStyle, value);
                var borderColor = Parser.Search(Parser.CssColors, value);

                if (borderWidth != null) BorderRightWidth = borderWidth;
                if (borderStyle != null) BorderRightStyle = borderStyle;
                if (borderColor != null) BorderRightColor = borderColor;
            }
        }

        [Property("border-top")]
        [DefaultValue("")]
        public string BorderTop
        {
            get { return _borderTop; }
            set
            {
                _borderTop = value;

                var borderWidth = Parser.Search(Parser.CssBorderWidth, value);
                var borderStyle = Parser.Search(Parser.CssBorderStyle, value);
                var borderColor = Parser.Search(Parser.CssColors, value);

                if (borderWidth != null) BorderTopWidth = borderWidth;
                if (borderStyle != null) BorderTopStyle = borderStyle;
                if (borderColor != null) BorderTopColor = borderColor;
            }
        }

        #endregion

        #region Table borders

        [Property("border-spacing")]
        [DefaultValue("0")]
        [PropertyInherited]
        public string BorderSpacing { get; set; }

        [Property("border-collapse")]
        [DefaultValue("separate")]
        [PropertyInherited]
        public string BorderCollapse { get; set; }

        #endregion

        #region Rounded corners

        [Property("corner-radius")]
        [DefaultValue("0")]
        public string CornerRadius
        {
            get { return _cornerRadius; }
            set
            {
                var r = Parser.Match(Parser.CssLength, value);

                switch (r.Count)
                {
                    case 1:
                        CornerNERadius = r[0].Value;
                        CornerNWRadius = r[0].Value;
                        CornerSERadius = r[0].Value;
                        CornerSWRadius = r[0].Value;
                        break;
                    case 2:
                        CornerNERadius = r[0].Value;
                        CornerNWRadius = r[0].Value;
                        CornerSERadius = r[1].Value;
                        CornerSWRadius = r[1].Value;
                        break;
                    case 3:
                        CornerNERadius = r[0].Value;
                        CornerNWRadius = r[1].Value;
                        CornerSERadius = r[2].Value;
                        break;
                    case 4:
                        CornerNERadius = r[0].Value;
                        CornerNWRadius = r[1].Value;
                        CornerSERadius = r[2].Value;
                        CornerSWRadius = r[3].Value;
                        break;
                }

                _cornerRadius = value;
            }
        }


        [Property("corner-nw-radius")]
        [DefaultValue("0")]
        public string CornerNWRadius { get; set; }

        [Property("corner-ne-radius")]
        [DefaultValue("0")]
        public string CornerNERadius { get; set; }

        [Property("corner-se-radius")]
        [DefaultValue("0")]
        public string CornerSERadius { get; set; }

        [Property("corner-sw-radius")]
        [DefaultValue("0")]
        public string CornerSWRadius { get; set; }

        #endregion

        #endregion

        #region Margin

        [Property("margin")]
        [DefaultValue("")]
        public string Margin
        {
            get { return _margin; }
            set
            {
                _margin = value;
                var values = Value.SplitValues(value);

                switch (values.Length)
                {
                    case 1:
                        MarginTop = MarginLeft = MarginRight = MarginBottom = values[0];
                        break;
                    case 2:
                        MarginTop = MarginBottom = values[0];
                        MarginLeft = MarginRight = values[1];
                        break;
                    case 3:
                        MarginTop = values[0];
                        MarginLeft = MarginRight = values[1];
                        MarginBottom = values[2];
                        break;
                    case 4:
                        MarginTop = values[0];
                        MarginRight = values[1];
                        MarginBottom = values[2];
                        MarginLeft = values[3];
                        break;
                    default:
                        break;
                }
            }
        }

        [Property("margin-bottom")]
        [DefaultValue("0")]
        public string MarginBottom { get; set; }

        [Property("margin-left")]
        [DefaultValue("0")]
        public string MarginLeft { get; set; }

        [Property("margin-right")]
        [DefaultValue("0")]
        public string MarginRight { get; set; }

        [Property("margin-top")]
        [DefaultValue("0")]
        public string MarginTop { get; set; }

        #endregion

        #region Padding

        [Property("padding")]
        [DefaultValue("")]
        public string Padding
        {
            get { return _padding; }
            set
            {
                _padding = value;

                var values = Value.SplitValues(value);

                switch (values.Length)
                {
                    case 1:
                        PaddingTop = PaddingLeft = PaddingRight = PaddingBottom = values[0];
                        break;
                    case 2:
                        PaddingTop = PaddingBottom = values[0];
                        PaddingLeft = PaddingRight = values[1];
                        break;
                    case 3:
                        PaddingTop = values[0];
                        PaddingLeft = PaddingRight = values[1];
                        PaddingBottom = values[2];
                        break;
                    case 4:
                        PaddingTop = values[0];
                        PaddingRight = values[1];
                        PaddingBottom = values[2];
                        PaddingLeft = values[3];
                        break;
                    default:
                        break;
                }
            }
        }

        [Property("padding-bottom")]
        [DefaultValue("0")]
        public string PaddingBottom
        {
            get { return _paddingBottom; }
            set
            {
                _paddingBottom = value;
                _actualPaddingBottom = float.NaN;
            }
        }

        [Property("padding-left")]
        [DefaultValue("0")]
        public string PaddingLeft
        {
            get { return _paddingLeft; }
            set
            {
                _paddingLeft = value;
                _actualPaddingLeft = float.NaN;
            }
        }

        [Property("padding-right")]
        [DefaultValue("0")]
        public string PaddingRight
        {
            get { return _paddingRight; }
            set
            {
                _paddingRight = value;
                _actualPaddingRight = float.NaN;
            }
        }

        [Property("padding-top")]
        [DefaultValue("0")]
        public string PaddingTop
        {
            get { return _paddingTop; }
            set
            {
                _paddingTop = value;
                _actualPaddingTop = float.NaN;
            }
        }

        #endregion

        #region Bounds

        [Property("left")]
        [DefaultValue("auto")]
        public string Left { get; set; }

        [Property("top")]
        [DefaultValue("auto")]
        public string Top { get; set; }

        //[CssProperty("right")]
        //[DefaultValue("auto")]
        //public string Right
        //{
        //    get { return _right; }
        //    set { _right = value; }
        //}

        //[CssProperty("bottom")]
        //[DefaultValue("auto")]
        //public string Bottom
        //{
        //    get { return _bottom; }
        //    set { _bottom = value; }
        //}

        [Property("width")]
        [DefaultValue("auto")]
        public string Width { get; set; }

        [Property("height")]
        [DefaultValue("auto")]
        public string Height { get; set; }

        #endregion

        #endregion

        #region Colors and Backgrounds

        [Property("background-color")]
        [DefaultValue("transparent")]
        public string BackgroundColor { get; set; }

        [Property("background-image")]
        [DefaultValue("none")]
        public string BackgroundImage { get; set; }

        [Property("background-repeat")]
        [DefaultValue("repeat")]
        public string BackgroundRepeat { get; set; }

        [Property("background-gradient")]
        [DefaultValue("none")]
        public string BackgroundGradient { get; set; }

        [Property("background-gradient-angle")]
        [DefaultValue("90")]
        public string BackgroundGradientAngle { get; set; }

        [Property("color")]
        [DefaultValue("black")]
        [PropertyInherited]
        public string Color
        {
            get { return _color; }
            set
            {
                _color = value;
                _actualColor = System.Drawing.Color.Empty;
            }
        }

        [Property("display")]
        [DefaultValue("inline")]
        public string Display { get; set; }

        [Property("direction")]
        [DefaultValue("ltr")]
        public string Direction { get; set; }


        [Property("empty-cells")]
        [DefaultValue("show")]
        [PropertyInherited]
        public string EmptyCells { get; set; }


        [Property("float")]
        [DefaultValue("none")]
        public string Float { get; set; }

        [Property("position")]
        [DefaultValue("static")]
        public string Position { get; set; }

        #endregion

        #region Text

        [Property("line-height")]
        [DefaultValue("normal")]
        public string LineHeight
        {
            get { return _lineHeight; }
            set { _lineHeight = NoEms(value); }
        }

        [Property("vertical-align")]
        [DefaultValue("baseline")]
        [PropertyInherited]
        public string VerticalAlign { get; set; }

        [Property("text-indent")]
        [DefaultValue("0")]
        [PropertyInherited]
        public string TextIndent
        {
            get { return _textIndent; }
            set { _textIndent = NoEms(value); }
        }

        [Property("text-align")]
        [DefaultValue("")]
        [PropertyInherited]
        public string TextAlign { get; set; }

        [Property("text-decoration")]
        [DefaultValue("")]
        public string TextDecoration { get; set; }

        [Property("white-space")]
        [DefaultValue("normal")]
        [PropertyInherited]
        public string WhiteSpace { get; set; }


        [Property("word-spacing")]
        [DefaultValue("normal")]
        public string WordSpacing
        {
            get { return _wordSpacing; }
            set { _wordSpacing = NoEms(value); }
        }

        #endregion

        #region Font

        [Property("font")]
        [DefaultValue("")]
        [PropertyInherited]
        public string Font
        {
            get { return _font; }
            set
            {
                _font = value;

                int mustBePos;
                var mustBe = Parser.Search(Parser.CssFontSizeAndLineHeight, value, out mustBePos);

                if (!string.IsNullOrEmpty(mustBe))
                {
                    mustBe = mustBe.Trim();
                    //Check for style||variant||weight on the left
                    var leftSide = value.Substring(0, mustBePos);
                    var fontStyle = Parser.Search(Parser.CssFontStyle, leftSide);
                    var fontVariant = Parser.Search(Parser.CssFontVariant, leftSide);
                    var fontWeight = Parser.Search(Parser.CssFontWeight, leftSide);

                    //Check for family on the right
                    var rightSide = value.Substring(mustBePos + mustBe.Length);
                    var fontFamily = rightSide.Trim();
                    //Parser.Search(Parser.CssFontFamily, rightSide); //TODO: Would this be right?

                    //Check for font-size and line-height
                    var fontSize = mustBe;
                    var lineHeight = string.Empty;

                    if (mustBe.Contains("/") && mustBe.Length > mustBe.IndexOf("/", StringComparison.Ordinal) + 1)
                    {
                        var slashPos = mustBe.IndexOf("/", StringComparison.Ordinal);
                        fontSize = mustBe.Substring(0, slashPos);
                        lineHeight = mustBe.Substring(slashPos + 1);
                    }

                    //Assign values p { font: 12px/14px sans-serif }
                    if (!string.IsNullOrEmpty(fontStyle)) FontStyle = fontStyle;
                    if (!string.IsNullOrEmpty(fontVariant)) FontVariant = fontVariant;
                    if (!string.IsNullOrEmpty(fontWeight)) FontWeight = fontWeight;
                    if (!string.IsNullOrEmpty(fontFamily)) FontFamily = fontFamily;
                    if (!string.IsNullOrEmpty(fontSize)) FontSize = fontSize;
                    if (!string.IsNullOrEmpty(lineHeight)) LineHeight = lineHeight;
                }
            }
        }

        [Property("font-family")]
        [DefaultValue("serif")]
        [PropertyInherited]
        public string FontFamily
        {
            get { return _fontFamily; }
            set
            {
                //HACK: Because of performance, generic font families
                //      will be checked when only the generic font 
                //      family is given.

                switch (value)
                {
                    case Constants.Serif:
                        _fontFamily = Defaults.FontSerif;
                        break;
                    case Constants.SansSerif:
                        _fontFamily = Defaults.FontSansSerif;
                        break;
                    case Constants.Cursive:
                        _fontFamily = Defaults.FontCursive;
                        break;
                    case Constants.Fantasy:
                        _fontFamily = Defaults.FontFantasy;
                        break;
                    case Constants.Monospace:
                        _fontFamily = Defaults.FontMonospace;
                        break;
                    default:
                        _fontFamily = value;
                        break;
                }
            }
        }

        [Property("font-size")]
        [DefaultValue("medium")]
        [PropertyInherited]
        public string FontSize
        {
            get { return _fontSize; }
            set
            {
                var length = Parser.Search(Parser.CssLength, value);

                if (length != null)
                {
                    var computedValue = string.Empty;
                    var len = new Length(length);

                    if (len.HasError)
                    {
                        computedValue = _defaults["font-size"];
                    }
                    else if (len.Unit == Length.CssUnit.Ems && ParentBox != null)
                    {
                        computedValue = len.ConvertEmToPoints(ParentBox.ActualFont.SizeInPoints).ToString();
                    }
                    else
                    {
                        computedValue = len.ToString();
                    }

                    _fontSize = computedValue;
                }
                else
                {
                    _fontSize = value;
                }
            }
        }

        [Property("font-style")]
        [DefaultValue("normal")]
        [PropertyInherited]
        public string FontStyle { get; set; }

        [Property("font-variant")]
        [DefaultValue("normal")]
        [PropertyInherited]
        public string FontVariant { get; set; }


        [Property("font-weight")]
        [DefaultValue("normal")]
        [PropertyInherited]
        public string FontWeight { get; set; }

        #endregion

        #region Lists

        [Property("list-style")]
        [DefaultValue("")]
        [PropertyInherited]
        public string ListStyle { get; set; }

        [Property("list-style-position")]
        [DefaultValue("outside")]
        [PropertyInherited]
        public string ListStylePosition { get; set; }

        [Property("list-style-image")]
        [DefaultValue("")]
        [PropertyInherited]
        public string ListStyleImage { get; set; }

        [Property("list-style-type")]
        [DefaultValue("disc")]
        [PropertyInherited]
        public string ListStyleType { get; set; }

        #endregion

        #endregion

        #region Actual Values Properties

        #region Fields

        private float _actualCornerNW = float.NaN;
        private float _actualCornerNE = float.NaN;
        private float _actualCornerSW = float.NaN;
        private float _actualCornerSE = float.NaN;
        private Color _actualColor = System.Drawing.Color.Empty;
        private float _actualBackgroundGradientAngle = float.NaN;
        private float _actualPaddingTop = float.NaN;
        private float _actualPaddingBottom = float.NaN;
        private float _actualPaddingRight = float.NaN;
        private float _actualPaddingLeft = float.NaN;
        private float _actualMarginTop = float.NaN;
        private float _actualMarginBottom = float.NaN;
        private float _actualMarginRight = float.NaN;
        private float _actualMarginLeft = float.NaN;
        private float _actualBorderTopWidth = float.NaN;
        private float _actualBorderLeftWidth = float.NaN;
        private float _actualBorderBottomWidth = float.NaN;
        private float _actualBorderRightWidth = float.NaN;
        private Color _actualBackgroundGradient = System.Drawing.Color.Empty;
        private Color _actualBorderTopColor = System.Drawing.Color.Empty;
        private Color _actualBorderLeftColor = System.Drawing.Color.Empty;
        private Color _actualBorderBottomColor = System.Drawing.Color.Empty;
        private Color _actualBorderRightColor = System.Drawing.Color.Empty;
        private float _actualWordSpacing = float.NaN;
        private Color _actualBackgroundColor = System.Drawing.Color.Empty;
        private Font _actualFont;
        private float _actualTextIndent = float.NaN;
        private float _actualBorderSpacingHorizontal = float.NaN;
        private float _actualBorderSpacingVertical = float.NaN;

        #endregion

        #region Boxing

        #region Padding

        /// <summary>
        ///     Gets the actual top's padding
        /// </summary>
        public float ActualPaddingTop
        {
            get
            {
                if (float.IsNaN(_actualPaddingTop))
                {
                    _actualPaddingTop = Value.ParseLength(PaddingTop, Size.Width, this);
                }

                return _actualPaddingTop;
            }
        }

        /// <summary>
        ///     Gets the actual padding on the left
        /// </summary>
        public float ActualPaddingLeft
        {
            get
            {
                if (float.IsNaN(_actualPaddingLeft))
                {
                    _actualPaddingLeft = Value.ParseLength(PaddingLeft, Size.Width, this);
                }
                return _actualPaddingLeft;
            }
        }

        /// <summary>
        ///     Gets the actual Padding of the bottom
        /// </summary>
        public float ActualPaddingBottom
        {
            get
            {
                if (float.IsNaN(_actualPaddingBottom))
                {
                    _actualPaddingBottom = Value.ParseLength(PaddingBottom, Size.Width, this);
                }
                return _actualPaddingBottom;
            }
        }

        /// <summary>
        ///     Gets the actual padding on the right
        /// </summary>
        public float ActualPaddingRight
        {
            get
            {
                if (float.IsNaN(_actualPaddingRight))
                {
                    _actualPaddingRight = Value.ParseLength(PaddingRight, Size.Width, this);
                }
                return _actualPaddingRight;
            }
        }

        #endregion

        #region Margin

        /// <summary>
        ///     Gets the actual top's Margin
        /// </summary>
        public float ActualMarginTop
        {
            get
            {
                if (float.IsNaN(_actualMarginTop))
                {
                    if (MarginTop == Constants.Auto) MarginTop = "0";
                    _actualMarginTop = Value.ParseLength(MarginTop, Size.Width, this);
                }

                return _actualMarginTop;
            }
        }

        /// <summary>
        ///     Gets the actual Margin on the left
        /// </summary>
        public float ActualMarginLeft
        {
            get
            {
                if (float.IsNaN(_actualMarginLeft))
                {
                    if (MarginLeft == Constants.Auto) MarginLeft = "0";
                    _actualMarginLeft = Value.ParseLength(MarginLeft, Size.Width, this);
                }
                return _actualMarginLeft;
            }
        }

        /// <summary>
        ///     Gets the actual Margin of the bottom
        /// </summary>
        public float ActualMarginBottom
        {
            get
            {
                if (float.IsNaN(_actualMarginBottom))
                {
                    if (MarginBottom == Constants.Auto) MarginBottom = "0";
                    _actualMarginBottom = Value.ParseLength(MarginBottom, Size.Width, this);
                }
                return _actualMarginBottom;
            }
        }

        /// <summary>
        ///     Gets the actual Margin on the right
        /// </summary>
        public float ActualMarginRight
        {
            get
            {
                if (float.IsNaN(_actualMarginRight))
                {
                    if (MarginRight == Constants.Auto) MarginRight = "0";
                    _actualMarginRight = Value.ParseLength(MarginRight, Size.Width, this);
                }
                return _actualMarginRight;
            }
        }

        #endregion

        #region Border

        #region Border Width

        /// <summary>
        ///     Gets the actual top border width
        /// </summary>
        public float ActualBorderTopWidth
        {
            get
            {
                if (float.IsNaN(_actualBorderTopWidth))
                {
                    _actualBorderTopWidth = Value.GetActualBorderWidth(BorderTopWidth, this);

                    if (string.IsNullOrEmpty(BorderTopStyle) || BorderTopStyle == Constants.None)
                    {
                        _actualBorderTopWidth = 0f;
                    }
                }

                return _actualBorderTopWidth;
            }
        }


        /// <summary>
        ///     Gets the actual Left border width
        /// </summary>
        public float ActualBorderLeftWidth
        {
            get
            {
                if (float.IsNaN(_actualBorderLeftWidth))
                {
                    _actualBorderLeftWidth = Value.GetActualBorderWidth(BorderLeftWidth, this);

                    if (string.IsNullOrEmpty(BorderLeftStyle) || BorderLeftStyle == Constants.None)
                    {
                        _actualBorderLeftWidth = 0f;
                    }
                }

                return _actualBorderLeftWidth;
            }
        }


        /// <summary>
        ///     Gets the actual Bottom border width
        /// </summary>
        public float ActualBorderBottomWidth
        {
            get
            {
                if (float.IsNaN(_actualBorderBottomWidth))
                {
                    _actualBorderBottomWidth = Value.GetActualBorderWidth(BorderBottomWidth, this);

                    if (string.IsNullOrEmpty(BorderBottomStyle) || BorderBottomStyle == Constants.None)
                    {
                        _actualBorderBottomWidth = 0f;
                    }
                }

                return _actualBorderBottomWidth;
            }
        }


        /// <summary>
        ///     Gets the actual Right border width
        /// </summary>
        public float ActualBorderRightWidth
        {
            get
            {
                if (float.IsNaN(_actualBorderRightWidth))
                {
                    _actualBorderRightWidth = Value.GetActualBorderWidth(BorderRightWidth, this);

                    if (string.IsNullOrEmpty(BorderRightStyle) || BorderRightStyle == Constants.None)
                    {
                        _actualBorderRightWidth = 0f;
                    }
                }

                return _actualBorderRightWidth;
            }
        }

        #endregion

        #region Border Color

        /// <summary>
        ///     Gets the actual top border Color
        /// </summary>
        public Color ActualBorderTopColor
        {
            get
            {
                if ((_actualBorderTopColor.IsEmpty))
                {
                    _actualBorderTopColor = Value.GetActualColor(BorderTopColor);
                }

                return _actualBorderTopColor;
            }
        }


        /// <summary>
        ///     Gets the actual Left border Color
        /// </summary>
        public Color ActualBorderLeftColor
        {
            get
            {
                if ((_actualBorderLeftColor.IsEmpty))
                {
                    _actualBorderLeftColor = Value.GetActualColor(BorderLeftColor);
                }

                return _actualBorderLeftColor;
            }
        }


        /// <summary>
        ///     Gets the actual Bottom border Color
        /// </summary>
        public Color ActualBorderBottomColor
        {
            get
            {
                if ((_actualBorderBottomColor.IsEmpty))
                {
                    _actualBorderBottomColor = Value.GetActualColor(BorderBottomColor);
                }

                return _actualBorderBottomColor;
            }
        }


        /// <summary>
        ///     Gets the actual Right border Color
        /// </summary>
        public Color ActualBorderRightColor
        {
            get
            {
                if ((_actualBorderRightColor.IsEmpty))
                {
                    _actualBorderRightColor = Value.GetActualColor(BorderRightColor);
                }

                return _actualBorderRightColor;
            }
        }

        #endregion

        #endregion

        #region Corners

        /// <summary>
        ///     Gets the actual lenght of the north west corner
        /// </summary>
        public float ActualCornerNW
        {
            get
            {
                if (float.IsNaN(_actualCornerNW))
                {
                    _actualCornerNW = Value.ParseLength(CornerNWRadius, 0, this);
                }

                return _actualCornerNW;
            }
        }

        /// <summary>
        ///     Gets the actual lenght of the north east corner
        /// </summary>
        public float ActualCornerNE
        {
            get
            {
                if (float.IsNaN(_actualCornerNE))
                {
                    _actualCornerNE = Value.ParseLength(CornerNERadius, 0, this);
                }
                return _actualCornerNE;
            }
        }

        /// <summary>
        ///     Gets the actual lenght of the south east corner
        /// </summary>
        public float ActualCornerSE
        {
            get
            {
                if (float.IsNaN(_actualCornerSE))
                {
                    _actualCornerSE = Value.ParseLength(CornerSERadius, 0, this);
                }

                return _actualCornerSE;
            }
        }

        /// <summary>
        ///     Gets the actual lenght of the south west corner
        /// </summary>
        public float ActualCornerSW
        {
            get
            {
                if (float.IsNaN(_actualCornerSW))
                {
                    _actualCornerSW = Value.ParseLength(CornerSWRadius, 0, this);
                }

                return _actualCornerSW;
            }
        }

        #endregion

        #endregion

        #region Layout Formatting

        /// <summary>
        ///     Gets the actual word spacing of the word.
        /// </summary>
        public float ActualWordSpacing
        {
            get
            {
                if (float.IsNaN(_actualWordSpacing))
                {
                    throw new Exception("Space must be calculated before using this property");
                }
                return _actualWordSpacing;
            }
        }

        #endregion

        #region Colors and Backgrounds

        /// <summary>
        ///     Gets the actual color for the text.
        /// </summary>
        public Color ActualColor
        {
            get
            {
                if (_actualColor.IsEmpty)
                {
                    _actualColor = Value.GetActualColor(Color);
                }

                return _actualColor;
            }
        }

        /// <summary>
        ///     Gets the actual background color of the box
        /// </summary>
        public Color ActualBackgroundColor
        {
            get
            {
                if (_actualBackgroundColor.IsEmpty)
                {
                    _actualBackgroundColor = Value.GetActualColor(BackgroundColor);
                }

                return _actualBackgroundColor;
            }
        }

        /// <summary>
        ///     Gets the second color that creates a gradient for the background
        /// </summary>
        public Color ActualBackgroundGradient
        {
            get
            {
                if (_actualBackgroundGradient.IsEmpty)
                {
                    _actualBackgroundGradient = Value.GetActualColor(BackgroundGradient);
                }
                return _actualBackgroundGradient;
            }
        }


        /// <summary>
        ///     Gets the actual angle specified for the background gradient
        /// </summary>
        public float ActualBackgroundGradientAngle
        {
            get
            {
                if (float.IsNaN(_actualBackgroundGradientAngle))
                {
                    _actualBackgroundGradientAngle = Value.ParseNumber(BackgroundGradientAngle, 360f);
                }

                return _actualBackgroundGradientAngle;
            }
        }

        #endregion

        #region Fonts

        /// <summary>
        ///     Gets the actual font of the parent
        /// </summary>
        public Font ActualParentFont
        {
            get
            {
                if (ParentBox == null)
                {
                    return ActualFont;
                }

                return ParentBox.ActualFont;
            }
        }

        /// <summary>
        ///     Gets the font that should be actually used to paint the text of the box
        /// </summary>
        public Font ActualFont
        {
            get
            {
                if (_actualFont == null)
                {
                    if (string.IsNullOrEmpty(FontFamily))
                    {
                        FontFamily = Defaults.FontSerif;
                    }
                    if (string.IsNullOrEmpty(FontSize))
                    {
                        FontSize = Defaults.FontSize + "pt";
                    }

                    var st = System.Drawing.FontStyle.Regular;

                    if (FontStyle == Constants.Italic || FontStyle == Constants.Oblique)
                    {
                        st |= System.Drawing.FontStyle.Italic;
                    }

                    if (FontWeight != Constants.Normal && FontWeight != Constants.Lighter &&
                        !string.IsNullOrEmpty(FontWeight))
                    {
                        st |= System.Drawing.FontStyle.Bold;
                    }

                    var fsize = 0f;
                    var parentSize = Defaults.FontSize;

                    if (ParentBox != null) parentSize = ParentBox.ActualFont.Size;

                    switch (FontSize)
                    {
                        case Constants.Medium:
                            fsize = Defaults.FontSize;
                            break;
                        case Constants.XXSmall:
                            fsize = Defaults.FontSize - 4;
                            break;
                        case Constants.XSmall:
                            fsize = Defaults.FontSize - 3;
                            break;
                        case Constants.Small:
                            fsize = Defaults.FontSize - 2;
                            break;
                        case Constants.Large:
                            fsize = Defaults.FontSize + 2;
                            break;
                        case Constants.XLarge:
                            fsize = Defaults.FontSize + 3;
                            break;
                        case Constants.XXLarge:
                            fsize = Defaults.FontSize + 4;
                            break;
                        case Constants.Smaller:
                            fsize = parentSize - 2;
                            break;
                        case Constants.Larger:
                            fsize = parentSize + 2;
                            break;
                        default:
                            fsize = Value.ParseLength(FontSize, parentSize, this, parentSize, true);
                            break;
                    }

                    if (fsize <= 1f) fsize = Defaults.FontSize;

                    _actualFont = new Font(FontFamily, fsize, st);
                }
                return _actualFont;
            }
        }

        #endregion

        #region Text

        /// <summary>
        ///     Gets the text indentation (on first line only)
        /// </summary>
        public float ActualTextIndent
        {
            get
            {
                if (float.IsNaN(_actualTextIndent))
                {
                    _actualTextIndent = Value.ParseLength(TextIndent, Size.Width, this);
                }

                return _actualTextIndent;
            }
        }

        #endregion

        #region Tables

        /// <summary>
        ///     Gets the actual horizontal border spacing for tables
        /// </summary>
        public float ActualBorderSpacingHorizontal
        {
            get
            {
                if (float.IsNaN(_actualBorderSpacingHorizontal))
                {
                    var matches = Parser.Match(Parser.CssLength, BorderSpacing);

                    if (matches.Count == 0)
                    {
                        _actualBorderSpacingHorizontal = 0;
                    }
                    else if (matches.Count > 0)
                    {
                        _actualBorderSpacingHorizontal = Value.ParseLength(matches[0].Value, 1, this);
                    }
                }


                return _actualBorderSpacingHorizontal;
            }
        }

        /// <summary>
        ///     Gets the actual vertical border spacing for tables
        /// </summary>
        public float ActualBorderSpacingVertical
        {
            get
            {
                if (float.IsNaN(_actualBorderSpacingVertical))
                {
                    var matches = Parser.Match(Parser.CssLength, BorderSpacing);

                    if (matches.Count == 0)
                    {
                        _actualBorderSpacingVertical = 0;
                    }
                    else if (matches.Count == 1)
                    {
                        _actualBorderSpacingVertical = Value.ParseLength(matches[0].Value, 1, this);
                    }
                    else
                    {
                        _actualBorderSpacingVertical = Value.ParseLength(matches[1].Value, 1, this);
                    }
                }
                return _actualBorderSpacingVertical;
            }
        }

        #endregion

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the • box
        /// </summary>
        public Box ListItemBox { get; private set; }

        /// <summary>
        ///     Gets the width available on the box, counting padding and margin.
        /// </summary>
        public float AvailableWidth
        {
            get
            {
                return Size.Width - ActualBorderLeftWidth - ActualPaddingLeft - ActualPaddingRight -
                       ActualBorderRightWidth;
            }
        }

        /// <summary>
        ///     Gets the bounds of the box
        /// </summary>
        public RectangleF Bounds
        {
            get { return new RectangleF(Location, Size); }
        }

        /// <summary>
        ///     Gets or sets the bottom of the box.
        ///     (When setting, alters only the Size.Height of the box)
        /// </summary>
        public float ActualBottom
        {
            get { return Location.Y + Size.Height; }
            set { Size = new SizeF(Size.Width, value - Location.Y); }
        }

        /// <summary>
        ///     Gets the childrenn boxes of this box
        /// </summary>
        public List<Box> Boxes { get; }

        /// <summary>
        ///     Gets the left of the client rectangle (Where content starts rendering)
        /// </summary>
        public float ClientLeft
        {
            get { return Location.X + ActualBorderLeftWidth + ActualPaddingLeft; }
        }

        /// <summary>
        ///     Gets the top of the client rectangle (Where content starts rendering)
        /// </summary>
        public float ClientTop
        {
            get { return Location.Y + ActualBorderTopWidth + ActualPaddingTop; }
        }

        /// <summary>
        ///     Gets the right of the client rectangle
        /// </summary>
        public float ClientRight
        {
            get { return ActualRight - ActualPaddingRight - ActualBorderRightWidth; }
        }

        /// <summary>
        ///     Gets the bottom of the client rectangle
        /// </summary>
        public float ClientBottom
        {
            get { return ActualBottom - ActualPaddingBottom - ActualBorderBottomWidth; }
        }

        /// <summary>
        ///     Gets the client rectangle
        /// </summary>
        public RectangleF ClientRectangle
        {
            get { return RectangleF.FromLTRB(ClientLeft, ClientTop, ClientRight, ClientBottom); }
        }

        /// <summary>
        ///     Gets the containing block-box of this box. (The nearest parent box with display=block)
        /// </summary>
        public Box ContainingBlock
        {
            get
            {
                if (ParentBox == null)
                {
                    return this; //This is the initial containing block.
                }

                var b = ParentBox;

                while (
                    b.Display != Constants.Block &&
                    b.Display != Constants.Table &&
                    b.Display != Constants.TableCell &&
                    b.ParentBox != null)
                {
                    b = b.ParentBox;
                }

                //Comment this following line to treat always superior box as block
                if (b == null) throw new Exception("There's no containing block on the chain");

                return b;
            }
        }

        /// <summary>
        ///     Gets the font's ascent
        /// </summary>
        public float FontAscent
        {
            get
            {
                if (float.IsNaN(_fontAscent))
                {
                    _fontAscent = LayoutEngine.GetAscent(ActualFont);
                }
                return _fontAscent;
            }
        }

        /// <summary>
        ///     Gets the font's line spacing
        /// </summary>
        public float FontLineSpacing
        {
            get
            {
                if (float.IsNaN(_fontLineSpacing))
                {
                    _fontLineSpacing = LayoutEngine.GetLineSpacing(ActualFont);
                }

                return _fontLineSpacing;
            }
        }

        /// <summary>
        ///     Gets the font's descent
        /// </summary>
        public float FontDescent
        {
            get
            {
                if (float.IsNaN(_fontDescent))
                {
                    _fontDescent = LayoutEngine.GetDescent(ActualFont);
                }
                return _fontDescent;
            }
        }

        /// <summary>
        ///     Gets the first word of the box
        /// </summary>
        internal BoxWord FirstWord
        {
            get { return Words[0]; }
        }

        /// <summary>
        ///     Gets or sets the first linebox where content of this box appear
        /// </summary>
        internal LineBox FirstHostingLineBox { get; set; }

        /// <summary>
        ///     Gets or sets the last linebox where content of this box appear
        /// </summary>
        internal LineBox LastHostingLineBox { get; set; }

        /// <summary>
        ///     Gets the HTMLTag that hosts this box
        /// </summary>
        public Tag HtmlTag { get; }

        /// <summary>
        ///     Gets the InitialContainer of the Box.
        ///     WARNING: May be null.
        /// </summary>
        public InitialContainer InitialContainer
        {
            get { return _initialContainer; }
        }

        /// <summary>
        ///     Gets if this box represents an image
        /// </summary>
        public bool IsImage
        {
            get { return Words.Count == 1 && Words[0].IsImage; }
        }

        /// <summary>
        ///     Gets a value indicating if at least one of the corners of the box is rounded
        /// </summary>
        public bool IsRounded
        {
            get { return ActualCornerNE > 0f || ActualCornerNW > 0f || ActualCornerSE > 0f || ActualCornerSW > 0f; }
        }

        /// <summary>
        ///     Tells if the box is empty or contains just blank spaces
        /// </summary>
        public bool IsSpaceOrEmpty
        {
            get
            {
                if ((Words.Count == 0 && Boxes.Count == 0) ||
                    (Words.Count == 1 && Words[0].IsSpaces) ||
                    Boxes.Count == 1 && Boxes[0] is CssAnonymousSpaceBlockBox) return true;

                foreach (var word in Words)
                {
                    if (!word.IsSpaces)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        ///     Gets the last word of the box
        /// </summary>
        internal BoxWord LastWord
        {
            get { return Words[Words.Count - 1]; }
        }

        /// <summary>
        ///     Gets the line-boxes of this box (if block box)
        /// </summary>
        internal List<LineBox> LineBoxes { get; }

        /// <summary>
        ///     Gets or sets the location of the box
        /// </summary>
        public PointF Location { get; set; }

        /// <summary>
        ///     Gets or sets the parent box of this box
        /// </summary>
        public Box ParentBox
        {
            get { return _parentBox; }
            set
            {
                //Remove from last parent
                if (_parentBox != null && _parentBox.Boxes.Contains(this))
                {
                    _parentBox.Boxes.Remove(this);
                }

                _parentBox = value;

                //Add to new parent
                if (value != null && !value.Boxes.Contains(this))
                {
                    _parentBox.Boxes.Add(this);
                    _initialContainer = value.InitialContainer;
                }
            }
        }

        /// <summary>
        ///     Gets the linebox(es) that contains words of this box (if inline)
        /// </summary>
        internal List<LineBox> ParentLineBoxes { get; }

        /// <summary>
        ///     Gets the rectangles where this box should be painted
        /// </summary>
        internal Dictionary<LineBox, RectangleF> Rectangles { get; }

        /// <summary>
        ///     Gets the right of the box. When setting, it will affect only the width of the box.
        /// </summary>
        public float ActualRight
        {
            get { return Location.X + Size.Width; }
            set { Size = new SizeF(value - Location.X, Size.Height); }
        }

        /// <summary>
        ///     Gets or sets the size of the box
        /// </summary>
        public SizeF Size { get; set; }

        /// <summary>
        ///     Gets or sets the inner text of the box
        /// </summary>
        public string Text
        {
            get { return _text; }
            set
            {
                _text = value;
                UpdateWords();
            }
        }

        /// <summary>
        ///     Gets the BoxWords of text in the box
        /// </summary>
        internal List<BoxWord> Words { get; }

        #endregion

        #region Methods

        /// <summary>
        ///     Sets the initial container of the box
        /// </summary>
        /// <param name="b"></param>
        private void SetInitialContainer(InitialContainer b)
        {
            _initialContainer = b;
        }

        /// <summary>
        ///     Returns false if some of the boxes
        /// </summary>
        /// <returns></returns>
        internal bool ContainsInlinesOnly()
        {
            foreach (var b in Boxes)
            {
                if (b.Display != Constants.Inline)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Gets the index of the box to be used on a (ordered) list
        /// </summary>
        /// <returns></returns>
        private int GetIndexForList()
        {
            var index = 0;

            foreach (var b in ParentBox.Boxes)
            {
                if (b.Display == Constants.ListItem) index++;

                if (b.Equals(this)) return index;
            }

            return index;
        }

        /// <summary>
        ///     Creates the <see cref="ListItemBox" />
        /// </summary>
        /// <param name="g"></param>
        private void CreateListItemBox(Graphics g)
        {
            if (Display == Constants.ListItem)
            {
                if (ListItemBox == null)
                {
                    ListItemBox = new Box();
                    ListItemBox.InheritStyle(this, false);
                    ListItemBox.Display = Constants.Inline;
                    ListItemBox.SetInitialContainer(InitialContainer);

                    if (ParentBox != null && ListStyleType == Constants.Decimal)
                    {
                        ListItemBox.Text = GetIndexForList() + ".";
                    }
                    else
                    {
                        ListItemBox.Text = "•";
                    }

                    ListItemBox.MeasureBounds(g);
                    ListItemBox.Size = new SizeF(ListItemBox.Words[0].Width, ListItemBox.Words[0].Height);
                }
                ListItemBox.Words[0].Left = Location.X - ListItemBox.Size.Width - 5;
                ListItemBox.Words[0].Top = Location.Y + ActualPaddingTop; // +FontAscent;
            }
        }

        /// <summary>
        ///     Searches for the first word occourence inside the box, on the specified linebox
        /// </summary>
        /// <param name="b"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        internal BoxWord FirstWordOccourence(Box b, LineBox line)
        {
            if (b.Words.Count == 0 && b.Boxes.Count == 0)
            {
                return null;
            }

            return b.Words.Count > 0
                ? b.Words.FirstOrDefault(word => line.Words.Contains(word))
                : b.Boxes.Select(bb => FirstWordOccourence(bb, line)).FirstOrDefault(w => w != null);
        }

        /// <summary>
        ///     Gets the specified Attribute, returns string.Empty if no attribute specified
        /// </summary>
        /// <param name="attribute">Attribute to retrieve</param>
        /// <returns>Attribute value or string.Empty if no attribute specified</returns>
        internal string GetAttribute(string attribute)
        {
            return GetAttribute(attribute, string.Empty);
        }

        /// <summary>
        ///     Gets the value of the specified attribute of the source HTML tag.
        /// </summary>
        /// <param name="attribute">Attribute to retrieve</param>
        /// <param name="defaultValue">Value to return if attribute is not specified</param>
        /// <returns>Attribute value or defaultValue if no attribute specified</returns>
        internal string GetAttribute(string attribute, string defaultValue)
        {
            if (HtmlTag == null)
            {
                return defaultValue;
            }

            if (!HtmlTag.HasAttribute(attribute))
            {
                return defaultValue;
            }

            return HtmlTag.Attributes[attribute];
        }

        /// <summary>
        ///     Gets the height of the font in the specified units
        /// </summary>
        /// <returns></returns>
        public float GetEmHeight()
        {
            //float res = Convert.ToSingle(ActualFont.Height);
            //float res = ActualFont.Size * ActualFont.FontFamily.GetCellAscent(f.Style) / ActualFont.FontFamily.GetEmHeight(f.Style);
            var res = ActualFont.GetHeight();
            return res;
        }

        /// <summary>
        ///     Gets the previous sibling of this box.
        /// </summary>
        /// <returns>Box before this one on the tree. Null if its the first</returns>
        private Box GetPreviousSibling(Box b)
        {
            if (b.ParentBox == null)
            {
                return null; //This is initial containing block
            }

            var index = b.ParentBox.Boxes.IndexOf(this);

            if (index < 0) throw new Exception("Box doesn't exist on parent's Box list");

            if (index == 0) return null; //This is the first sibling.


            var diff = 1;
            var sib = b.ParentBox.Boxes[index - diff];

            while ((sib.Display == Constants.None || sib.Position == Constants.Absolute) && index - diff - 1 >= 0)
            {
                sib = b.ParentBox.Boxes[index - ++diff];
            }

            return sib.Display == Constants.None ? null : sib;
        }

        /// <summary>
        ///     Gets the minimum width that the box can be.
        ///     The box can be as thin as the longest word plus padding.
        ///     The check is deep thru box tree.
        /// </summary>
        /// <returns></returns>
        internal float GetMinimumWidth()
        {
            var maxw = 0f;
            var padding = 0f;
            BoxWord word = null;

            GetMinimumWidth_LongestWord(this, ref maxw, ref word);

            if (word != null)
            {
                GetMinimumWidth_BubblePadding(word.OwnerBox, this, ref padding);
            }

            return maxw + padding;
        }

        /// <summary>
        ///     Bubbles up the padding from the starting box
        /// </summary>
        /// <param name="box"></param>
        /// <param name="endbox"></param>
        /// <param name="sum"></param>
        /// <returns></returns>
        private void GetMinimumWidth_BubblePadding(Box box, Box endbox, ref float sum)
        {
            //float padding = box.ActualMarginLeft + box.ActualBorderLeftWidth + box.ActualPaddingLeft +
            //    box.ActualMarginRight + box.ActualBorderRightWidth + box.ActualPaddingRight;

            var padding = box.ActualBorderLeftWidth + box.ActualPaddingLeft +
                          box.ActualBorderRightWidth + box.ActualPaddingRight;

            sum += padding;

            if (!box.Equals(endbox))
            {
                GetMinimumWidth_BubblePadding(box.ParentBox, endbox, ref sum);
            }
        }

        /// <summary>
        ///     Gets the longest word (in width) inside the box, deeply.
        /// </summary>
        /// <param name="b"></param>
        /// <param name="maxw"></param>
        /// <param name="word"></param>
        /// <returns></returns>
        private void GetMinimumWidth_LongestWord(Box b, ref float maxw, ref BoxWord word)
        {
            if (b.Words.Count > 0)
            {
                foreach (var w in b.Words)
                {
                    if (!(w.FullWidth > maxw)) continue;
                    maxw = w.FullWidth;
                    word = w;
                }
            }
            else
            {
                foreach (var bb in b.Boxes)
                    GetMinimumWidth_LongestWord(bb, ref maxw, ref word);
            }
        }

        /// <summary>
        ///     Gets the maximum bottom of the boxes inside the startBox
        /// </summary>
        /// <param name="startBox"></param>
        /// <param name="currentMaxBottom"></param>
        /// <returns></returns>
        internal float GetMaximumBottom(Box startBox, float currentMaxBottom)
        {
            foreach (var line in startBox.Rectangles.Keys)
            {
                currentMaxBottom = Math.Max(currentMaxBottom, startBox.Rectangles[line].Bottom);
            }

            foreach (var b in startBox.Boxes)
            {
                currentMaxBottom = Math.Max(currentMaxBottom, b.ActualBottom);
                currentMaxBottom = Math.Max(currentMaxBottom, GetMaximumBottom(b, currentMaxBottom));
            }

            return currentMaxBottom;
        }

        /// <summary>
        ///     Get the width of the box at full width (No line breaks)
        /// </summary>
        /// <returns></returns>
        internal float GetFullWidth(Graphics g)
        {
            var sum = 0f;
            var paddingsum = 0f;
            GetFullWidth_WordsWith(this, g, ref sum, ref paddingsum);

            return paddingsum + sum;
        }

        /// <summary>
        ///     Gets the longest word (in width) inside the box, deeply.
        /// </summary>
        /// <param name="b"></param>
        /// <param name="g"></param>
        /// <param name="sum"></param>
        /// <param name="paddingsum"></param>
        /// <returns></returns>
        private void GetFullWidth_WordsWith(Box b, Graphics g, ref float sum, ref float paddingsum)
        {
            if (b.Display != Constants.Inline)
            {
                sum = 0;
            }

            paddingsum += b.ActualBorderLeftWidth + b.ActualBorderRightWidth + b.ActualPaddingRight +
                          b.ActualPaddingLeft;

            if (b.Words.Count > 0)
            {
                sum += b.Words.Sum(word => word.FullWidth);
            }
            else
            {
                foreach (var bb in b.Boxes)
                {
                    GetFullWidth_WordsWith(bb, g, ref sum, ref paddingsum);
                }
            }
        }

        /// <summary>
        ///     Gets the next sibling of this box.
        /// </summary>
        /// <returns>Box after this one on the tree. Null if its the last one.</returns>
        private Box GetNextSibling()
        {
            if (ParentBox == null)
            {
                return null; //This is initial containing block
            }

            var index = ParentBox.Boxes.IndexOf(this);

            if (index < 0) throw new Exception("Box doesn't exist on parent's Box list");

            if (index == ParentBox.Boxes.Count - 1) return null; //This is the last sibling

            return ParentBox.Boxes[index + 1];
        }

        /// <summary>
        ///     Gets if this box has only inline siblings (including itself)
        /// </summary>
        /// <returns></returns>
        internal bool HasJustInlineSiblings()
        {
            if (ParentBox == null)
            {
                return false;
            }

            return ParentBox.ContainsInlinesOnly();
        }

        /// <summary>
        ///     Gets the rectangles where inline box will be drawn. See Remarks for more info.
        /// </summary>
        /// <returns>Rectangles where content should be placed</returns>
        /// <remarks>
        ///     Inline boxes can be splitted across different LineBoxes, that's why this method
        ///     Delivers a rectangle for each LineBox related to this box, if inline.
        /// </remarks>
        /// <summary>
        ///     Inherits inheritable values from parent.
        /// </summary>
        internal void InheritStyle()
        {
            InheritStyle(ParentBox, false);
        }

        /// <summary>
        ///     Inherits inheritable values from specified box.
        /// </summary>
        /// <param name="everything">Set to true to inherit all CSS properties instead of only the ineritables</param>
        /// <param name="godfather">Box to inherit the properties</param>
        internal void InheritStyle(Box godfather, bool everything)
        {
            if (godfather != null)
            {
                IEnumerable<PropertyInfo> pps = everything ? _cssproperties : _inheritables;
                foreach (var prop in pps)
                {
                    prop.SetValue(this,
                        prop.GetValue(godfather, null),
                        null);
                }
            }
        }

        /// <summary>
        ///     Gets the result of collapsing the vertical margins of the two boxes
        /// </summary>
        /// <param name="a">Superior box (checks for margin-bottom)</param>
        /// <param name="b">Inferior box (checks for margin-top)</param>
        /// <returns>Maximum of margins</returns>
        private float MarginCollapse(Box a, Box b)
        {
            return Math.Max(
                a?.ActualMarginBottom ?? 0,
                b?.ActualMarginTop ?? 0);
        }

        /// <summary>
        ///     Measures the bounds of box and children, recursively.
        /// </summary>
        /// <param name="g">Device context to draw</param>
        public virtual void MeasureBounds(Graphics g)
        {
            if (Display == Constants.None) return;

            RectanglesReset();

            MeasureWordsSize(g);

            if (Display == Constants.Block ||
                Display == Constants.ListItem ||
                Display == Constants.Table ||
                Display == Constants.InlineTable ||
                Display == Constants.TableCell ||
                Display == Constants.None)
            {
                #region Measure Bounds

                if (Display != Constants.TableCell)
                {
                    var prevSibling = GetPreviousSibling(this);
                    var left = ContainingBlock.Location.X + ContainingBlock.ActualPaddingLeft + ActualMarginLeft +
                               ContainingBlock.ActualBorderLeftWidth;
                    var top =
                        (prevSibling == null && ParentBox != null ? ParentBox.ClientTop : 0) +
                        MarginCollapse(prevSibling, this) +
                        (prevSibling != null ? prevSibling.ActualBottom + prevSibling.ActualBorderBottomWidth : 0);
                    Location = new PointF(left, top);
                    ActualBottom = top;
                }

                if (Display != Constants.TableCell &&
                    Display != Constants.Table) //Because their width and height are set by CssTable
                {
                    #region Set Width

                    //width at 100% (or auto)
                    var minwidth = GetMinimumWidth();
                    var width =
                        ContainingBlock.Size.Width
                        - ContainingBlock.ActualPaddingLeft - ContainingBlock.ActualPaddingRight
                        - ContainingBlock.ActualBorderLeftWidth - ContainingBlock.ActualBorderRightWidth
                        - ActualMarginLeft - ActualMarginRight - ActualBorderLeftWidth - ActualBorderRightWidth;

                    //Check width if not auto
                    if (Width != Constants.Auto && !string.IsNullOrEmpty(Width))
                    {
                        width = Value.ParseLength(Width, width, this);
                    }

                    if (width < minwidth) width = minwidth;

                    Size = new SizeF(width, Size.Height);

                    #endregion
                }

                //If we're talking about a table here..
                if (Display == Constants.Table || Display == Constants.InlineTable)
                {
                    var tbl = new Table(this, g);
                }
                else if (Display != Constants.None)
                {
                    //If there's just inlines, create LineBoxes
                    if (ContainsInlinesOnly())
                    {
                        ActualBottom = Location.Y;
                        LayoutEngine.CreateLineBoxes(g, this); //This will automatically set the bottom of this block
                    }
                    else
                    {
                        Box lastOne = null; // Boxes[Boxes.Count - 1];

                        //Treat as BlockBox
                        foreach (var box in Boxes)
                        {
                            if (box.Display == Constants.None) continue;
                            //box.Display = CssConstants.Block; //Force to be block, according to CSS spec
                            box.MeasureBounds(g);
                            lastOne = box;
                        }

                        if (lastOne != null)
                            ActualBottom = Math.Max(ActualBottom,
                                lastOne.ActualBottom + lastOne.ActualMarginBottom + ActualPaddingBottom);
                    }
                }

                #endregion
            }

            if (InitialContainer != null)
            {
                InitialContainer.MaximumSize = new SizeF(
                    Math.Max(InitialContainer.MaximumSize.Width, ActualRight),
                    Math.Max(InitialContainer.MaximumSize.Height, ActualBottom));
            }
        }

        /// <summary>
        ///     Measures the word spacing
        /// </summary>
        /// <param name="g"></param>
        private void MeasureWordSpacing(Graphics g)
        {
            _actualWordSpacing = LayoutEngine.WhiteSpace(g, this);

            if (WordSpacing != Constants.Normal)
            {
                var len = Parser.Search(Parser.CssLength, WordSpacing);

                _actualWordSpacing += Value.ParseLength(len, 1, this);
            }
        }

        /// <summary>
        ///     Assigns words its width and height
        /// </summary>
        /// <param name="g"></param>
        internal void MeasureWordsSize(Graphics g)
        {
            if (_wordsSizeMeasured) return;

            //Measure white space if not yet done
            if (float.IsNaN(_actualWordSpacing))
                MeasureWordSpacing(g);

            if (HtmlTag != null && HtmlTag.TagName.Equals("img", StringComparison.CurrentCultureIgnoreCase))
            {
                #region Measure image

                var word = new BoxWord(this, Value.GetImage(GetAttribute("src")));
                Words.Clear();
                Words.Add(word);

                #endregion
            }
            else
            {
                #region Measure text words

                var lastWasSpace = false;

                foreach (var b in Words)
                {
                    var collapse = BoxWordSplitter.CollapsesWhiteSpaces(this);
                    if (BoxWordSplitter.EliminatesLineBreaks(this)) b.ReplaceLineBreaksAndTabs();

                    if (b.IsSpaces)
                    {
                        b.Height = FontLineSpacing;

                        if (b.IsTab)
                        {
                            b.Width = ActualWordSpacing*4; //TODO: Configure tab size
                        }
                        else if (b.IsLineBreak)
                        {
                            b.Width = 0;
                        }
                        else
                        {
                            if (!(lastWasSpace && collapse))
                            {
                                b.Width = ActualWordSpacing*(collapse ? 1 : b.Text.Length);
                            }
                        }

                        lastWasSpace = true;
                    }
                    else
                    {
                        var word = b.Text;

                        CharacterRange[] measurable = {new CharacterRange(0, word.Length)};
                        var sf = new StringFormat();

                        sf.SetMeasurableCharacterRanges(measurable);

                        var regions = g.MeasureCharacterRanges(word, ActualFont,
                            new RectangleF(0, 0, float.MaxValue, float.MaxValue),
                            sf);

                        var s = regions[0].GetBounds(g).Size;
                        var p = regions[0].GetBounds(g).Location;

                        b.LastMeasureOffset = new PointF(p.X, p.Y);
                        b.Width = s.Width; // +p.X;
                        b.Height = s.Height; // +p.Y;

                        lastWasSpace = false;
                    }
                }

                #endregion
            }

            _wordsSizeMeasured = true;
        }

        /// <summary>
        ///     Ensures that the specified length is converted to pixels if necessary
        /// </summary>
        /// <param name="length"></param>
        private string NoEms(string length)
        {
            var len = new Length(length);

            if (len.Unit == Length.CssUnit.Ems)
            {
                length = len.ConvertEmToPixels(GetEmHeight()).ToString();
            }

            return length;
        }

        /// <summary>
        ///     Deeply offsets the top of the box and its contents
        /// </summary>
        /// <param name="amount"></param>
        internal void OffsetTop(float amount)
        {
            var lines = new List<LineBox>();
            foreach (var line in Rectangles.Keys)
                lines.Add(line);

            foreach (var line in lines)
            {
                var r = Rectangles[line];
                Rectangles[line] = new RectangleF(r.X, r.Y + amount, r.Width, r.Height);
            }

            foreach (var word in Words)
            {
                word.Top += amount;
            }

            foreach (var b in Boxes)
            {
                b.OffsetTop(amount);
            }
            //TODO: Aquí me quede: no se mueve bien todo (probar con las tablas rojas)
            Location = new PointF(Location.X, Location.Y + amount);
        }

        /// <summary>
        ///     Paints the fragment
        /// </summary>
        /// <param name="g"></param>
        public void Paint(Graphics g)
        {
            if (Display == Constants.None)
                return;

            if (Display == Constants.TableCell &&
                EmptyCells == Constants.Hide &&
                IsSpaceOrEmpty)
                return;

            var areas = Rectangles.Count == 0
                ? new List<RectangleF>(new[] {Bounds})
                : new List<RectangleF>(Rectangles.Values);

            var rects = areas.ToArray();
            var offset = InitialContainer != null ? InitialContainer.ScrollOffset : PointF.Empty;

            for (var i = 0; i < rects.Length; i++)
            {
                var actualRect = rects[i];
                actualRect.Offset(offset);

                if (InitialContainer != null && HtmlTag != null &&
                    HtmlTag.TagName.Equals("a", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (InitialContainer.LinkRegions.ContainsKey(this)) InitialContainer.LinkRegions.Remove(this);

                    InitialContainer.LinkRegions.Add(this, actualRect);
                }

                PaintBackground(g, actualRect);
                PaintBorder(g, actualRect, i == 0, i == rects.Length - 1);
            }

            if (IsImage)
            {
                var r = Words[0].Bounds;
                r.Offset(offset);
                r.Height -= ActualBorderTopWidth + ActualBorderBottomWidth + ActualPaddingTop + ActualPaddingBottom;
                r.Y += ActualBorderTopWidth + ActualPaddingTop;
                //HACK: round rectangle only when necessary
                g.DrawImage(Words[0].Image, System.Drawing.Rectangle.Round(r));
            }
            else
            {
                var f = ActualFont;
                using (var b = new SolidBrush(Value.GetActualColor(Color)))
                {
                    foreach (var word in Words)
                    {
                        g.DrawString(word.Text, f, b, word.Left - word.LastMeasureOffset.X + offset.X,
                            word.Top + offset.Y);
                    }
                }
            }
            for (var i = 0; i < rects.Length; i++)
            {
                var actualRect = rects[i];
                actualRect.Offset(offset);

                PaintDecoration(g, actualRect, i == 0, i == rects.Length - 1);
            }

            foreach (var b in Boxes)
            {
                b.Paint(g);
            }

            CreateListItemBox(g);

            ListItemBox?.Paint(g);
        }

        /// <summary>
        ///     Paints the border of the box
        /// </summary>
        /// <param name="g"></param>
        /// <param name="rectangle"></param>
        /// <param name="isFirst"></param>
        /// <param name="isLast"></param>
        private void PaintBorder(Graphics g, RectangleF rectangle, bool isFirst, bool isLast)
        {
            var smooth = g.SmoothingMode;

            if (InitialContainer != null && !InitialContainer.AvoidGeometryAntialias && IsRounded)
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
            }

            //Top border
            if (!(string.IsNullOrEmpty(BorderTopStyle) || BorderTopStyle == Constants.None))
            {
                using (var b = new SolidBrush(ActualBorderTopColor))
                {
                    if (BorderTopStyle == Constants.Inset) b.Color = DrawingHelper.Darken(ActualBorderTopColor);
                    g.FillPath(b,
                        DrawingHelper.GetBorderPath(DrawingHelper.Border.Top, this, rectangle, isFirst, isLast));
                }
            }


            if (isLast)
            {
                //Right Border
                if (!(string.IsNullOrEmpty(BorderRightStyle) || BorderRightStyle == Constants.None))
                {
                    using (var b = new SolidBrush(ActualBorderRightColor))
                    {
                        if (BorderRightStyle == Constants.Outset)
                            b.Color = DrawingHelper.Darken(ActualBorderRightColor);
                        g.FillPath(b,
                            DrawingHelper.GetBorderPath(DrawingHelper.Border.Right, this, rectangle, isFirst,
                                isLast));
                    }
                }
            }

            //Bottom border
            if (!(string.IsNullOrEmpty(BorderBottomStyle) || BorderBottomStyle == Constants.None))
            {
                using (var b = new SolidBrush(ActualBorderBottomColor))
                {
                    if (BorderBottomStyle == Constants.Outset)
                        b.Color = DrawingHelper.Darken(ActualBorderBottomColor);
                    g.FillPath(b,
                        DrawingHelper.GetBorderPath(DrawingHelper.Border.Bottom, this, rectangle, isFirst, isLast));
                }
            }

            if (isFirst)
            {
                //Left Border
                if (!(string.IsNullOrEmpty(BorderLeftStyle) || BorderLeftStyle == Constants.None))
                {
                    using (var b = new SolidBrush(ActualBorderLeftColor))
                    {
                        if (BorderLeftStyle == Constants.Inset)
                            b.Color = DrawingHelper.Darken(ActualBorderLeftColor);
                        g.FillPath(b,
                            DrawingHelper.GetBorderPath(DrawingHelper.Border.Left, this, rectangle, true,
                                isLast));
                    }
                }
            }

            g.SmoothingMode = smooth;
        }

        /// <summary>
        ///     Paints the background of the box
        /// </summary>
        /// <param name="g"></param>
        /// <param name="rectangle"></param>
        private void PaintBackground(Graphics g, RectangleF rectangle)
        {
            //HACK: Background rectangles are being deactivated when justifying text.
            if (ContainingBlock.TextAlign == Constants.Justify) return;

            GraphicsPath roundrect = null;
            Brush b = null;
            var smooth = g.SmoothingMode;

            if (IsRounded)
            {
                roundrect = DrawingHelper.GetRoundRect(rectangle, ActualCornerNW, ActualCornerNE, ActualCornerSE,
                    ActualCornerSW);
            }

            if (BackgroundGradient != Constants.None && rectangle.Width > 0 && rectangle.Height > 0)
            {
                b = new LinearGradientBrush(rectangle, ActualBackgroundColor, ActualBackgroundGradient,
                    ActualBackgroundGradientAngle);
            }
            else
            {
                b = new SolidBrush(ActualBackgroundColor);
            }

            if (InitialContainer != null && !InitialContainer.AvoidGeometryAntialias && IsRounded)
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
            }

            if (roundrect != null)
            {
                g.FillPath(b, roundrect);
            }
            else
            {
                g.FillRectangle(b, rectangle);
            }

            g.SmoothingMode = smooth;

            roundrect?.Dispose();
            b.Dispose();
        }

        /// <summary>
        ///     Paints the text decoration
        /// </summary>
        /// <param name="g"></param>
        /// <param name="rectangle"></param>
        /// <param name="isFirst"></param>
        /// <param name="isLast"></param>
        private void PaintDecoration(Graphics g, RectangleF rectangle, bool isFirst, bool isLast)
        {
            if (string.IsNullOrEmpty(TextDecoration) || TextDecoration == Constants.None || IsImage) return;

            var desc = LayoutEngine.GetDescent(ActualFont);
            var asc = LayoutEngine.GetAscent(ActualFont);
            var y = 0f;

            if (TextDecoration == Constants.Underline)
            {
                y = rectangle.Bottom - desc;
            }
            else if (TextDecoration == Constants.LineThrough)
            {
                y = rectangle.Bottom - desc - asc/2;
            }
            else if (TextDecoration == Constants.Overline)
            {
                y = rectangle.Bottom - desc - asc - 2;
            }

            y -= ActualPaddingBottom - ActualBorderBottomWidth;

            var x1 = rectangle.X;
            var x2 = rectangle.Right;

            if (isFirst) x1 += ActualPaddingLeft + ActualBorderLeftWidth;
            if (isLast) x2 -= ActualPaddingRight + ActualBorderRightWidth;

            g.DrawLine(new Pen(ActualColor), x1, y, x2, y);
        }

        /// <summary>
        ///     Offsets the rectangle of the specified linebox by the specified gap,
        ///     and goes deep for rectangles of children in that linebox.
        /// </summary>
        /// <param name="lineBox"></param>
        /// <param name="gap"></param>
        internal void OffsetRectangle(LineBox lineBox, float gap)
        {
            if (Rectangles.ContainsKey(lineBox))
            {
                var r = Rectangles[lineBox];
                Rectangles[lineBox] = new RectangleF(r.X, r.Y + gap, r.Width, r.Height);
            }

            //foreach (Box b in Boxes)
            //{
            //    b.OffsetRectangle(lineBox, gap);
            //}
        }

        /// <summary>
        ///     Resets the <see cref="Rectangles" /> array
        /// </summary>
        internal void RectanglesReset()
        {
            Rectangles.Clear();
        }

        /// <summary>
        ///     Removes boxes that are just blank spaces
        /// </summary>
        internal void RemoveAnonymousSpaces()
        {
            for (var i = 0; i < Boxes.Count; i++)
            {
                if (Boxes[i] is CssAnonymousSpaceBlockBox || Boxes[i] is CssAnonymousSpaceBox)
                {
                    Boxes.RemoveAt(i);
                    i--;
                }
            }
        }

        /// <summary>
        ///     Sets the bounds of the box
        /// </summary>
        /// <param name="r"></param>
        public void SetBounds(System.Drawing.Rectangle r)
        {
            SetBounds(new RectangleF(r.X, r.Y, r.Width, r.Height));
        }

        /// <summary>
        ///     Sets the bounds of the box
        /// </summary>
        /// <param name="rectangle"></param>
        public void SetBounds(RectangleF rectangle)
        {
            Size = rectangle.Size;
            Location = rectangle.Location;
        }

        /// <summary>
        ///     ToString override.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var t = GetType().Name;
            if (HtmlTag != null)
            {
                t = string.Format("<{0}>", HtmlTag.TagName);
            }

            if (ParentBox == null)
            {
                return "Initial Container";
            }
            if (Display == Constants.Block)
            {
                return string.Format("{0} BlockBox {2}, Children:{1}", t, Boxes.Count, FontSize);
            }
            if (Display == Constants.None)
            {
                return string.Format("{0} None", t);
            }
            return string.Format("{0} {2}: {1}", t, Text, Display);


            //return base.ToString();
        }

        /// <summary>
        ///     Splits the text into words and saves the result
        /// </summary>
        private void UpdateWords()
        {
            Words.Clear();

            var splitter = new BoxWordSplitter(this, Text);
            splitter.SplitWords();

            Words.AddRange(splitter.Words);
        }

        #endregion
    }
}