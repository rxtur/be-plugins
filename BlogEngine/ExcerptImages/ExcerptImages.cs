using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;
using System.Web.UI.HtmlControls;
using BlogEngine.Core;
using BlogEngine.Core.Web.Controls;
using BlogEngine.Core.Web.Extensions;
// Author:      John Thomas, John's PC Repair (http://www.johnpcrepair.com)
/// <summary>
/// Automatically places the first image from a post into the post list excerpt or
/// another picture if specified
/// </summary>
[Extension("Automatically places the first image from a post into the post list excerpt. Requires BlogParser 1.1", "1.0.0.0", "John Thomas")]
public class ExcerptImages : BlogParser.ISubscriber
{
    #region Constants

    const string _ExtensionName = "ExcerptImages";
    const string _ExtensionAdminPath = "~/admin/Extensions/ExcerptImages/ExcerptImagesAdmin.aspx";

    public const string _BlogImageFolder = "App_Data/files/";
    public const string _SmallFileNameAddon = "_pliSmall";
    
    const string _UrlRewriteDirectory = "IMAGES/";
    const string _UrlRewriteExtension = ".JPGX";
    const string _ImageServer = "image.axd";
    const string _ImageServerPicturePathVar = "picture";

    #endregion // Constants

    #region Fields

    static readonly object _Sync = new object();
    static protected Dictionary<Guid, ExtensionSettings> _BlogSettings = new Dictionary<Guid, ExtensionSettings>();
    static ExcerptImages _Instance; // Required for BlogParser
    static ImageSize _ImageSize;
    static string _Units = "px";

    // Admin changeable settings
    static int _MaxWidth = 125;
    static int _MaxHeight = 125;
    static bool _ForceMaxWidth = true;
    static bool _ForceMaxHeight = true;
    static bool _InsertIntoContent = true;
    static string _ImageInlineStyle = "float:left; margin:0.5em;";
    static string _ImageCssClass = String.Empty;
    static bool _UseDefaultImage = false;
    static string _DefaultImagePath = String.Empty;

    #endregion // Fields

    #region Public Properties

    // ----------------------------------------------------------------------
    /// <summary>
    /// Gets the settings help for the ExcerptImages extension
    /// </summary>
    public static string Help
    {
        get
        {
            return "<a href=\"" + Path.Combine(Utils.RelativeWebRoot, _ExtensionAdminPath.Replace("~/", String.Empty)) + "\">Go to Detailed Admin Page.</a>";
        }
    }

    /// <summary>
    /// Gets the settings for this extension
    /// </summary>
    public static ExtensionSettings Settings
    {
        get
        {
            Guid id = Blog.CurrentInstance.Id;
            if (!_BlogSettings.ContainsKey(id))
            {
                lock (_Sync)
                {
                    if (!_BlogSettings.ContainsKey(id))
                        _BlogSettings[id] = LoadSettings();
                }
            }

            return _BlogSettings[id];
        }
    }

    #endregion // Public Properties

    #region Private Methods

