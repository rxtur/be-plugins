/* 
Author: rtur (http://rtur.net)
NivoSlider implementation for BlogEngine.NET (http://nivo.dev7studios.com)
*/

namespace rtur.net.NivoSlider
{
    using System.Text.RegularExpressions;
    using BlogEngine.Core;
    using BlogEngine.Core.Web.Controls;

    /// <summary>
    /// NivoSlider implementation for BlogEngine.NET
    /// </summary>
    [Extension("jQuery Image Slider", "3.2.0.0", "<a href=\"http://rtur.net/blog/search.aspx?q=nivoslider\">rtur.net</a>")]
    public class NivoSlider
    {
        public NivoSlider()
        {
            Post.Serving += PublishableServing;
            Page.Serving += PublishableServing;
            var s = NivoSettings.ImageData;
        }

        private static void PublishableServing(object sender, ServingEventArgs e)
        {
            const string regex = @"\[SLIDER:.*?\]";
            var matches = Regex.Matches(e.Body, regex);

            if (matches.Count == 0) return;

            foreach (Match match in matches)
            {
                var width = "650";
                var height = "220";

                string[] options = match.Value.Replace("[SLIDER:", "").Replace("]", "").Trim().Split(':');

                if (options.GetUpperBound(0) > 0)
                    width = options[1];

                if (options.GetUpperBound(0) > 1)
                    height = options[2];

                e.Body = e.Body.Replace(match.Value, Repository.GetSlider(options[0], width, height));
            }
        }
    }
}
