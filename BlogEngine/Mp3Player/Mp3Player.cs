/* Author:      Ruslan Tur (http://rtur.net)
 * Copyright:   2011 Ruslan Tur (http://rtur.net)
 * About:       mp3player (http://rtur.net/blog/page/MP3-Player.aspx)
 *              BlogEngine.NET mp3 audio player extension.
*/
namespace App_Code.Extensions
{
    using System;
    using BlogEngine.Core;
    using System.Web;
    using System.Web.UI;
    using System.Web.UI.HtmlControls;
    using System.Text.RegularExpressions;
    using BlogEngine.Core.Web.Controls;
    using BlogEngine.Core.Web.Extensions;

    /// <summary>
    /// Add flash audio player(s) to the blog post
    /// Developed by Ruslan Tur (http://rtur.net)
    /// Visit http://rtur.net for updates
    /// </summary>
    [Extension("Mp3 Flash Audio Player", "3.2.0.0", "<a href=\"http://rtur.net/blog/page/MP3-Player.aspx\">Rtur.net</a>")]
    public class Mp3Player
    {
        #region Private members
        public const string Ext = "Mp3Player";
        public const string Width = "width";
        public const string Height = "height";
        public const string BgColor = "bgColor";
        public const string Bg = "bg";
        public const string Leftbg = "leftbg";
        public const string Lefticon = "lefticon";
        public const string Rightbg = "rightbg";
        public const string Rightbghover = "rightbghover";
        public const string Righticon = "righticon";
        public const string Righticonhover = "righticonhover";
        public const string Text = "text";
        public const string Slider = "slider";
        public const string Track = "track";
        public const string Border = "border";
        public const string Loader = "loader";
        public static ExtensionSettings Settings;
        private static long cnt;

        #endregion

        /// <summary>
        /// Default constructor called on application start up
        /// from Global.asax to initialize extension
        /// </summary>
        public Mp3Player()
        {
            Post.Serving += PublishableServing;
            BlogEngine.Core.Page.Serving += PublishableServing;

            // set page that extension manager will use  
            // instead of default settings page
            ExtensionManager.SetAdminPage("Mp3Player", "~/Custom/Controls/Mp3Player/Admin.aspx");

            // set default setting values
            SetDefaultSettings();
        }

