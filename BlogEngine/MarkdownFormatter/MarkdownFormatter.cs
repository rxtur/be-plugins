using System;
using System.Collections.Generic;
using System.Web;
using BlogEngine.Core;
using BlogEngine.Core.Web.Controls;
using MarkdownSharp;
using BlogEngine.Core.Web.Extensions;

/// <summary>
/// Summary description for ClassName
/// </summary>
[Extension("Uses MarkdownSharp to format posts as markdown.", "3.0.0.1", "Kyle Trauberman")]
public class MarkdownFormatter
{
	static protected Dictionary<Guid, ExtensionSettings> _options = new Dictionary<Guid, ExtensionSettings>();

	public MarkdownFormatter()
	{
		Post.Serving += Post_Serving;

		// load the options at startup by calling the getter
		var x = Options;
	}

	void Post_Serving(object sender, ServingEventArgs e)
	{
		Markdown md = new Markdown(new MarkdownOptions() { 
			AutoHyperlink = bool.Parse(Options.GetSingleValue("autoHyperlink")),
			AutoNewlines = bool.Parse(Options.GetSingleValue("autoNewlines")), 
			EmptyElementSuffix = Options.GetSingleValue("emptyElementSuffix"),
			EncodeProblemUrlCharacters = bool.Parse(Options.GetSingleValue("encodeProblemUrlCharacters")),
			LinkEmails = bool.Parse(Options.GetSingleValue("linkEmails")),
			StrictBoldItalic = bool.Parse(Options.GetSingleValue("strictBoldItalic"))
		});

		e.Body = md.Transform(e.Body);
	}

	private static readonly object lockObj = new object();

	private static ExtensionSettings Options
	{
		get
		{
			Guid blogId = Blog.CurrentInstance.Id;
			ExtensionSettings options = null;
			_options.TryGetValue(blogId, out options);

			if (options == null)
			{
				lock (lockObj)
				{
					_options.TryGetValue(blogId, out options);

					if (options == null)
					{
						// options
						options = new ExtensionSettings("Markdown");
						options.IsScalar = true;

						options.AddParameter("autoHyperlink", "Auto Hyperlink");
						options.AddParameter("autoNewlines", "Auto Newlines");
						options.AddParameter("emptyElementSuffix", "Empty Element Suffix");
						options.AddParameter("encodeProblemUrlCharacters", "Encode Problem Url Characters");
						options.AddParameter("linkEmails", "Link Emails");
						options.AddParameter("strictBoldItalic", "Strict Bold Italic");

						options.AddValue("autoHyperlink", false);
						options.AddValue("autoNewlines", false);
						options.AddValue("emptyElementSuffix", new string[] {">", "/>"}, "/>");
						options.AddValue("encodeProblemUrlCharacters", false);
						options.AddValue("linkEmails", false);
						options.AddValue("strictBoldItalic", false);

						_options[blogId] = ExtensionManager.InitSettings("MarkdownFormatterExtension", options);
					}
				}
			}

			return options;
		}
	}
}
