namespace FeaturedPostsRotator
{
  #region using

  using BlogEngine.Core.Web.Extensions;
  using System;
  using System.Data;
  using System.Text.RegularExpressions;
  using BlogEngine.Core;
  using BlogEngine.Core.Web.Controls;
  using System.Web;
  using System.IO;

  #endregion

  /// <summary>
  /// Add Featured Posts images in your slide show.
  /// </summary>
  [Extension("Add Featured Posts images in your slideshow", "3.0.0.1", "<a href=\"http://www.informarea.it\">Inform@rea</a>")]
  public class FeaturedPostsRotator
  {
    #region Private Members
    static protected ExtensionSettings _settings = null;
    static protected ExtensionSettings _featuredPostsImages = null;
    #endregion

    #region FeaturedPostsRotator
    public FeaturedPostsRotator()
    {
      InitSettings();
      InitFeaturedPostsImages();
    }
    #endregion

    #region Initialization
    #region InitSettings
    protected void InitSettings()
    {
      ExtensionSettings settings = new ExtensionSettings("FeaturedPostsRotator");

      settings.AddParameter("UseExtensionJQuery", "Use Extension JQuery file", 1, true, false, ParameterType.Boolean);

      settings.AddValue("UseExtensionJQuery", true);

      settings.IsScalar = true;

      // set page that extension manager will use  
      // instead of default settings page
      ExtensionManager.SetAdminPage("FeaturedPostsRotator", "~/Custom/Controls/FeaturedPostsRotator/Admin.aspx");

      ExtensionManager.ImportSettings(settings);
      _settings = ExtensionManager.GetSettings("FeaturedPostsRotator");
    }
    #endregion

    #region InitFeaturedPostsImages
    protected void InitFeaturedPostsImages()
    {
      ExtensionSettings featuredPostsImages = new ExtensionSettings("FeaturedPostsImages");

      featuredPostsImages.AddParameter("id", "id", 38, true, true, ParameterType.Integer);
      featuredPostsImages.AddParameter("PostID", "PostID", 38, true, false, ParameterType.DropDown);
      featuredPostsImages.AddParameter("PostTitle", "Post Title", 38, true, false, ParameterType.DropDown);
      featuredPostsImages.AddParameter("Image", "Image");
      featuredPostsImages.AddParameter("ImageSize", "Size", 100, false, false, ParameterType.String);

      ExtensionManager.SetAdminPage("FeaturedPostsRotator", "~/Custom/Controls/FeaturedPostsRotator/Admin.aspx");

      ExtensionManager.ImportSettings("FeaturedPostsRotator", featuredPostsImages);
      _featuredPostsImages = ExtensionManager.GetSettings("FeaturedPostsRotator", "FeaturedPostsImages");
    }
    #endregion
    #endregion
  }
}