        /// <summary>
        /// An event that handles ServingEventArgs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void PublishableServing(object sender, ServingEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Body))
            {
                // only process the posts
                if (e.Location == ServingLocation.PostList
                    || e.Location == ServingLocation.SinglePost
                    || e.Location == ServingLocation.SinglePage
                    || e.Location == ServingLocation.Feed)
                {
                    const string regex = @"\[mp3:.*?\.mp3]";
                    var matches = Regex.Matches(e.Body, regex);

                    if (matches.Count > 0)
                    {
                        if (e.Location != ServingLocation.Feed)
                            AddJsToTheHeader();

                        var mp3Root = string.Format("{0}Custom/Media/mp3/", Utils.AbsoluteWebRoot);

                        foreach (Match match in matches)
                        {
                            string filename = match.Value.Replace("[mp3:", "").Replace("]", "").Trim();
                            if (e.Location == ServingLocation.Feed)
                            {
                                // inject link to .mp3 file (to support enclosure)
                                var url = mp3Root + filename;
                                var link = "<a href=\"{0}\">{1}</a>";
                                link = string.Format(link, url, filename);
                                e.Body = e.Body.Replace(match.Value, link);
                            }
                            else
                            {
                                // inject player object in the post
                                var player = PlayerObject(filename);
                                player = "<script type=\"text/javascript\">InsertPlayer(\"" + player + "\");</script>";
                                e.Body = e.Body.Replace(match.Value, player);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Inject JavaScript file into the header of the post
        /// </summary>
        public static void AddJsToTheHeader()
        {
            // get a page handler
            var pg = (System.Web.UI.Page)HttpContext.Current.CurrentHandler;
            bool added = false;

            // check if script already added to the page header
            foreach (Control ctl in pg.Header.Controls)
            {
                if (ctl.GetType() == typeof(HtmlGenericControl))
                {
                    var gc = (HtmlGenericControl)ctl;
                    if (gc.Attributes["src"] != null)
                    {
                        if (gc.Attributes["src"].Contains("player.js"))
                        {
                            added = true;
                        }
                    }
                }
            }

            if (!added)
            {
                var js = new HtmlGenericControl("script");
                var playerRoot = string.Format("{0}Custom/Controls/Mp3Player/", Utils.AbsoluteWebRoot);
                js.Attributes.Add("type", "text/javascript");
                js.Attributes.Add("src", playerRoot + "player.js");

                pg.Header.Controls.Add(js);
            }
        }

        /// <summary>
        /// Build object tag
        /// </summary>
        /// <param name="soundFile">Name of the mp3 file ("my song.mp3")</param>
        /// <returns>Flash object markup</returns>
        public static string PlayerObject(string soundFile)
        {
            string sFile = string.Empty;
            string[] sFiles = soundFile.Split(",".ToCharArray());
            var mp3Root = string.Format("{0}Custom/Media/mp3/", Utils.AbsoluteWebRoot);

            foreach (string file in sFiles)
            {
                if (file.Substring(0, 7) == "http://")
                {
                    sFile += file;
                }
                else
                {
                    sFile += mp3Root + file;
                }
                sFile += ",";
            }

            sFile = sFile.Substring(0, sFile.Length - 1);
            sFile = HttpUtility.UrlEncode(sFile);
            var playerRoot = string.Format("{0}Custom/Controls/Mp3Player/", Utils.AbsoluteWebRoot);

            const string s = "<p>"
                + "<object type='application/x-shockwave-flash' data='{0}player.swf' id='audioplayer{1}' height='{18}' width='{17}'>"
                + "<param name='movie' value='{0}player.swf'>"
                + "<param name='FlashVars' value='playerID={1}&bg=0x{5}&leftbg=0x{6}&lefticon=0x{7}&rightbg=0x{8}&rightbghover=0x{9}&righticon=0x{10}&righticonhover=0x{11}&text=0x{12}&slider=0x{13}&track=0x{14}&border=0x{15}&loader=0x{16}&soundFile={2}'>"
                + "<param name='quality' value='high'>"
                + "<param name='menu' value='{3}'>"
                + "<param name='bgcolor' value='{4}'>"
                + "</object>"
                + "</p>";

            cnt++;

            return String.Format(s, playerRoot, cnt, sFile, "No",
                Settings.GetSingleValue(BgColor),
                Settings.GetSingleValue(Bg),
                Settings.GetSingleValue(Leftbg),
                Settings.GetSingleValue(Lefticon),
                Settings.GetSingleValue(Rightbg),
                Settings.GetSingleValue(Rightbghover),
                Settings.GetSingleValue(Righticon),
                Settings.GetSingleValue(Righticonhover),
                Settings.GetSingleValue(Text),
                Settings.GetSingleValue(Slider),
                Settings.GetSingleValue(Track),
                Settings.GetSingleValue(Border),
                Settings.GetSingleValue(Loader),
                Settings.GetSingleValue(Width),
                Settings.GetSingleValue(Height));
        }

        /// <summary>
        /// Initializes settings with default values
        /// </summary>
        public static void SetDefaultSettings()
        {
            var settings = new ExtensionSettings(Ext);

            settings.AddParameter(Width);
            settings.AddParameter(Height);
            settings.AddParameter(BgColor);
            settings.AddParameter(Bg);
            settings.AddParameter(Leftbg);
            settings.AddParameter(Lefticon);
            settings.AddParameter(Rightbg);
            settings.AddParameter(Rightbghover);
            settings.AddParameter(Righticon);
            settings.AddParameter(Righticonhover);
            settings.AddParameter(Text);
            settings.AddParameter(Slider);
            settings.AddParameter(Track);
            settings.AddParameter(Border);
            settings.AddParameter(Loader);

            settings.AddValue(Width, "290");
            settings.AddValue(Height, "24");
            settings.AddValue(BgColor, "ffffff");
            settings.AddValue(Bg, "f8f8f8");
            settings.AddValue(Leftbg, "eeeeee");
            settings.AddValue(Lefticon, "666666");
            settings.AddValue(Rightbg, "cccccc");
            settings.AddValue(Rightbghover, "999999");
            settings.AddValue(Righticon, "666666");
            settings.AddValue(Righticonhover, "ffffff");
            settings.AddValue(Text, "666666");
            settings.AddValue(Slider, "666666");
            settings.AddValue(Track, "ffffff");
            settings.AddValue(Border, "666666");
            settings.AddValue(Loader, "9FFFB8");

            settings.IsScalar = true;
            ExtensionManager.ImportSettings(settings);
            Settings = ExtensionManager.GetSettings(Ext);
        }

        public static string PlayerTag
        {
            get
            {
                var player = Mp3Player.PlayerObject("test.mp3");
                player = "<script type=\"text/javascript\">InsertPlayer(\"" + player + "\");</script>";
                return "";
            }
        }
    }
}