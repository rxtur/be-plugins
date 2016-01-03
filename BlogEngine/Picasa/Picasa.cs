using System;
using System.Text.RegularExpressions;
using BlogEngine.Core;
using BlogEngine.Core.Web.Controls;
using Google.GData.Photos;
using BlogEngine.Core.Web.Extensions;

namespace App_Code.Extensions
{
    /// <summary>
    /// Picasa extension for BlogEngine 3.0
    /// </summary>
    [Extension("Picasa Web Albums and SlideShow", "3.1.1.0", "<a href=\"http://rtur.net\">Rtur.net</a>")]
    public class Picasa
    {
        #region Private members
        static protected ExtensionSettings Settings;
        static protected ExtensionSettings Albums;

        const string Tag = "<embed type=\"application/x-shockwave-flash\" src=\"http://picasaweb.google.com/s/c/bin/slideshow.swf\" width=\"{4}\" height=\"{5}\" flashvars=\"host=picasaweb.google.com{3}&RGB=0x000000&feed=http%3A%2F%2Fpicasaweb.google.com%2Fdata%2Ffeed%2Fapi%2Fuser%2F{2}%2Falbumid%2F{0}%3Fkind%3Dphoto%26alt%3Drss%26{1}\" pluginspage=\"http://www.macromedia.com/go/getflashplayer\"></embed>";
        const string Img = "<li><a rel=\"prettyPhoto\" href='{1}'><img src='{0}'></a></li>";
        #endregion

        public Picasa()
        {
            Post.Serving += PostServing;
            BlogEngine.Core.Page.Serving += PageServing;

            InitSettings();
        }

        static void PageServing(object sender, ServingEventArgs e)
        {
            e.Body = GetBody(e.Body);
        }

        private static void PostServing(object sender, ServingEventArgs e)
        {
            if (e.Location == ServingLocation.PostList
                || e.Location == ServingLocation.SinglePost)
            {
                e.Body = GetBody(e.Body);
            }
        }

        private static string GetBody(string body)
        {
            if (!string.IsNullOrEmpty(body))
            {
                const string regShow = @"\[PicasaShow:.*?\]";
                const string regAlbm = @"\[PicasaAlbum:.*?\]";
                string picasa;

                MatchCollection shows = Regex.Matches(body, regShow);
                MatchCollection albums = Regex.Matches(body, regAlbm);

                if (shows.Count > 0)
                {
                    foreach (Match match in shows)
                    {
                        if (match.Length > 11)
                        {
                            string id = match.Value.Substring(12).Replace("]", "");
                            picasa = "<div id=\"PicasaShow\">";
                            picasa += GetSlideShow(id);
                            picasa += "</div>";
                            body = body.Replace(match.Value, picasa);
                        }
                    }
                }

                if (albums.Count > 0)
                {
                    foreach (Match album in albums)
                    {
                        if (album.Length > 12)
                        {
                            string id = album.Value.Substring(13).Replace("]", "");
                            picasa = "<div id=\"PicasaAlbum\">";
                            picasa += GetAlbum(id);
                            picasa += "</div>";
                            picasa += Styles;
                            body = body.Replace(album.Value, picasa);
                        }
                    }
                }
            }
            return body;
        }

        private static string GetSlideShow(string albumId)
        {
            string s;
            try
            {
                var service = new PicasaService("exampleCo-exampleApp-1");
                string usr = Settings.GetSingleValue("Account") + "@gmail.com";
                string pwd = Settings.GetSingleValue("Password");
                service.setUserCredentials(usr, pwd);

                var query = new AlbumQuery(PicasaQuery.CreatePicasaUri(usr));
                PicasaFeed feed = service.Query(query);

                string id = "";
                string key = "";

                foreach (PicasaEntry entry in feed.Entries)
                {
                    var ac = new AlbumAccessor(entry);

                    if (ac.Name == albumId)
                    {
                        id = ac.Id;
                        string feedUri = entry.FeedUri;
                        if (feedUri.Contains("authkey="))
                        {
                            string authKey = feedUri.Substring(feedUri.IndexOf("authkey=")).Substring(8);
                            key = authKey;
                        }
                    }
                }

                if (key.Length > 0) key = "authkey%3D" + key;

                string user = Settings.GetSingleValue("Account");

                string auto = "";
                if (bool.Parse(Settings.GetSingleValue("AutoPlay")) == false)
                {
                    auto = "&noautoplay=1";
                }

                string width = Settings.GetSingleValue("ShowWidth");
                string height = "96";

                if (int.Parse(width) > 0)
                {
                    height = (int.Parse(width) * 0.74).ToString();
                }
                s = string.Format(Tag, id, key, user, auto, width, height);
            }
            catch (Exception exp)
            {
                s = exp.Message;
            }
            return s;
        }

        private static string GetAlbum(string album)
        {
            string retVal;
            try
            {
                var service = new PicasaService("exampleCo-exampleApp-1");

                string usr = Settings.GetSingleValue("Account") + "@gmail.com";
                string pwd = Settings.GetSingleValue("Password");

                service.setUserCredentials(usr, pwd);

                var query = new PhotoQuery(PicasaQuery.CreatePicasaUri(usr, album));
                PicasaFeed feed = service.Query(query);

                retVal = "<ul id=\"AlbumList\">";
                foreach (PicasaEntry entry in feed.Entries)
                {
                    var firstThumbUrl = entry.Media.Thumbnails[0].Attributes["url"] as string;
                    string thumGrp = "/s" + Settings.GetSingleValue("PicWidth") + "/";

                    if (firstThumbUrl != null) firstThumbUrl = firstThumbUrl.Replace("/s72/", thumGrp);

                    var contentUrl = entry.Media.Content.Attributes["url"] as string;

                    if (contentUrl != null) contentUrl = contentUrl.Substring(0, contentUrl.LastIndexOf("/"));
                    
                    contentUrl += "/s640/" + entry.Title.Text;

                    retVal += string.Format(Img, firstThumbUrl, contentUrl);
                }
                retVal += "</ul>";
            }
            catch (Exception qex)
            {
                retVal = qex.Message;
            }
            return retVal;
        }

        protected void InitSettings()
        {
            var settings = new ExtensionSettings("Picasa");

            settings.AddParameter("Account");
            settings.AddParameter("Password");
            settings.AddParameter("ShowWidth");
            settings.AddParameter("PicWidth");
            settings.AddParameter("AutoPlay");

            settings.AddValue("Account", "");
            settings.AddValue("Password", "secret");
            settings.AddValue("ShowWidth", "400");
            settings.AddValue("PicWidth", "72");
            settings.AddValue("AutoPlay", true);

            settings.IsScalar = true;
            ExtensionManager.ImportSettings(settings);

            ExtensionManager.SetAdminPage("Picasa", "~/Custom/Controls/Picasa/Admin.aspx");

            Settings = ExtensionManager.GetSettings("Picasa");
        }

        static string Styles
        {
            get
            {
                return @"
                <style>
    #PicasaShow { display:block; overflow:auto; margin: 15px 0 0 0; }
    #PicasaAlbum { display:block; overflow:auto; margin:10px 0 0 0; clear:both; }
    #PicasaAlbum ul { list-style-type: none; }
    #AlbumList { float:left; padding:0; margin: 0; }
    #AlbumList a { background: #fff; padding: 0; margin:0; }
    #AlbumList li { width: 144px; height: 110px; padding: 0; margin: 2px; float:left; background: #fafafa ; border: 1px solid #ccc; }
    #AlbumList li:hover { border: 1px solid #000; }
    #AlbumList img { display:block; border: 0; }
                </style>
                ";
            }
        }
    }
}
