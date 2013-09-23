using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Umbraco.Core.Macros
{
	/// <summary>
	/// Parses the macro syntax in a string and renders out it's contents
	/// </summary>
	internal class MacroTagParser
	{
        private static readonly Regex MacroRteContent = new Regex(@"(<div class=[""']umb-macro-holder[""'].*?>.*?<!--\s*?)(<\?UMBRACO_MACRO.*?/>)(.*?</div>)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private static readonly Regex MacroPersistedFormat = new Regex(@"<\?UMBRACO_MACRO macroAlias=[""'](\w+?)[""'].+?/>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

	    /// <summary>
	    /// This formats the persisted string to something useful for the rte so that the macro renders properly since we 
	    /// persist all macro formats like {?UMBRACO_MACRO macroAlias=\"myMacro\" /}
	    /// </summary>
	    /// <param name="persistedContent"></param>
	    /// <param name="htmlAttributes">The html attributes to be added to the div</param>
	    /// <returns></returns>
	    /// <remarks>
	    /// This converts the persisted macro format to this:
	    /// 
	    ///     {div class='umb-macro-holder'}
	    ///         <!-- <?UMBRACO_MACRO macroAlias=\"myMacro\" /> -->
	    ///         This could be some macro content
	    ///     {/div}
	    /// 
	    /// </remarks>
	    internal static string FormatRichTextPersistedDataForEditor(string persistedContent, IDictionary<string ,string> htmlAttributes)
        {
            return MacroPersistedFormat.Replace(persistedContent, match =>
            {
                if (match.Groups.Count >= 2)
                {
                    //<div class="umb-macro-holder" data-load-content="false">
                    var sb = new StringBuilder("<div class=\"umb-macro-holder\"");
                    foreach (var htmlAttribute in htmlAttributes)
                    {
                        sb.Append(" ");
                        sb.Append(htmlAttribute.Key);
                        sb.Append("=\"");
                        sb.Append(htmlAttribute.Value);
                        sb.Append("\"");
                    }
                    sb.AppendLine(">");
                    sb.Append("<!-- ");
                    sb.Append(match.Groups[0].Value);
                    sb.AppendLine(" -->");
                    sb.Append("Macro alias: ");
                    sb.Append("<strong>");
                    sb.Append(match.Groups[1].Value);
                    sb.Append("</strong></div>");
                    return sb.ToString();
                }
                //replace with nothing if we couldn't find the syntax for whatever reason
                return "";
            });
        }

        /// <summary>
        /// This formats the string content posted from a rich text editor that contains macro contents to be persisted.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// 
        /// This is required because when editors are using the rte, the html that is contained in the editor might actually be displaying 
        /// the entire macro content, when the data is submitted the editor will clear most of this data out but we'll still need to parse it properly
        /// and ensure the correct sytnax is persisted to the db.
        /// 
        /// When a macro is inserted into the rte editor, the html will be:
        /// 
        ///     {div class='umb-macro-holder'}
        ///         <!-- <?UMBRACO_MACRO macroAlias=\"myMacro\" /> -->
        ///         This could be some macro content
        ///     {/div}
        /// 
        /// What this method will do is remove the {div} and parse out the commented special macro syntax: {?UMBRACO_MACRO macroAlias=\"myMacro\" /}
        /// since this is exactly how we need to persist it to the db.
        /// 
        /// </remarks>
        internal static string FormatRichTextContentForPersistence(string rteContent)
        {
            return MacroRteContent.Replace(rteContent, match =>
                {
                    if (match.Groups.Count >= 3)
                    {
                        //get the 3rd group which is the macro syntax
                        return match.Groups[2].Value;
                    }
                    //replace with nothing if we couldn't find the syntax for whatever reason
                    return "";
                });
        }

		/// <summary>
		/// This will accept a text block and seach/parse it for macro markup.
		/// When either a text block or a a macro is found, it will call the callback method.
		/// </summary>
		/// <param name="text"> </param>
		/// <param name="textFoundCallback"></param>
		/// <param name="macroFoundCallback"></param>
		/// <returns></returns>
		/// <remarks>
		/// This method  simply parses the macro contents, it does not create a string or result, 
		/// this is up to the developer calling this method to implement this with the callbacks.
		/// </remarks>
		internal static void ParseMacros(
			string text,
			Action<string> textFoundCallback, 
			Action<string, Dictionary<string, string>> macroFoundCallback )
		{
			if (textFoundCallback == null) throw new ArgumentNullException("textFoundCallback");
			if (macroFoundCallback == null) throw new ArgumentNullException("macroFoundCallback");

			string elementText = text;

			var fieldResult = new StringBuilder(elementText);

			//NOTE: This is legacy code, this is definitely not the correct way to do a while loop! :)
			var stop = false;
			while (!stop)
			{
				var tagIndex = fieldResult.ToString().ToLower().IndexOf("<?umbraco");
				if (tagIndex < 0)
					tagIndex = fieldResult.ToString().ToLower().IndexOf("<umbraco:macro");
				if (tagIndex > -1)
				{
					var tempElementContent = "";

					//text block found, call the call back method
					textFoundCallback(fieldResult.ToString().Substring(0, tagIndex));

					fieldResult.Remove(0, tagIndex);

					var tag = fieldResult.ToString().Substring(0, fieldResult.ToString().IndexOf(">") + 1);
					var attributes = XmlHelper.GetAttributesFromElement(tag);

					// Check whether it's a single tag (<?.../>) or a tag with children (<?..>...</?...>)
					if (tag.Substring(tag.Length - 2, 1) != "/" && tag.IndexOf(" ") > -1)
					{
						String closingTag = "</" + (tag.Substring(1, tag.IndexOf(" ") - 1)) + ">";
						// Tag with children are only used when a macro is inserted by the umbraco-editor, in the
						// following format: "<?UMBRACO_MACRO ...><IMG SRC="..."..></?UMBRACO_MACRO>", so we
						// need to delete extra information inserted which is the image-tag and the closing
						// umbraco_macro tag
						if (fieldResult.ToString().IndexOf(closingTag) > -1)
						{
							fieldResult.Remove(0, fieldResult.ToString().IndexOf(closingTag));
						}
					}

				    var macroAlias = attributes.ContainsKey("macroalias") ? attributes["macroalias"] : attributes["alias"];

					//call the callback now that we have the macro parsed
					macroFoundCallback(macroAlias, attributes);

					fieldResult.Remove(0, fieldResult.ToString().IndexOf(">") + 1);
					fieldResult.Insert(0, tempElementContent);
				}
				else
				{
					//text block found, call the call back method
					textFoundCallback(fieldResult.ToString());

					stop = true; //break;
				}
			}
		}
	}
}