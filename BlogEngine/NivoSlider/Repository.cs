/* 
Author: rtur (http://rtur.net)
NivoSlider implementation for BlogEngine.NET (http://nivo.dev7studios.com)
*/

namespace rtur.net.NivoSlider
{
    using System.Data;
    using BlogEngine.Core;

    /// <summary>
    /// Repository for slider data
    /// </summary>
    public class Repository
    {
        public static string GetSlider(string id, string width = "960", string height = "370")
        {
            if (NivoSettings.ImageData == null || NivoSettings.ImageData.GetDataTable().Rows.Count < 1)
                return "";

            var div = string.Format("<div id=\"{0}\" style=\"width: {1}px; height: {2}px;\">", id, width, height);

            const string img = "<a href=\"{0}\" style=\"display:none\"><img runat=\"server\" src=\"~/image.axd?picture=Slider/{1}\" width=\"{2}\" height=\"{3}\" title=\"{4}\" alt=\"\" /></a>";

            foreach (DataRow row in NivoSettings.ImageData.GetDataTable().Rows)
            {
                string[] uid = row[Constants.UID].ToString().Split(':');
                
                if (uid.GetUpperBound(0) < 1)
                    continue;

                if(uid[0] == id)
                    div += string.Format(img, row[Constants.Url], uid[1], width, height, row[Constants.Title]);
            }

            div += "</div>";

            //if (Security.IsAdministrator)
            //{
            //    var left = int.Parse(width) - 40;
            //    var url = Blog.CurrentInstance.RelativeWebRoot + Constants.AdminUrl.Replace("~/", "") + "?ctrl=" + id;
            //    div += string.Format("<a href=\"{0}\" runat=\"server\" class=\"nivo-edit\" style=\"left:{1}px\">Edit</a>", url, left);
            //}

            div += string.Format(Template, Utils.ApplicationRelativeWebRoot);
            
            div += "<script type=\"text/javascript\">$(window).load(function (){ LoadSlider('#" + id + "');});</script>";

            return div;
        }

        static string Template
        {
            get
            {
                return @"
                <link rel=""stylesheet"" type=""text/css"" href=""{0}Custom/Extensions/NivoSlider/nivo-slider.css"">
                <script type=""text/javascript"" src=""{0}Custom/Extensions/NivoSlider/jquery.nivo.slider.pack.js""></script>
                ";
            }
        }
	}
}
