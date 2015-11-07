using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using W0.Ui.HtmlRender.Css;

namespace W0.Ui.HtmlRender
{
    public class InitialContainer
        : Box
    {
        #region Fields

        #endregion

        #region Ctor

        public InitialContainer()
        {
            _initialContainer = this;
            MediaBlocks = new Dictionary<string, Dictionary<string, Block>>();
            LinkRegions = new Dictionary<Box, RectangleF>();
            MediaBlocks.Add("all", new Dictionary<string, Block>());

            Display = Css.Constants.Block;

            FeedStyleSheet(Defaults.DefaultStyleSheet);
        }

        public InitialContainer(string documentSource)
            : this()
        {
            DocumentSource = documentSource;
            ParseDocument();
            CascadeStyles(this);
            BlockCorrection(this);
        }

        #endregion

        #region Props

        /// <summary>
        ///     Gets the link regions of the container
        /// </summary>
        internal Dictionary<Box, RectangleF> LinkRegions { get; }


        /// <summary>
        ///     Gets the blocks of style defined on this structure, separated by media type.
        ///     General blocks are defined under the "all" Key.
        /// </summary>
        /// <remarks>
        ///     Normal use of this dictionary will be something like:
        ///     MediaBlocks["print"]["strong"].Properties
        ///     - Or -
        ///     MediaBlocks["all"]["strong"].Properties
        /// </remarks>
        public Dictionary<string, Dictionary<string, Block>> MediaBlocks { get; }

        /// <summary>
        ///     Gets the document's source
        /// </summary>
        public string DocumentSource { get; }

        /// <summary>
        ///     Gets or sets a value indicating if antialiasing should be avoided
        ///     for geometry like backgrounds and borders
        /// </summary>
        public bool AvoidGeometryAntialias { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating if antialiasing should be avoided
        ///     for text rendering
        /// </summary>
        public bool AvoidTextAntialias { get; set; }

        /// <summary>
        ///     Gets or sets the maximum size of the container
        /// </summary>
        public SizeF MaximumSize { get; set; }

        /// <summary>
        ///     Gets or sets the scroll offset of the document
        /// </summary>
        public PointF ScrollOffset { get; set; }

        #endregion

        #region Methods

        /// <summary>
        ///     Feeds the source of the stylesheet
        /// </summary>
        /// <param name="stylesheet"></param>
        public void FeedStyleSheet(string stylesheet)
        {
            if (string.IsNullOrEmpty(stylesheet)) return;

            //Convert everything to lower-case
            stylesheet = stylesheet.ToLower();

            #region Remove comments

            for (var comments = Parser.Match(Parser.CssComments, stylesheet);
                comments.Count > 0;
                comments = Parser.Match(Parser.CssComments, stylesheet))
            {
                stylesheet = stylesheet.Remove(comments[0].Index, comments[0].Length);
            }

            #endregion

            #region Extract @media blocks

            //MatchCollection atrules = Parser.Match(Parser.CssAtRules, stylesheet);

            for (var atrules = Parser.Match(Parser.CssAtRules, stylesheet);
                atrules.Count > 0;
                atrules = Parser.Match(Parser.CssAtRules, stylesheet))
            {
                var match = atrules[0];

                //Extract whole at-rule
                var atrule = match.Value;

                //Remove rule from sheet
                stylesheet = stylesheet.Remove(match.Index, match.Length);

                //Just processs @media rules
                if (!atrule.StartsWith("@media")) continue;

                //Extract specified media types
                var types = Parser.Match(Parser.CssMediaTypes, atrule);

                if (types.Count == 1)
                {
                    var line = types[0].Value;

                    if (line.StartsWith("@media") && line.EndsWith("{"))
                    {
                        //Get specified media types in the at-rule
                        var media = line.Substring(6, line.Length - 7).Split(' ');

                        //Scan media types
                        for (var i = 0; i < media.Length; i++)
                        {
                            if (string.IsNullOrEmpty(media[i].Trim())) continue;

                            //Get blocks inside the at-rule
                            var insideBlocks = Parser.Match(Parser.CssBlocks, atrule);

                            //Scan blocks and feed them to the style sheet
                            foreach (Match insideBlock in insideBlocks)
                            {
                                FeedStyleBlock(media[i].Trim(), insideBlock.Value);
                            }
                        }
                    }
                }
            }

            #endregion

            #region Extract general blocks

            //This blocks are added under the "all" keyword

            var blocks = Parser.Match(Parser.CssBlocks, stylesheet);

            foreach (Match match in blocks)
            {
                FeedStyleBlock("all", match.Value);
            }

            #endregion
        }

        /// <summary>
        ///     Feeds the style with a block about the specific media.
        ///     When no media is specified, "all" will be used
        /// </summary>
        /// <param name="media"></param>
        /// <param name="block"></param>
        private void FeedStyleBlock(string media, string block)
        {
            if (string.IsNullOrEmpty(media)) media = "all";

            var bracketIndex = block.IndexOf("{", StringComparison.Ordinal);
            var blockSource = block.Substring(bracketIndex).Replace("{", string.Empty).Replace("}", string.Empty);

            if (bracketIndex < 0) return;

            ///TODO: Only supporting definitions like:
            /// h1, h2, h3 {...
            ///Support needed for definitions like:
            ///* {...
            ///h1 h2 {...
            ///h1 > h2 {...
            ///h1:before {...
            ///h1:hover {...
            var classes = block.Substring(0, bracketIndex).Split(',');

            for (var i = 0; i < classes.Length; i++)
            {
                var className = classes[i].Trim();
                if (string.IsNullOrEmpty(className)) continue;

                var newblock = new Block(blockSource);

                //Create media blocks if necessary
                if (!MediaBlocks.ContainsKey(media)) MediaBlocks.Add(media, new Dictionary<string, Block>());

                if (!MediaBlocks[media].ContainsKey(className))
                {
                    //Create block
                    MediaBlocks[media].Add(className, newblock);
                }
                else
                {
                    //Merge newblock and oldblock's properties

                    var oldblock = MediaBlocks[media][className];

                    foreach (var property in newblock.Properties.Keys)
                    {
                        if (oldblock.Properties.ContainsKey(property))
                        {
                            oldblock.Properties[property] = newblock.Properties[property];
                        }
                        else
                        {
                            oldblock.Properties.Add(property, newblock.Properties[property]);
                        }
                    }

                    oldblock.UpdatePropertyValues();
                }
            }
        }

        /// <summary>
        ///     Parses the document
        /// </summary>
        private void ParseDocument()
        {
            var root = this;
            var tags = Parser.Match(Parser.HtmlTag, DocumentSource);
            Box curBox = root;
            var lastEnd = -1;

            foreach (Match tagmatch in tags)
            {
                var text = tagmatch.Index > 0
                    ? DocumentSource.Substring(lastEnd + 1, tagmatch.Index - lastEnd - 1)
                    : string.Empty;

                if (!string.IsNullOrEmpty(text.Trim()))
                {
                    var abox = new AnonymousBox(curBox);
                    abox.Text = text;
                }
                else if (text != null && text.Length > 0)
                {
                    var sbox = new CssAnonymousSpaceBox(curBox);
                    sbox.Text = text;
                }

                var tag = new Tag(tagmatch.Value);

                if (tag.IsClosing)
                {
                    curBox = FindParent(tag.TagName, curBox);
                }
                else if (tag.IsSingle)
                {
                    var foo = new Box(curBox, tag);
                }
                else
                {
                    curBox = new Box(curBox, tag);
                }


                lastEnd = tagmatch.Index + tagmatch.Length - 1;
            }

            var finaltext = DocumentSource.Substring((lastEnd > 0 ? lastEnd + 1 : 0),
                DocumentSource.Length - lastEnd - 1 + (lastEnd == 0 ? 1 : 0));

            if (!string.IsNullOrEmpty(finaltext))
            {
                var abox = new AnonymousBox(curBox);
                abox.Text = finaltext;
            }
        }

        /// <summary>
        ///     Recursively searches for the parent with the specified HTML Tag name
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="b"></param>
        private Box FindParent(string tagName, Box b)
        {
            if (b == null)
            {
                return InitialContainer;
            }
            if (b.HtmlTag != null && b.HtmlTag.TagName.Equals(tagName, StringComparison.CurrentCultureIgnoreCase))
            {
                return b.ParentBox == null ? InitialContainer : b.ParentBox;
            }
            return FindParent(tagName, b.ParentBox);
        }

        /// <summary>
        ///     Applies style to all boxes in the tree
        /// </summary>
        private void CascadeStyles(Box startBox)
        {
            var someBlock = false;

            foreach (var b in startBox.Boxes)
            {
                b.InheritStyle();

                if (b.HtmlTag != null)
                {
                    //Check if tag name matches with a defined class
                    if (MediaBlocks["all"].ContainsKey(b.HtmlTag.TagName))
                    {
                        MediaBlocks["all"][b.HtmlTag.TagName].AssignTo(b);
                    }

                    //Check if class="" attribute matches with a defined style
                    if (b.HtmlTag.HasAttribute("class") &&
                        MediaBlocks["all"].ContainsKey("." + b.HtmlTag.Attributes["class"]))
                    {
                        MediaBlocks["all"]["." + b.HtmlTag.Attributes["class"]].AssignTo(b);
                    }

                    b.HtmlTag.TranslateAttributes(b);

                    //Check for the style="" attribute
                    if (b.HtmlTag.HasAttribute("style"))
                    {
                        var block = new Block(b.HtmlTag.Attributes["style"]);
                        block.AssignTo(b);
                    }

                    //Check for the <style> tag
                    if (b.HtmlTag.TagName.Equals("style", StringComparison.CurrentCultureIgnoreCase) &&
                        b.Boxes.Count == 1)
                    {
                        FeedStyleSheet(b.Boxes[0].Text);
                    }

                    //Check for the <link rel=stylesheet> tag
                    if (b.HtmlTag.TagName.Equals("link", StringComparison.CurrentCultureIgnoreCase) &&
                        b.GetAttribute("rel", string.Empty)
                            .Equals("stylesheet", StringComparison.CurrentCultureIgnoreCase))
                    {
                        FeedStyleSheet(Value.GetStyleSheet(b.GetAttribute("href", string.Empty)));
                    }
                }

                CascadeStyles(b);
            }

            if (someBlock)
            {
                foreach (var box in startBox.Boxes)
                {
                    box.Display = Css.Constants.Block;
                }
            }
        }

        /// <summary>
        ///     Makes block boxes be among only block boxes.
        ///     Inline boxes should live in a pool of Inline boxes only.
        /// </summary>
        /// <param name="startBox"></param>
        private void BlockCorrection(Box startBox)
        {
            var inlinesonly = startBox.ContainsInlinesOnly();

            if (!inlinesonly)
            {
                var inlinegroups = BlockCorrection_GetInlineGroups(startBox);

                foreach (var group in inlinegroups)
                {
                    if (group.Count == 0) continue;

                    if (group.Count == 1 && group[0] is CssAnonymousSpaceBox)
                    {
                        var sbox = new CssAnonymousSpaceBlockBox(startBox, group[0]);

                        group[0].ParentBox = sbox;
                    }
                    else
                    {
                        var newbox = new AnonymousBlockBox(startBox, group[0]);

                        foreach (var inline in group)
                        {
                            inline.ParentBox = newbox;
                        }
                    }
                }
            }

            foreach (var b in startBox.Boxes)
            {
                BlockCorrection(b);
            }
        }

        /// <summary>
        ///     Scans the boxes (non-deeply) of the box, and returns groups of contiguous inline boxes.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        private List<List<Box>> BlockCorrection_GetInlineGroups(Box box)
        {
            var result = new List<List<Box>>();
            List<Box> current = null;

            //Scan boxes
            for (var i = 0; i < box.Boxes.Count; i++)
            {
                var b = box.Boxes[i];

                //If inline, add it to the current group
                if (b.Display == Css.Constants.Inline)
                {
                    if (current == null)
                    {
                        current = new List<Box>();
                        result.Add(current);
                    }
                    current.Add(b);
                }
                else
                {
                    current = null;
                }
            }


            //If last list contains nothing, erase it
            if (result.Count > 0 && result[result.Count - 1].Count == 0)
            {
                result.RemoveAt(result.Count - 1);
            }

            return result;
        }

        public override void MeasureBounds(Graphics g)
        {
            LinkRegions.Clear();

            base.MeasureBounds(g);
        }

        #endregion
    }
}