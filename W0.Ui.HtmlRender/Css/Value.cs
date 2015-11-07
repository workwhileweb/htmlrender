using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace W0.Ui.HtmlRender.Css
{
    public static class Value
    {
        /// <summary>
        ///     Evals a number and returns it. If number is a percentage, it will be multiplied by <see cref="hundredPercent" />
        /// </summary>
        /// <param name="number">Number to be parsed</param>
        /// <param name="hundredPercent"></param>
        /// <returns>Parsed number. Zero if error while parsing.</returns>
        public static float ParseNumber(string number, float hundredPercent)
        {
            if (string.IsNullOrEmpty(number))
            {
                return 0f;
            }

            var toParse = number;
            var isPercent = number.EndsWith("%");
            float result;

            if (isPercent) toParse = number.Substring(0, number.Length - 1);

            if (!float.TryParse(toParse, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out result))
            {
                return 0f;
            }

            if (isPercent)
            {
                result = (result/100f)*hundredPercent;
            }

            return result;
        }

        /// <summary>
        ///     Parses a length. Lengths are followed by an unit identifier (e.g. 10px, 3.1em)
        /// </summary>
        /// <param name="length">Specified length</param>
        /// <param name="hundredPercent">Equivalent to 100 percent when length is percentage</param>
        /// <param name="box"></param>
        /// <returns></returns>
        public static float ParseLength(string length, float hundredPercent, Box box)
        {
            return ParseLength(length, hundredPercent, box, box.GetEmHeight(), false);
        }

        /// <summary>
        ///     Parses a length. Lengths are followed by an unit identifier (e.g. 10px, 3.1em)
        /// </summary>
        /// <param name="length">Specified length</param>
        /// <param name="hundredPercent">Equivalent to 100 percent when length is percentage</param>
        /// <param name="box"></param>
        /// <param name="emFactor"></param>
        /// <param name="returnPoints">Allows the return float to be in points. If false, result will be pixels</param>
        /// <returns></returns>
        public static float ParseLength(string length, float hundredPercent, Box box, float emFactor,
            bool returnPoints)
        {
            //Return zero if no length specified, zero specified
            if (string.IsNullOrEmpty(length) || length == "0") return 0f;

            //If percentage, use ParseNumber
            if (length.EndsWith("%")) return ParseNumber(length, hundredPercent);

            //If no units, return zero
            if (length.Length < 3) return 0f;

            //Get units of the length
            var unit = length.Substring(length.Length - 2, 2);

            //Factor will depend on the unit
            float factor;

            //Number of the length
            var number = length.Substring(0, length.Length - 2);

            //TODO: Units behave different in paper and in screen!
            switch (unit)
            {
                case Constants.Em:
                    factor = emFactor;
                    break;
                case Constants.Px:
                    factor = 1f;
                    break;
                case Constants.Mm:
                    factor = 3f; //3 pixels per millimeter
                    break;
                case Constants.Cm:
                    factor = 37f; //37 pixels per centimeter
                    break;
                case Constants.In:
                    factor = 96f; //96 pixels per inch
                    break;
                case Constants.Pt:
                    factor = 96f/72f; // 1 point = 1/72 of inch

                    if (returnPoints)
                    {
                        return ParseNumber(number, hundredPercent);
                    }

                    break;
                case Constants.Pc:
                    factor = 96f/72f*12f; // 1 pica = 12 points
                    break;
                default:
                    factor = 0f;
                    break;
            }


            return factor*ParseNumber(number, hundredPercent);
        }

        /// <summary>
        ///     Parses a color value in CSS style; e.g. #ff0000, red, rgb(255,0,0), rgb(100%, 0, 0)
        /// </summary>
        /// <param name="colorValue">Specified color value; e.g. #ff0000, red, rgb(255,0,0), rgb(100%, 0, 0)</param>
        /// <returns>System.Drawing.Color value</returns>
        public static Color GetActualColor(string colorValue)
        {
            int r;
            int g;
            int b;
            var onError = Color.Empty;

            if (string.IsNullOrEmpty(colorValue)) return onError;

            colorValue = colorValue.ToLower().Trim();

            if (colorValue.StartsWith("#"))
            {
                #region hexadecimal forms

                var hex = colorValue.Substring(1);

                if (hex.Length == 6)
                {
                    r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                    g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                    b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
                }
                else if (hex.Length == 3)
                {
                    r = int.Parse(new string(hex.Substring(0, 1)[0], 2), NumberStyles.HexNumber);
                    g = int.Parse(new string(hex.Substring(1, 1)[0], 2), NumberStyles.HexNumber);
                    b = int.Parse(new string(hex.Substring(2, 1)[0], 2), NumberStyles.HexNumber);
                }
                else
                {
                    return onError;
                }

                #endregion
            }
            else if (colorValue.StartsWith("rgb(") && colorValue.EndsWith(")"))
            {
                #region RGB forms

                var rgb = colorValue.Substring(4, colorValue.Length - 5);
                var chunks = rgb.Split(',');

                if (chunks.Length == 3)
                {
                    unchecked
                    {
                        r = Convert.ToInt32(ParseNumber(chunks[0].Trim(), 255f));
                        g = Convert.ToInt32(ParseNumber(chunks[1].Trim(), 255f));
                        b = Convert.ToInt32(ParseNumber(chunks[2].Trim(), 255f));
                    }
                }
                else
                {
                    return onError;
                }

                #endregion
            }
            else
            {
                #region Color Constants

                var hex = string.Empty;

                switch (colorValue)
                {
                    case Constants.Maroon:
                        hex = "#800000";
                        break;
                    case Constants.Red:
                        hex = "#ff0000";
                        break;
                    case Constants.Orange:
                        hex = "#ffA500";
                        break;
                    case Constants.Olive:
                        hex = "#808000";
                        break;
                    case Constants.Purple:
                        hex = "#800080";
                        break;
                    case Constants.Fuchsia:
                        hex = "#ff00ff";
                        break;
                    case Constants.White:
                        hex = "#ffffff";
                        break;
                    case Constants.Lime:
                        hex = "#00ff00";
                        break;
                    case Constants.Green:
                        hex = "#008000";
                        break;
                    case Constants.Navy:
                        hex = "#000080";
                        break;
                    case Constants.Blue:
                        hex = "#0000ff";
                        break;
                    case Constants.Aqua:
                        hex = "#00ffff";
                        break;
                    case Constants.Teal:
                        hex = "#008080";
                        break;
                    case Constants.Black:
                        hex = "#000000";
                        break;
                    case Constants.Silver:
                        hex = "#c0c0c0";
                        break;
                    case Constants.Gray:
                        hex = "#808080";
                        break;
                    case Constants.Yellow:
                        hex = "#FFFF00";
                        break;
                }

                if (string.IsNullOrEmpty(hex))
                {
                    return onError;
                }
                var c = GetActualColor(hex);
                r = c.R;
                g = c.G;
                b = c.B;

                #endregion
            }

            return Color.FromArgb(r, g, b);
        }

        /// <summary>
        ///     Parses a border value in CSS style; e.g. 1px, 1, thin, thick, medium
        /// </summary>
        /// <param name="borderValue"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static float GetActualBorderWidth(string borderValue, Box b)
        {
            if (string.IsNullOrEmpty(borderValue))
            {
                return GetActualBorderWidth(Constants.Medium, b);
            }

            switch (borderValue)
            {
                case Constants.Thin:
                    return 1f;
                case Constants.Medium:
                    return 2f;
                case Constants.Thick:
                    return 4f;
                default:
                    return Math.Abs(ParseLength(borderValue, 1, b));
            }
        }

        /// <summary>
        ///     Split the value by spaces; e.g. Useful in values like 'padding:5 4 3 inherit'
        /// </summary>
        /// <param name="value">Value to be splitted</param>
        /// <returns>Splitted and trimmed values</returns>
        public static string[] SplitValues(string value)
        {
            return SplitValues(value, ' ');
        }

        /// <summary>
        ///     Split the value by the specified separator; e.g. Useful in values like 'padding:5 4 3 inherit'
        /// </summary>
        /// <param name="value">Value to be splitted</param>
        /// <param name="separator"></param>
        /// <returns>Splitted and trimmed values</returns>
        public static string[] SplitValues(string value, char separator)
        {
            //TODO: CRITICAL! Don't split values on parenthesis (like rgb(0, 0, 0)) or quotes ("strings")


            if (string.IsNullOrEmpty(value)) return new string[] {};

            var values = value.Split(separator);

            return values.Select(t => t.Trim()).Where(val => !string.IsNullOrEmpty(val)).ToArray();
        }

        /// <summary>
        ///     Detects the type name in a path.
        ///     E.g. Gets System.Drawing.Graphics from a path like System.Drawing.Graphics.Clear
        /// </summary>
        /// <param name="path"></param>
        /// <param name="moreInfo"></param>
        /// <returns></returns>
        private static Type GetTypeInfo(string path, ref string moreInfo)
        {
            var lastDot = path.LastIndexOf('.');

            if (lastDot < 0) return null;

            var type = path.Substring(0, lastDot);
            moreInfo = path.Substring(lastDot + 1);
            moreInfo = moreInfo.Replace("(", string.Empty).Replace(")", string.Empty);


            return Renderer.References.Select(a => a.GetType(type, false, true)).FirstOrDefault(t => t != null);
        }

        /// <summary>
        ///     Returns the object specific to the path
        /// </summary>
        /// <param name="path"></param>
        /// <returns>One of the following possible objects: FileInfo, MethodInfo, PropertyInfo</returns>
        private static object DetectSource(string path)
        {
            if (path.StartsWith("method:", StringComparison.CurrentCultureIgnoreCase))
            {
                var methodName = string.Empty;
                var t = GetTypeInfo(path.Substring(7), ref methodName);
                if (t == null) return null;
                var method = t.GetMethod(methodName);

                if (!method.IsStatic || method.GetParameters().Length > 0)
                {
                    return null;
                }

                return method;
            }
            if (path.StartsWith("property:", StringComparison.CurrentCultureIgnoreCase))
            {
                var propName = string.Empty;
                var t = GetTypeInfo(path.Substring(9), ref propName);
                var prop = t?.GetProperty(propName);

                return prop;
            }
            if (Uri.IsWellFormedUriString(path, UriKind.RelativeOrAbsolute))
            {
                return new Uri(path);
            }
            return new FileInfo(path);
        }

        /// <summary>
        ///     Gets the image of the specified path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Image GetImage(string path)
        {
            var source = DetectSource(path);

            var finfo = source as FileInfo;
            var prop = source as PropertyInfo;
            var method = source as MethodInfo;

            try
            {
                if (finfo != null)
                {
                    if (!finfo.Exists) return null;

                    return Image.FromFile(finfo.FullName);
                }
                if (prop != null)
                {
                    if (!prop.PropertyType.IsSubclassOf(typeof (Image)) && prop.PropertyType != typeof (Image))
                        return null;

                    return prop.GetValue(null, null) as Image;
                }
                if (method != null)
                {
                    if (!method.ReturnType.IsSubclassOf(typeof (Image))) return null;

                    return method.Invoke(null, null) as Image;
                }
                return null;
            }
            catch
            {
                return new Bitmap(50, 50); //TODO: Return error image
            }
        }

        /// <summary>
        ///     Gets the content of the stylesheet specified in the path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetStyleSheet(string path)
        {
            var source = DetectSource(path);

            var finfo = source as FileInfo;
            var prop = source as PropertyInfo;
            var method = source as MethodInfo;

            try
            {
                if (finfo != null)
                {
                    if (!finfo.Exists) return null;

                    var sr = new StreamReader(finfo.FullName);
                    var result = sr.ReadToEnd();
                    sr.Dispose();

                    return result;
                }
                if (prop != null)
                {
                    if (prop.PropertyType != typeof (string)) return null;

                    return prop.GetValue(null, null) as string;
                }
                if (method == null) return string.Empty;
                if (method.ReturnType != typeof (string)) return null;

                return method.Invoke(null, null) as string;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        ///     Executes the desired action when the user clicks a link
        /// </summary>
        /// <param name="href"></param>
        public static void GoLink(string href)
        {
            var source = DetectSource(href);

            var finfo = source as FileInfo;
            //var prop = source as PropertyInfo;
            var method = source as MethodInfo;
            var uri = source as Uri;

            if (finfo != null || uri != null)
            {
                var nfo = new ProcessStartInfo(href) {UseShellExecute = true};

                Process.Start(nfo);
            }
            else
            {
                method?.Invoke(null, null);
            }
        }
    }
}