using System;
using System.Text.RegularExpressions;
using BlogEngine.Core;
using BlogEngine.Core.Web.Controls;
using BlogEngine.Core.Web.Extensions;
using Page = System.Web.UI.Page;
using System.Web;

namespace rtur.net.Jcarousel
{
    /// <summary>
    /// Jcarousel extension for BlogEngine.NET
    /// </summary>
    [Extension("Allows to save and display images in a carousel-like fashion.", "3.2.0.0", "<a href=\"http://rtur.net\">Rtur.net</a>")]
    public class Jcarousel
    {
        public Jcarousel()
        {
            Post.Serving += Publishable_Serving;
            BlogEngine.Core.Page.Serving += Publishable_Serving;
            var s = Settings.ImageData;
        }

        private static void Publishable_Serving(object sender, ServingEventArgs e)
        {
            if (!ExtensionManager.ExtensionEnabled("Jcarousel"))
                return;

            if (e.Location == ServingLocation.PostList || e.Location == ServingLocation.SinglePost || e.Location == ServingLocation.SinglePage)
            {
                var matches = Regex.Matches(e.Body, @"\[CAROUSEL:.*?\]");

                if (matches.Count == 0) return;

                foreach (Match match in matches)
                {
                    var width = "600";
                    var height = "400";

                    string[] options = match.Value.Replace("[CAROUSEL:", "").Replace("]", "").Trim().Split(':');

                    if (options.GetUpperBound(0) > 0)
                        width = options[1];

                    if (options.GetUpperBound(0) > 1)
                        height = options[2];

                    e.Body = e.Body.Replace(match.Value, Repository.GetCarousel(options[0], width, height));
                }
            }
        }
    }
}
