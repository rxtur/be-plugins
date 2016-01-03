/* 
Author: rtur (http://rtur.net)
jCarousel implementation for BlogEngine.NET
*/

using System.Data;
using BlogEngine.Core;

namespace rtur.net.Jcarousel
{
    /// <summary>
    /// Summary description for Repository
    /// </summary>
    public class Repository
    {
        public static string GetCarousel(string id, string width = "600", string height = "400")
        {
            if (Settings.ImageData == null || Settings.ImageData.GetDataTable().Rows.Count < 1)
                return "";

            var imgList = "";
            var thumbList = "";
            var root = Utils.RelativeWebRoot;

            var img = "<li><img src=\"" + root + "image.axd?picture=/jcarousel/{0}/{1}\" width=\"{2}\" height=\"{3}\"></li>";
            var thumb = "<li><img src=\"" + root + "image.axd?picture=/jcarousel/{0}/{1}\" width=\"50\" height=\"50\"></li>";

            foreach (DataRow row in Settings.ImageData.GetDataTable().Rows)
            {
                string[] uid = row[Constants.UID].ToString().Split(':');

                if (uid.GetUpperBound(0) < 1)
                    continue;

                if (uid[0] == id)
                {
                    imgList += string.Format(img, uid[0], uid[1], width, height);
                    thumbList += string.Format(thumb, uid[0], uid[1].Replace(".", "_thumb."));
                }
            }
            return string.Format(Template, imgList, thumbList, root);
        }

        static string Template
        {
            get{
                return @"
                <link rel=""stylesheet"" type=""text/css"" href=""{2}Custom/Extensions/Jcarousel/style.css"">
                <link rel=""stylesheet"" type=""text/css"" href=""{2}Custom/Extensions/Jcarousel/jcarousel.connected-carousels.css"">
                <script type=""text/javascript"" src=""{2}Custom/Extensions/Jcarousel/jquery.jcarousel.min.js""></script>
                <script type=""text/javascript"" src=""{2}Custom/Extensions/Jcarousel/jcarousel.connected-carousels.js""></script>
                <div class=""connected-carousels""> 
                    <div class=""stage"">
                        <div class=""carousel carousel-stage"">
                            <ul>
                                {0}
                            </ul>
                        </div>
                        <a href=""#"" class=""prev prev-stage""><span>&lsaquo;</span></a>
                        <a href=""#"" class=""next next-stage""><span>&rsaquo;</span></a>
                    </div>
                    <div class=""navigation"">
                        <a href=""#"" class=""prev prev-navigation"">&lsaquo;</a>
                        <a href=""#"" class=""next next-navigation"">&rsaquo;</a>
                        <div class=""carousel carousel-navigation"">
                            <ul>
                                {1}
                            </ul>
                        </div>
                    </div>
                </div>
                ";
            }
        }
    }
}
