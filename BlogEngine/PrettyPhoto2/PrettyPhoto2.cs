using System;
using System.Text;
using System.Web;
using System.Web.UI;
using BlogEngine.Core;
using BlogEngine.Core.Web.Controls;

namespace App_Code.Extensions
{
    [Extension("PrettyPhoto2 - jQuery lightbox clone", "3.0.0.1", "<a href=\"http://rtur.net\">Rtur.net</a>")]
    public class PrettyPhoto2
    {
        private const string PrettyPhotoFolder = "Custom/Controls/PrettyPhoto"; 

        public PrettyPhoto2()
        {
            BlogEngine.Core.Page.Serving += ItemServing;
            Post.Serving += ItemServing;
        }

        static void ItemServing(object sender, ServingEventArgs e)
        {
            if (HttpContext.Current.CurrentHandler is BlogBasePage)
                (HttpContext.Current.CurrentHandler as BlogBasePage).LoadComplete += BasePageLoadComplete;
        }

        static void BasePageLoadComplete(object sender, EventArgs e)
        {
            var basePage = sender as BlogBasePage;
            PreparePrettyPhoto(basePage);
        }

        private static void PreparePrettyPhoto(BlogBasePage basePage)
        {
            var litLinks = new LiteralControl {ID = "ppControl"};

            if (basePage.Header.FindControl("ppControl") == null)
            {
                var scriptLinks = new StringBuilder();
            
                const string js = "<script type=\"text/javascript\" src=\"{0}{1}\"></script>";
                const string css = "<link type=\"text/css\" href=\"{0}\" rel=\"stylesheet\" />";

                scriptLinks.Append(string.Format(css, Utils.RelativeWebRoot + PrettyPhotoFolder + "/css/prettyPhoto.css"));
                scriptLinks.Append(string.Format(js, Utils.RelativeWebRoot, PrettyPhotoFolder + "/js/jquery-migrate-1.0.0.js"));
                scriptLinks.Append(string.Format(js, Utils.RelativeWebRoot, PrettyPhotoFolder + "/js/jquery.prettyPhoto.js"));
                scriptLinks.Append(string.Format(js, Utils.RelativeWebRoot, PrettyPhotoFolder + "/js/PrettyPhotoStarter.js"));

                litLinks.Text = scriptLinks.ToString();  
                basePage.Header.Controls.Add(litLinks);
            }
        }
    }
}
