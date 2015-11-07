using System;
using System.Globalization;

namespace W0.Ui.HtmlRender.Css
{
    /// <summary>
    ///     Represents and gets info about a CSS Length
    /// </summary>
    /// <remarks>
    ///     http://www.w3.org/TR/CSS21/syndata.html#length-units
    /// </remarks>
    public class Length
    {
        #region Enum

        /// <summary>
        ///     Represents the possible units of the CSS lengths
        /// </summary>
        /// <remarks>
        ///     http://www.w3.org/TR/CSS21/syndata.html#length-units
        /// </remarks>
        public enum CssUnit
        {
            None,

            Ems,

            Pixels,

            Ex,

            Inches,

            Centimeters,

            Milimeters,

            Points,

            Picas
        }

        #endregion

        #region Fields

        private readonly float _number;

        #endregion

        #region Ctor

        /// <summary>
        ///     Creates a new CssLength from a length specified on a CSS style sheet or fragment
        /// </summary>
        /// <param name="internalLength">Length as specified in the Style Sheet or style fragment</param>
        public Length(string internalLength)
        {
            InternalLength = internalLength;
            _number = 0f;
            Unit = CssUnit.None;
            IsPercentage = false;

            //Return zero if no length specified, zero specified
            if (string.IsNullOrEmpty(internalLength) || internalLength == "0") return;

            //If percentage, use ParseNumber
            if (internalLength.EndsWith("%"))
            {
                _number = Value.ParseNumber(internalLength, 1);
                IsPercentage = true;
                return;
            }

            //If no units, has error
            if (internalLength.Length < 3)
            {
                float.TryParse(internalLength, out _number);
                HasError = true;
                return;
            }

            //Get units of the length
            var u = internalLength.Substring(internalLength.Length - 2, 2);

            //Number of the length
            var number = internalLength.Substring(0, internalLength.Length - 2);

            //TODO: Units behave different in paper and in screen!
            switch (u)
            {
                case Constants.Em:
                    Unit = CssUnit.Ems;
                    IsRelative = true;
                    break;
                case Constants.Ex:
                    Unit = CssUnit.Ex;
                    IsRelative = true;
                    break;
                case Constants.Px:
                    Unit = CssUnit.Pixels;
                    IsRelative = true;
                    break;
                case Constants.Mm:
                    Unit = CssUnit.Milimeters;
                    break;
                case Constants.Cm:
                    Unit = CssUnit.Centimeters;
                    break;
                case Constants.In:
                    Unit = CssUnit.Inches;
                    break;
                case Constants.Pt:
                    Unit = CssUnit.Points;
                    break;
                case Constants.Pc:
                    Unit = CssUnit.Picas;
                    break;
                default:
                    HasError = true;
                    return;
            }

            if (!float.TryParse(number, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out _number))
            {
                HasError = true;
            }
        }

        #endregion

        #region Props

        /// <summary>
        ///     Gets the number in the length
        /// </summary>
        public float Number
        {
            get { return _number; }
        }

        /// <summary>
        ///     Gets if the length has some parsing error
        /// </summary>
        public bool HasError { get; }


        /// <summary>
        ///     Gets if the length represents a precentage (not actually a length)
        /// </summary>
        public bool IsPercentage { get; }


        /// <summary>
        ///     Gets if the length is specified in relative units
        /// </summary>
        public bool IsRelative { get; }

        /// <summary>
        ///     Gets the unit of the length
        /// </summary>
        public CssUnit Unit { get; }

        /// <summary>
        ///     Gets the length as specified in the string
        /// </summary>
        public string InternalLength { get; }

        #endregion

        #region Methods

        /// <summary>
        ///     If length is in Ems, returns its value in points
        /// </summary>
        /// <param name="emSize">Em size factor to multiply</param>
        /// <returns>Points size of this em</returns>
        /// <exception cref="InvalidOperationException">If length has an error or isn't in ems</exception>
        public Length ConvertEmToPoints(float emSize)
        {
            if (HasError) throw new InvalidOperationException("Invalid length");
            if (Unit != CssUnit.Ems) throw new InvalidOperationException("Length is not in ems");

            return
                new Length(string.Format("{0}pt",
                    Convert.ToSingle(Number*emSize).ToString("0.0", NumberFormatInfo.InvariantInfo)));
        }

        /// <summary>
        ///     If length is in Ems, returns its value in pixels
        /// </summary>
        /// <returns>Pixels size of this em</returns>
        /// <exception cref="InvalidOperationException">If length has an error or isn't in ems</exception>
        public Length ConvertEmToPixels(float pixelFactor)
        {
            if (HasError) throw new InvalidOperationException("Invalid length");
            if (Unit != CssUnit.Ems) throw new InvalidOperationException("Length is not in ems");

            return
                new Length(string.Format("{0}px",
                    Convert.ToSingle(Number*pixelFactor).ToString("0.0", NumberFormatInfo.InvariantInfo)));
        }

        /// <summary>
        ///     Returns the length formatted ready for CSS interpreting.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (HasError)
            {
                return string.Empty;
            }
            if (IsPercentage)
            {
                return string.Format(NumberFormatInfo.InvariantInfo, "{0}%", Number);
            }
            var u = string.Empty;

            switch (Unit)
            {
                case CssUnit.None:
                    break;
                case CssUnit.Ems:
                    u = "em";
                    break;
                case CssUnit.Pixels:
                    u = "px";
                    break;
                case CssUnit.Ex:
                    u = "ex";
                    break;
                case CssUnit.Inches:
                    u = "in";
                    break;
                case CssUnit.Centimeters:
                    u = "cm";
                    break;
                case CssUnit.Milimeters:
                    u = "mm";
                    break;
                case CssUnit.Points:
                    u = "pt";
                    break;
                case CssUnit.Picas:
                    u = "pc";
                    break;
            }

            return string.Format(NumberFormatInfo.InvariantInfo, "{0}{1}", Number, u);
        }

        #endregion
    }
}