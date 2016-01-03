using System;
using System.Linq;
using System.Text;
using System.Web;
using BlogEngine.Core;
using BlogEngine.Core.Web.Controls;
using BlogEngine.Core.Web.Extensions;

/// <summary>
/// Adds an Heyos advertisement in your posts where the [heyos] keyword is found in the body.
/// </summary>
[Extension(
	"Adds an Heyos advertisement in your posts where the [heyos] keyword is found in the body",
    "3.0.0.1",
	"<a href=\"http://www.Informarea.it/\">By Fabrizio Cannatelli</a>",
    100)]
public class Heyos_Control
{

	static protected ExtensionSettings _extensionSettings;
	private const string ExtensionName = "Heyos_Control";

    /// <summary>
	/// Initializes static members of the <see cref="HeyosControl"/> class. 
    ///     Hooks up an event handler to the Post.Serving event.
    /// </summary>
	static Heyos_Control()
    {
        InitializeSettings();
        Post.Serving += PostServing;
    }

    /// <summary>
    /// Handles the Serving event of the Post control.
    /// Handles the Post.Serving event to take care of the [heyos] keyword.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="BlogEngine.Core.ServingEventArgs"/> instance containing the event data.</param>
    private static void PostServing(object sender, ServingEventArgs e)
    {
        if (!e.Body.Contains("[heyos]"))
        {
            return;
        }

        switch (e.Location)
        {
            case ServingLocation.PostList:
                e.Body = e.Body.Replace("[heyos]", string.Empty);
                break;
            case ServingLocation.SinglePost:
                PrepareFullPost(e);
                break;
            case ServingLocation.Feed:
                e.Body = e.Body.Replace("[heyos]", string.Empty);
                break;
        }
    }

    /// <summary>
    /// Replaces the [heyos] string on the full post page.
    /// </summary>
    /// <param name="e">
    /// The event arguments.
    /// </param>
    private static void PrepareFullPost(ServingEventArgs e)
    {
        var request = HttpContext.Current.Request;
        AdSettings settings = new AdSettings();

        StringBuilder syntax = new StringBuilder();
        syntax.AppendFormat(@"<div class='HeyosControl'><script type='text/javascript'><!--
		heyos_ad_user = '{0}';
		heyos_ad_type = '{1}';
		heyos_ad_format = '{2}';
		heyos_color_border = '{3}';
		heyos_color_bg = '{4}';
		heyos_color_link = '{5}';
		heyos_color_text = '{6}';
		heyos_color_url = '{7}';
		
		//-->
		</script>


<script type='text/javascript'
src='http://admaster.heyos.com/core/bnr.js'>
</script></div>", settings.user, settings.type, settings.format, settings.border, settings.bg, settings.link, settings.text, settings.url);

        e.Body = request.UrlReferrer == null || request.UrlReferrer.Host != request.Url.Host
					 ? e.Body.Replace("[heyos]", string.Empty)
					 : e.Body.Replace("<p>[heyos]</p>", syntax.ToString());
    }

    private static void InitializeSettings()
    {
        var settings = new ExtensionSettings(ExtensionName);
        settings.Help = @"<p>Adds an heyos advertisement in your posts where the [heyos] keyword is found in the body</p>
<p>Place the [heyos] keyword on a line by itself in your blog. BlogEngine will automatically wrap the line with paragraph tags which will be replaced with your Heys code.</p>
<ol>
	<li><b>User</b>: Enter the entire string for heyos_ad_user</li>
	<li><b>type</b>: Enter the entire string for heyos_ad_type</li>
	<li><b>format</b>: Enter the entire string for heyos_ad_format</li>  
	<li><b>border</b>: Enter the entire string for heyos_color_border</li>
	<li><b>bg</b>: Enter the entire string for heyos_color_bg</li>
	<li><b>link</b>: Enter the entire string for heyos_color_link</li>
	<li><b>text</b>: Enter the entire string for heyos_color_text</li>  
	<li><b>url</b>: Enter the entire string for heyos_color_url</li>
</ol>
<p>
	<a href='http://www.Informarea.it'>By Fabrizio Cannatelli</a>
</p>";

        settings.IsScalar = true;

        settings.AddParameter("user", "user");
        settings.AddValue("user", string.Empty);
        settings.AddParameter("type", "type");
        settings.AddValue("type", string.Empty);	
		settings.AddParameter("format", "format");
        settings.AddValue("format", string.Empty);	
		settings.AddParameter("border", "border");
        settings.AddValue("border", string.Empty);		
        settings.AddParameter("bg", "bg");
        settings.AddValue("bg", string.Empty);		
        settings.AddParameter("link", "link");
        settings.AddValue("link", string.Empty);		
        settings.AddParameter("text", "text");
        settings.AddValue("text", string.Empty);		
		settings.AddParameter("url", "url");
        settings.AddValue("url", string.Empty);
		

        _extensionSettings = ExtensionManager.InitSettings(ExtensionName, settings);
    }

    class AdSettings
    {
        public string user { get; set; }
        public string type { get; set; }
        public string format { get; set; }
		public string border { get; set; }
        public string bg { get; set; }		
		public string link { get; set; }
		public string text { get; set; }
		public string url { get; set; }

        public AdSettings()
        {
            this.user = _extensionSettings.GetSingleValue("user");
            this.type = _extensionSettings.GetSingleValue("type");
            this.format = _extensionSettings.GetSingleValue("format");
            this.border = _extensionSettings.GetSingleValue("border");			
			this.bg = _extensionSettings.GetSingleValue("bg");
            this.link = _extensionSettings.GetSingleValue("link");
            this.text = _extensionSettings.GetSingleValue("text");
            this.url = _extensionSettings.GetSingleValue("url");
        }
    }
}
