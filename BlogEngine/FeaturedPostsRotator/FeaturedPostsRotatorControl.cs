namespace App_Code.Controls
{
  #region using
  using BlogEngine.Core.Web.Extensions;
  using System;
  using System.Collections.Generic;
  using System.Web;
  using System.Web.UI;
  using System.Text;
  using System.Data;
  using System.IO;
  using BlogEngine.Core;
  using BlogEngine.Core.Providers;
  using System.Web.UI.HtmlControls;
  using Page = System.Web.UI.Page;
  #endregion

  /// <summary>
  /// Summary description for FeaturedPostsRotatorControl_Revisited
  /// </summary>
  public class FeaturedPostsRotatorControl : Control
  {
    static protected ManagedExtension _rotatorExtension = null;
    static protected ExtensionSettings _rotatorExtensionImages = null;
    static protected ExtensionSettings _rotatorSettings = null;
    static protected string _html = "Empty";
    private enum PageStyle { Page, Front, Tag }

    private PageStyle _pageLocation;
    private Page _page = null;

    #region Properties
    public bool RotatorEnabled
    {
      get
      {
        return _rotatorExtension.Enabled;
      }
    }
    #endregion

    static FeaturedPostsRotatorControl()
    {

    }

    #region AddFeaturedPostsRotator
    private void AddFeaturedPostsRotator()
    {
      string html = GetImagesHtmlList();

      if (html != "Empty")
      {
        HttpContext context = HttpContext.Current;
        if ((context.CurrentHandler is Page == false))
        {
          return;
        }
        Page page = (Page)context.CurrentHandler;

        AddStylesheetToPage(page);
       
      }

    }
    #endregion

    #region GetImagesHtmlList
    private static string GetImagesHtmlList()
    {
      DataTable dt = _rotatorExtensionImages.GetDataTable();
      if (dt.Rows.Count == 0)
      {
        return "Empty";
      }

      StringBuilder sb = new StringBuilder();

      sb.AppendLine("<div id=\"featured\">");
//	   id=\"show\"
      sb.AppendLine("<ul id=\"show\">");
      foreach (DataRow dr in _rotatorExtensionImages.GetDataTable().Rows)
      {
        Post post = Post.GetPost(new Guid(dr["PostID"].ToString()));
        if (post == null)
          continue;

        if (!post.IsVisibleToPublic)
          continue;

        string featuredImage = dr["Image"].ToString();

        sb.AppendLine("<li>");
        sb.AppendLine("<span class=\"top\"></span>");
        sb.AppendLine("<span class=\"left\"><a class=\"prev\" href=\"#\" data-slide=\"prev\">‹</a></span>");
        sb.AppendLine("<span class=\"right\"><a class=\"next\" href=\"#\" data-slide=\"next\">›</a></span>");
        sb.AppendLine("<span class=\"bottom\"></span>");
        sb.AppendLine("<div class=\"desc\">");
         sb.AppendFormat("<h1><a href=\"{0}\" title=\"{1}\">{1}</a></h1>", post.RelativeLink, HttpUtility.HtmlEncode(post.Title));
       sb.AppendLine("<small>");
        foreach (Category category in post.Categories)
        {
          sb.AppendFormat("<a class=\"accent\" href=\"{0}\">{1}</a>", category.RelativeLink, category.Title);
        }
       //DISQUS		
		
		sb.AppendFormat("<a class=\"accent\" href=\"{0}#disqus_thread\">{1}</a>", post.PermaLink, "Comments");
		sb.AppendFormat("<a class=\"accent\" href=\"#\">{0}</a>", post.DateCreated.ToString("MMM dd, yyyy"));		
        sb.AppendLine("</small>");
        sb.AppendLine("</div>");

        sb.AppendFormat("<a href=\"{0}\" title=\"{1}\"><img src=\"{2}\" alt=\"{1}\" height=\"260\" width=\"638\" /></a>", post.RelativeLink, HttpUtility.HtmlEncode(post.Title), featuredImage);

        sb.AppendLine("</li>");
      } 
	  
      sb.Append("</ul>"); 
	  
      sb.Append("</div>");
      return sb.ToString();
	  
	 	  
    }
    #endregion

    #region AddJavaScriptToPage
    private void AddJavaScriptToPage(Page page)
    {
      if (page != null)
      {
        string innerFadeScript = "InnerFadeScript";
        ClientScriptManager clientScriptManager = Page.ClientScript;
        Type csType = page.GetType();
        StringBuilder scripts = new StringBuilder();

        // register innerfade script plugin
        if (!clientScriptManager.IsClientScriptBlockRegistered(csType, innerFadeScript))
        {
          if (bool.Parse(_rotatorSettings.GetSingleValue("UseExtensionJQuery")))
            scripts.AppendLine("<script type=\"text/javascript\" src=\"http://ajax.googleapis.com/ajax/libs/jquery/1.4.2/jquery.min.js\"></script>");

          scripts.AppendFormat("<script type=\"text/javascript\" src=\"{0}Custom/Controls/FeaturedPostsRotator/js/jquery.innerfade.js\"></script>", Utils.AbsoluteWebRoot);		  
		  		  
          scripts.AppendLine("<script type=\"text/javascript\">");
          scripts.AppendLine("$(document).ready(function () {");
                
	   scripts.AppendLine("$('#show').innerFade({ indexContainer: '#index', currentItemContainer: '.current', totalItemsContainer: '.total', animationtype: 'fade', speed: 3000, timeout: 5000, type: 'sequence', prevLink: '.prev', nextLink: '.next', containerheight: 260 });");
 
  
		  scripts.AppendLine("});");
          scripts.AppendLine("</script>");

          page.ClientScript.RegisterStartupScript(csType, innerFadeScript, scripts.ToString());
        }
      }
    }
    #endregion

    #region AddStylesheetToPage
    private static void AddStylesheetToPage(Page page)
    {
      if (page != null)
      {
        HtmlLink css = new HtmlLink();
        css.Attributes["type"] = "text/css";
        css.Attributes["rel"] = "stylesheet";
        css.Attributes["href"] = String.Format("{0}Custom/Controls/FeaturedPostsRotator/style.css", Utils.RelativeWebRoot);

        HtmlHead header = page.Header;
        ControlCollection controls = header.Controls;
        controls.Add(css);
      }
    }
    #endregion

    #region GetPageLocation
    private static PageStyle GetPageLocation()
    {
      string path = HttpContext.Current.Request.RawUrl.ToLower();

      if (path.Contains("author/"))
        path = "author.aspx";
      else if (path.LastIndexOf('/') > -1)
        path = path.Substring(path.LastIndexOf('/') + 1);

      string queryTag = HttpContext.Current.Request.QueryString["tag"];

      if (path.StartsWith("default.aspx") || path.StartsWith("blog.aspx") || path.StartsWith("author.aspx") || path == "" || !String.IsNullOrEmpty(queryTag))
        return PageStyle.Front;
      else
        return PageStyle.Page;
    }
    #endregion

    #region Overridden Methods
    #region OnInit
    protected override void OnInit(EventArgs e)
    {
      base.OnInit(e);

      _pageLocation = GetPageLocation();

      if (_pageLocation == PageStyle.Front)
      {
        _rotatorExtension = ExtensionManager.GetExtension("FeaturedPostsRotator");
        _rotatorExtensionImages = ExtensionManager.GetSettings("FeaturedPostsRotator", "FeaturedPostsImages");
        _rotatorSettings = ExtensionManager.GetSettings("FeaturedPostsRotator");
        _html = GetImagesHtmlList();

        HttpContext context = HttpContext.Current;
        if (context.CurrentHandler is Page == false)
        {
          return;
        }
        _page = (Page)context.CurrentHandler;

        AddStylesheetToPage(_page);
      }
    }
    #endregion

    #region Render
    protected override void Render(HtmlTextWriter writer)
    {
      if (RotatorEnabled && _pageLocation == PageStyle.Front)
      {
        RenderControl(writer);
      }
    }
    #endregion

    #region RenderControl
    public override void RenderControl(HtmlTextWriter output)
    {
      if (_html != "Empty" && _pageLocation == PageStyle.Front)
      {
        AddJavaScriptToPage(_page);

        output.Write(_html);
      }
    }
    #endregion
    #endregion
  }
}