    // ----------------------------------------------------------------------
    /// <summary>
    /// Updates settings variables for this extension
    /// </summary>
    private static void UpdateSettings()
    {
        try
        {
            _MaxWidth = Convert.ToInt32(Settings.GetSingleValue("MaxWidth"));
            _MaxHeight = Convert.ToInt32(Settings.GetSingleValue("MaxHeight"));
            _ForceMaxWidth = Convert.ToBoolean(Settings.GetSingleValue("ForceMaxWidth"));
            _ForceMaxHeight = Convert.ToBoolean(Settings.GetSingleValue("ForceMaxHeight"));
            _ImageSize = new ImageSize(_MaxWidth, _MaxHeight, _Units);
            _InsertIntoContent = Convert.ToBoolean(Settings.GetSingleValue("InsertIntoContent"));
            _ImageInlineStyle = Settings.GetSingleValue("ImageInlineStyle");
            _ImageCssClass = Settings.GetSingleValue("ImageCssClass");
            _UseDefaultImage = Convert.ToBoolean(Settings.GetSingleValue("UseDefaultImage"));
            _DefaultImagePath = Settings.GetSingleValue("DefaultImagePath");
        }
        catch { }
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// Loads the settings for this extension
    /// </summary>
    private static ExtensionSettings LoadSettings()
    {
        ExtensionSettings initialSettings = new ExtensionSettings(_ExtensionName);
        initialSettings.Help = @"";
        initialSettings.IsScalar = true;
        ExtensionSettings settings = null;

        initialSettings.AddParameter("MaxWidth");
        initialSettings.AddValue("MaxWidth", _MaxWidth);

        initialSettings.AddParameter("ForceMaxWidth");
        initialSettings.AddValue("ForceMaxWidth", _ForceMaxWidth);

        initialSettings.AddParameter("MaxHeight");
        initialSettings.AddValue("MaxHeight", _MaxHeight);

        initialSettings.AddParameter("ForceMaxHeight");
        initialSettings.AddValue("ForceMaxHeight", _ForceMaxHeight);

        initialSettings.AddParameter("InsertIntoContent");
        initialSettings.AddValue("InsertIntoContent", _InsertIntoContent);

        initialSettings.AddParameter("ImageInlineStyle");
        initialSettings.AddValue("ImageInlineStyle", _ImageInlineStyle);

        initialSettings.AddParameter("ImageCssClass");
        initialSettings.AddValue("ImageCssClass", _ImageCssClass);

        initialSettings.AddParameter("UseDefaultImage");
        initialSettings.AddValue("UseDefaultImage", _UseDefaultImage);

        initialSettings.AddParameter("DefaultImagePath");
        initialSettings.AddValue("DefaultImagePath", _DefaultImagePath);

        settings = ExtensionManager.InitSettings(_ExtensionName, initialSettings);
        ExtensionManager.SetStatus(_ExtensionName, false);

        return settings;
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// Builds an html string showing an error
    /// </summary>
    /// <param name="exc">The exception to report</param>
    /// <param name="message">A custom message</param>
    /// <returns>Html string</returns>
    private static string ShowError(Exception exc, string message)
    {
        if (Security.IsAuthenticated)
        {
            string r =
                "<div class=\"" + _ExtensionName + "Error\">There was an error in the PostListImages Extension:<br>" +
                "<small>Extension Message: " + message + "<br><br>";

            if (exc != null)
            {
                r += "<strong>Exception</strong>:<br>" + exc.Message.Replace("\r", "<br><br>") +
                                       "<br><br><strong>Stack Trace:</strong><br>" + exc.StackTrace.Replace("\r", "<br><br>");
            }
            r += "</small></div><br><br>";
            return r;
        }
        return String.Empty;
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// Gets the thumbnail image file path and creates the 
    /// thumbnail if it does not already exist.
    /// </summary>
    /// <param name="bigFile">The path of the original file (from the img tag)</param>
    /// <param name="width">The original image width</param>
    /// <param name="height">The original image height</param>
    /// <returns>The path of the thumbnail</returns>
    private static string GetSmallFile(string bigFile, int width, int height)
    {
        SmallImageUri imageUri = new SmallImageUri(bigFile, _SmallFileNameAddon, _BlogImageFolder);

        if (!imageUri.FoundSmallFile) return null;

        if (!File.Exists(imageUri.SmallFileHostPath)) // See if the small file already exists
        {
            try
            {
                // Create small file
                using (FileStream bigFileStream = new FileStream(imageUri.FileHostPath, FileMode.Open)) // Open the big file
                {
                    using (FileStream smallFileStream = new FileStream(imageUri.SmallFileHostPath, FileMode.Create)) // Create and open the small file for writing
                    {
                        ResizeImage(bigFileStream, smallFileStream, width, height);
                    }
                }
            }
            catch (FileNotFoundException err)
            {
                return "\0" + ShowError(err, "FileNotFound:GetSmallFile:" + imageUri.FileHostPath);
            }
            catch (DirectoryNotFoundException err)
            {
                return "\0" + ShowError(err, "DirectoryNotFound:GetSmallFile:" + imageUri.FileHostPath);
            }
            catch (PathTooLongException err)
            {
                return "\0" + ShowError(err, "PathToLong:GetSmallFile:" + imageUri.FileHostPath + ", " + imageUri.SmallFileHostPath);
            }
        }

        return imageUri.SmallFileName;
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// Gets the ImageSize display size based on the extension settings
    /// </summary>
    /// <param name="origWidth">The original width of the image</param>
    /// <param name="origHeight">The original height of the image</param>
    /// <returns>ImageSize object</returns>
    private static ImageSize GetDisplaySize(float origWidth, float origHeight)
    {
        float ratio;
        float destWidth = _MaxWidth;
        float destHeight = _MaxHeight;
        float goalWidth = origWidth;
        float goalHeight = origHeight;

        if (!_ForceMaxWidth && (!_ForceMaxHeight))
        {
            if (destWidth < origWidth || destHeight < origHeight)
            {
                if (origHeight > origWidth)
                {
                    ratio = origWidth / origHeight;
                    if (destHeight * ratio > destWidth)
                    {
                        ratio = origHeight / origWidth;
                        goalWidth = destWidth;
                        goalHeight = Convert.ToInt32(goalWidth * ratio);
                    }
                    else
                    {
                        goalHeight = destHeight;
                        goalWidth = Convert.ToInt32(goalHeight * ratio);
                    }
                }
                else
                {
                    ratio = origHeight / origWidth;
                    if (destWidth * ratio > destHeight)
                    {
                        ratio = origWidth / origHeight;
                        goalHeight = destHeight;
                        goalWidth = Convert.ToInt32(goalHeight * ratio);
                    }
                    else
                    {
                        goalWidth = destWidth;
                        goalHeight = Convert.ToInt32(goalWidth * ratio);
                    }
                }
            }
            else
            {
                goalWidth = origWidth;
                goalHeight = origHeight;
            }
        }
        else if (_ForceMaxWidth && _ForceMaxHeight)
        {
            ratio = origHeight / origWidth;
            goalWidth = _MaxWidth;
            goalHeight = goalWidth * ratio;
            if (goalHeight < _MaxHeight)
            {
                ratio = origWidth / origHeight;
                goalHeight = _MaxHeight;
                goalWidth = goalHeight * ratio;
            }
        }
        else
        {
            if (_ForceMaxWidth)
            {
                ratio = origHeight / origWidth;
                goalWidth = _MaxWidth;
                goalHeight = goalWidth * ratio;
            }
            else
            {
                ratio = origWidth / origHeight;
                goalHeight = _MaxHeight;
                goalWidth = goalHeight * ratio;

            }
        }
        return new ImageSize(Convert.ToInt32(goalWidth), Convert.ToInt32(goalHeight), _Units);
    }

    #endregion Private Methods

    #region Public Methods

    // ----------------------------------------------------------------------
    /// <summary>
    /// Gets the image to use for a post list item
    /// </summary>
    /// <param name="postId">The id of the post</param>
    /// <returns>Html string for the image.</returns>
    public static string GetPostImage(Guid postId)
    {
        return GetPostImage(postId, String.Empty);
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// Gets the image to use for a post list item
    /// </summary>
    /// <param name="postId">The Id of the post</param>
    /// <param name="style">Inline css to add</param>
    /// <returns>Html string for the image.</returns>
    public static string GetPostImage(Guid postId, string style)
    {
        if (!ExtensionManager.ExtensionEnabled(_ExtensionName)) return String.Empty;
        Post post = Post.GetPost(postId);
        
        if (post != null)
        {
            int tagIndex = 0;
            List<BlogParser.HtmlTag> imgTags = BlogParser.HtmlDom.GetTagByTagName(
                                                BlogParser.ParseForDoms(post.Content),
                                                "img");

            // See if a different image has been specified to be used
            for (int i = 0; i < imgTags.Count; i++)
            {
                if (String.Compare(imgTags[i]["postlist"].Value, "true", true) == 0)
                {
                    tagIndex = i;
                    break;
                }
            }

            if (imgTags.Count > 0) // Images Found
            {
                // Add or replace CssClass specified in settings
                if (!String.IsNullOrEmpty(_ImageCssClass))
                    imgTags[tagIndex].Insert(0, new BlogParser.HtmlAttribute("class", _ImageCssClass));

                // Get filename for the new image to be created
                SmallImageUri imageUri = new SmallImageUri(imgTags[tagIndex]["src"].Value, _SmallFileNameAddon, _BlogImageFolder);
                if (!imageUri.FoundSmallFile) return String.Empty;


                ImageSize newSize;

                // Create Post list image if it does not exist
                if (!_ForceMaxHeight || !_ForceMaxWidth)
                {
                    // Get display size

                    try
                    {
                        using (FileStream imageStream = new FileStream(imageUri.FileHostPath, FileMode.Open, FileAccess.Read, FileShare.Read)) // Open the big file
                        {
                            using (Image image = Image.FromStream(imageStream))
                            {
                                newSize = GetDisplaySize(image.Width, image.Height);

                            }

                        }
                    }
                    catch (DirectoryNotFoundException err)
                    {
                        return ShowError(err, "DirectoryNotFound: " + imageUri.FileHostPath);
                    }
                    catch (FileNotFoundException err)
                    {
                        return ShowError(err, "FileNotFound: " + imageUri.FileHostPath);
                    }
                    catch (PathTooLongException err)
                    {
                        return ShowError(err, "PathTooLong: " + imageUri.FileHostPath);
                    }
                    catch (IOException err)
                    {
                        return ShowError(err, "IOException: " + imageUri.FileHostPath);
                    }
                }
                else // No need to get a display size
                {
                    newSize = new ImageSize(_MaxWidth, _MaxHeight, "px");
                }


                    // Create Image to display
                    string smallFile = GetSmallFile(imgTags[tagIndex]["src"].Value, Convert.ToInt32(newSize.Width.Replace(_Units, String.Empty)),
                            Convert.ToInt32(newSize.Height.Replace(_Units, String.Empty)));

                    if (Security.IsAuthenticated && !String.IsNullOrEmpty(smallFile) && smallFile[0] == '\0') // See if error from GetSmallFile and send back
                    {
                        string r = smallFile.Replace("\0", String.Empty);
                        return "\">" + r;
                    }
                

                imgTags[tagIndex]["src"].Value = smallFile;

                // Add root if there is none
                if (String.IsNullOrEmpty(Path.GetDirectoryName(imgTags[tagIndex]["src"].Value)))
                    imgTags[tagIndex]["src"].Value = Blog.CurrentInstance.AbsoluteWebRoot + imgTags[tagIndex]["src"].Value;

                // Prevent the browser from caching so that changes made by an authenticated user will be displayed
                if (Security.IsAuthenticated)
                {
                    if (imgTags[tagIndex]["src"].Value.Contains("?"))
                        imgTags[tagIndex]["src"].Value += "&nocache=" + Guid.NewGuid().ToString().Replace("-", String.Empty);
                    else
                        imgTags[tagIndex]["src"].Value += "?nocache=" + Guid.NewGuid().ToString().Replace("-", String.Empty);
                }

                if (_ForceMaxWidth && _ForceMaxHeight)
                {
                    if (newSize.GetWidth() > _MaxWidth)
                        newSize.Width = _MaxWidth + "px";
                    if (newSize.GetHeight() > _MaxHeight)
                        newSize.Height = _MaxHeight + "px";
                }

                imgTags[tagIndex]["width"].Value = newSize.Width;
                imgTags[tagIndex]["height"].Value = newSize.Height;
                imgTags[tagIndex]["alt"].Value = post.Title;
                imgTags[tagIndex]["title"].Value = post.Title;

                BlogParser.HtmlStyleAttribute styleAttr = new BlogParser.HtmlStyleAttribute();
                styleAttr.AddRange(style); // Add single instance specified style
                styleAttr.AddRange(_ImageInlineStyle); // Add global specified style
                styleAttr.Add("width", newSize.Width, false);
                styleAttr.Add("height", newSize.Height, false);

                imgTags[tagIndex]["style"].Value = styleAttr.Value;

                BlogParser.HtmlTag alink = new BlogParser.HtmlTag("a",
                                            new BlogParser.HtmlAttribute("href", HttpUtility.UrlDecode(post.AbsoluteLink.AbsolutePath))
                                            );

                alink.InnerHtml.Add(imgTags[tagIndex].Clone() as BlogParser.HtmlTag);
                return alink.ToHtml();
            }
            else // No images in post
            {
                if (_UseDefaultImage)
                {
                    BlogParser.HtmlTag alink = new BlogParser.HtmlTag("a",
                                            new BlogParser.HtmlAttribute("href", HttpUtility.UrlDecode(post.AbsoluteLink.AbsolutePath)));
                    BlogParser.HtmlStyleAttribute styleAttr = new BlogParser.HtmlStyleAttribute();
                    styleAttr.AddRange(style); // Add single instance specified style
                    styleAttr.AddRange(_ImageInlineStyle); // Add global specified style
                    BlogParser.HtmlTag defaultTag = new BlogParser.HtmlTag("img",
                                                            new BlogParser.HtmlAttribute("src", _DefaultImagePath),
                                                            new BlogParser.HtmlAttribute("alt", post.Title),
                                                            new BlogParser.HtmlAttribute("title", post.Title),
                                                            new BlogParser.HtmlAttribute("class", _ImageCssClass),
                                                            styleAttr);
                    alink.InnerHtml.Add(defaultTag);
                    return alink.ToString();

                }
            }
        }
        return String.Empty;
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// Resizes an image to the specified size
    /// </summary>
    /// <param name="fromStream">Stream from the original file</param>
    /// <param name="toStream">Stream to write the resized image</param>
    /// <param name="newWidth">The width of the new image</param>
    /// <param name="newHeight">The height of the new image</param>
    public static void ResizeImage(Stream fromStream, Stream toStream, int newWidth, int newHeight)
    {
        int width = newWidth;
        int height = newHeight;
        if (width > _MaxWidth)
            width = _MaxWidth;
        if (height > _MaxHeight)
            height = _MaxHeight;

        using (Image image = Image.FromStream(fromStream))
        {
            using (Bitmap thumbnailBitmap = new Bitmap(newWidth, newHeight))
            {
                using (Graphics g = Graphics.FromImage(thumbnailBitmap))
                {

                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    Rectangle imageRectangle = new Rectangle(0, 0, newWidth, newHeight);
                    g.DrawImage(image, imageRectangle);
                    if (width == newWidth && height == newHeight)
                    {
                        thumbnailBitmap.Save(toStream, image.RawFormat);
                        return;
                    }
                }

                // image does not fit within bounds and must be cropped
                using (Bitmap croppedBitmap = new Bitmap(width, height))
                {
                    using (Graphics g = Graphics.FromImage(croppedBitmap))
                    {

                        g.CompositingQuality = CompositingQuality.HighQuality;
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                        int x = 0;
                        int y = 0;
                        if (width != newWidth)
                            x = (newWidth - width) / 2;
                        if (height != newHeight)
                            y = (newHeight - height) / 2;
                        Rectangle srcRectangle = new Rectangle(x, y, width, height);
                        Rectangle dstRectangle = new Rectangle(0, 0, width, height);
                        g.DrawImage(thumbnailBitmap, dstRectangle, srcRectangle, GraphicsUnit.Pixel);
                        croppedBitmap.Save(toStream, image.RawFormat);
                    }

                }
            }
        }
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// BlogParser.ISubscriber Interface implementation
    /// (Not Implemented)
    /// </summary>
    public void TagFound(BlogParser.HtmlTagArgs e)
    {
        throw new NotImplementedException();
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// BlogParser.ISubscriber Interface implementation
    /// </summary>
    public void DomReady(BlogParser.HtmlDomArgs e)
    {
        if (!ExtensionManager.ExtensionEnabled(_ExtensionName))
            return;

        if (e == null) return;
        if (e.Dom == null) return;
        if (e.Location != ServingLocation.PostList) return;

        bool tagOrCategory = e.ContentBy == ServingContentBy.Category || e.ContentBy == ServingContentBy.Tag;

        if (BlogSettings.Instance.ShowDescriptionInPostList ||
            (BlogSettings.Instance.ShowDescriptionInPostListForPostsByTagOrCategory && tagOrCategory))
        {

            UpdateSettings();
            if (!_InsertIntoContent) return;

            string imgString = GetPostImage(e.Post.Id);
            e.Dom.Insert(0, imgString);
        }
    }

    #endregion // Public Methods

    #region Constructor

    /// <summary>
    /// Constructor
    /// </summary>
    static ExcerptImages()
    {
        // Register with BlogParser
        _Instance = new ExcerptImages();
        BlogParser.RegisterSubscriber(_ExtensionName, _Instance);
        BlogParser.AddSubscriberSetting(_ExtensionName, "SubscribeToDom", "PostList");
        ExtensionManager.SetAdminPage(_ExtensionName, _ExtensionAdminPath);
        // Initialize Settings
        var s = Settings;
    }

    #endregion // Constructor

    #region Classes

    // ----------------------------------------------------------------------
    /// <summary>
    /// Supplies image paths tailored for creating
    /// small images in BlogEngine.Net
    /// </summary>
    public class SmallImageUri
    {
        #region Fields

        string _RelativePath;
        string _BigFileName;
        string _RequestedFileName;
        string _SmallFileIndicator;
        bool _FoundSmallFile = false;
        bool _BigIsSmall = false;
        ImagePathType _PathType;

        #endregion // Fields

        #region Public Properties
        // ----------------------------------------------------------------------
        /// <summary>
        /// The original filename supplied in the constructor
        /// </summary>
        public string FileName
        {
            get
            {
                return _BigFileName;
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Gets the files server path
        /// </summary>
        public string FileHostPath
        {
            get
            {
                if (_FoundSmallFile && !String.IsNullOrEmpty(_RelativePath))
                {
                    string file = Path.Combine(Path.GetDirectoryName(_RequestedFileName), Path.GetFileName(_RequestedFileName));
                    return Path.Combine(HostingEnvironment.MapPath("~/"), _RelativePath, file);
                }
                return null;
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// The name of the small version of
        /// the filename supplied in the constructor
        /// </summary>
        public string SmallFileName
        {
            get
            {
                if (_FoundSmallFile)
                {
                    string file = Path.Combine(Path.GetDirectoryName(_RequestedFileName), Path.GetFileNameWithoutExtension(_RequestedFileName) + _SmallFileIndicator + Path.GetExtension(_RequestedFileName));
                    return String.Concat(Utils.RelativeWebRoot, _ImageServer, '?', _ImageServerPicturePathVar, '=', HttpUtility.UrlEncode(file));
                }
                else if (_BigIsSmall)
                {
                    return _BigFileName;
                }
                return null;
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// The small files server path
        /// </summary>
        public string SmallFileHostPath
        {
            get
            {
                if (_FoundSmallFile && !String.IsNullOrEmpty(_RelativePath))
                {
                    string file = Path.Combine(Path.GetDirectoryName(_RequestedFileName),
                                  Path.GetFileNameWithoutExtension(_RequestedFileName))
                                  + _SmallFileIndicator + Path.GetExtension(_RequestedFileName);
                    return Path.Combine(HostingEnvironment.MapPath("~/"), _RelativePath, file);
                }
                return null;
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Indicates if a small file was 
        /// successfully extrapolated
        /// </summary>
        public bool FoundSmallFile
        {
            get { return _FoundSmallFile; }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Indicates if the filename supplied in
        /// the constructor is actually a small file
        /// </summary>
        public bool BigIsSmall
        {
            get { return _BigIsSmall; }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// The name addon at the end of the file
        /// that denotes it is a small version
        /// </summary>
        public string SmallFileIndicator
        {
            get { return _SmallFileIndicator; }
            set { _SmallFileIndicator = value; }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Indicates what type of filepath
        /// the filname is which was
        /// supplied in the constructor
        /// </summary>
        public ImagePathType PathType
        {
            get { return _PathType; }
            set { PathType = value; }
        }

        #endregion // Public Properties

        #region Public Methods

        // ----------------------------------------------------------------------
        /// <summary>
        /// Removes the leading slash from a string
        /// </summary>
        /// <param name="removeFrom">The string to remove the slash from</param>
        /// <returns>The string supplied without a leading slash</returns>
        public static string RemoveLeadingSlash(string removeFrom)
        {
            if (removeFrom.Length > 0 && (removeFrom[0] == '/' || removeFrom[0] == '\\'))
                removeFrom = removeFrom.Substring(1, removeFrom.Length - 1);
            return removeFrom;
        }

        #endregion // Public Methods

        #region Constructor

        // ----------------------------------------------------------------------
        /// <summary>
        /// SmallImageUri Constructor
        /// </summary>
        /// <param name="bigFileName">The filename and path of the big file</param>
        /// <param name="smallFileIndicator">The filename addon which indicates a small file</param>
        /// <param name="relativePath">The relative path to insert between the filename/path and the root path</param>
        public SmallImageUri(string bigFileName, string smallFileIndicator, string relativePath)
        {
            _BigFileName = bigFileName;
            _RelativePath = relativePath;
            _SmallFileIndicator = smallFileIndicator;
            bool isRelative = !bigFileName.Contains("http://");
            bigFileName = RemoveLeadingSlash(_BigFileName);
            string urlRewritePath = HttpUtility.UrlDecode(bigFileName);

            // Check if using image.axd
            if (bigFileName.Contains(_ImageServer))
            {
                string[] att = bigFileName.Split('?');

                if (att.Length == 2)
                {
                    string tempFile = att[1];
                    att = tempFile.Split('&');

                    foreach (string s in att)
                    {
                        string[] variable = s.Split('=');
                        if (variable.Length == 2 && variable[0] == _ImageServerPicturePathVar)
                        {

                            _RequestedFileName = RemoveLeadingSlash(HttpUtility.UrlDecode(variable[1]));
                            _PathType = ImagePathType.ImageAxd;
                            _FoundSmallFile = true;
                            break;
                        }
                    }
                }
            }
            // Maybe we're using BE.NET url-rewriting
            else if (urlRewritePath.IndexOf(urlRewritePath) != -1 &&
                    urlRewritePath.Length > _UrlRewriteExtension.Length &&
                    String.Compare(urlRewritePath.Substring(urlRewritePath.Length - _UrlRewriteExtension.Length, _UrlRewriteExtension.Length), _UrlRewriteExtension, true) == 0)
            {
                int start = urlRewritePath.IndexOf(_UrlRewriteDirectory) + _UrlRewriteDirectory.Length;
                string relevantPath = urlRewritePath.Substring(start);
                relevantPath = relevantPath.Remove(relevantPath.Length - 5);
                _RequestedFileName = RemoveLeadingSlash(relevantPath);
                _PathType = ImagePathType.UrlRewrite;
                _FoundSmallFile = true;
            }
            // Absolute Image Path (No file handler)
            else if (!isRelative)
            {
                _PathType = ImagePathType.Absolute;
            }
            // Relative Image Path (No file handler)
            else
            {
                _PathType = ImagePathType.Relative;
            }

            if (_FoundSmallFile)
            {
                // Check to see if the file is already small
                string fileCheck = Path.GetFileNameWithoutExtension(_RequestedFileName);
                if (fileCheck.Length > _SmallFileIndicator.Length &&
                    fileCheck.Substring(fileCheck.Length - _SmallFileIndicator.Length) == _SmallFileIndicator)
                {
                    _FoundSmallFile = false;
                    _BigIsSmall = true;
                }
            }

        }

        #endregion // Constructor

        #region Enums

        // ----------------------------------------------------------------------
        public enum ImagePathType
        {
            ImageAxd,
            UrlRewrite,
            Relative,
            Absolute
        }

        #endregion // Enums

    }

    public class ImageSize
    {
        #region Fields

        public string Width;
        public string Height;

        #endregion // Fields

        #region Public Methods

        // ----------------------------------------------------------------------
        /// <summary>
        /// Gets the integer width
        /// </summary>
        /// <returns></returns>
        public int GetWidth()
        {
            return Convert.ToInt32(Regex.Replace(Width, "[EeMmPpXx]", String.Empty));
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Gets the integer height
        /// </summary>
        /// <returns></returns>
        public int GetHeight()
        {
            return Convert.ToInt32(Regex.Replace(Height, "[EeMmPpXx]", String.Empty));
        }

        #endregion // Public Methods

        #region Constructors

        // ----------------------------------------------------------------------
        /// <summary>
        /// Constructor
        /// </summary>
        public ImageSize()
        {
            Width = "0px";
            Height = "0px";
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="width">The width of the image</param>
        /// <param name="height">The height of the image</param>
        public ImageSize(string width, string height)
        {
            Width = width;
            Height = height;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="width">The width of the image</param>
        /// <param name="height">The height of the image</param>
        /// <param name="units">Measurement units</param>
        public ImageSize(int width, int height, string units)
        {
            Width = width.ToString() + units;
            Height = height.ToString() + units;
        }

        #endregion // Constructors
    }

    #endregion // Classes
}
