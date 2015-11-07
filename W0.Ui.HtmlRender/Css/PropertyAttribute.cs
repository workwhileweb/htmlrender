using System;

namespace W0.Ui.HtmlRender.Css
{
    /// <summary>
    ///     Used to mark a property as a Css property.
    ///     The <see cref="Name" /> property is used to specify the oficial CSS name
    /// </summary>
    public class PropertyAttribute : Attribute
    {
        #region Ctor

        /// <summary>
        ///     Creates a new CssPropertyAttribute
        /// </summary>
        /// <param name="name">Name of the Css property</param>
        public PropertyAttribute(string name)
        {
            Name = name;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets the name of the CSS property
        /// </summary>
        public string Name { get; set; }

        #endregion

        #region Fields

        #endregion
    }
}