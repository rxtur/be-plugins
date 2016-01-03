/* 
Author: rtur (http://rtur.net)
Showcase for BlogEngine.NET
*/

using System.Data;
using BlogEngine.Core;
using System;
using System.Linq;
using System.Collections.Generic;

namespace rtur.net.Showcase
{
    /// <summary>
    /// Summary description for Repository
    /// </summary>
    public class Repository
    {
        public static string GetData(string id, string width = "970", string height = "300", int MaxItems = 3)
        {
            if (Settings.ImageData == null || Settings.ImageData.GetDataTable().Rows.Count < 1)
                return "";

            var imgList = "";
            var root = Utils.ApplicationRelativeWebRoot;

            var img = "<div data-src=\"{0}{1}/{2}\">{3}</div>";
            var ttl = "<div class=\"camera_caption fadeFromBottom\"><a target=\"_new\" href=\"{0}\">{1}</a></div>";


            var random = new Random(DateTime.Now.Millisecond);

            var randomSortTable = new Dictionary<double, DataRow>();

            foreach (DataRow row in Settings.ImageData.GetDataTable().Rows)
                randomSortTable[random.NextDouble()] = row;

            List<DataRow> dt = randomSortTable.OrderBy(kvp => kvp.Key).Take(MaxItems).Select(kvp => kvp.Value).ToList();


            foreach (DataRow row in dt)
            {
                string image = row[Constants.Img].ToString();
                string url = row[Constants.Url].ToString();
                string title = row[Constants.Title].ToString();

                if (string.IsNullOrEmpty(img))
                    continue;

                if (string.IsNullOrEmpty(url))
                    url = "#";

                if (!string.IsNullOrEmpty(title))
                    title = string.Format(ttl, url, title);

                imgList += string.Format(img, Utils.ApplicationRelativeWebRoot, Constants.ImageFolder, image, title);
            }
            return string.Format(Template, imgList, height, width, Utils.ApplicationRelativeWebRoot);
        }

        static string Template
        {
            get{
                return @"
                <link rel=""stylesheet"" type=""text/css"" href=""{3}Custom/Controls/Showcase/camera.css"">                
                <script type=""text/javascript"" src=""{3}Custom/Controls/Showcase/Scripts/jquery.mobile.customized.min.js""></script>
                <script type=""text/javascript"" src=""{3}Custom/Controls/Showcase/Scripts/jquery.easing.1.3.js""></script>
                <script type=""text/javascript"" src=""{3}Custom/Controls/Showcase/Scripts/camera.min.js""></script>
                <script>
                    jQuery(function () {{
                        jQuery('#showcase1').camera({{
                            height: '{1}px',
                            thumbnails: false
                        }});
                    }});
	            </script>
                <style>.fluid_container {{ margin: 0 auto; max-width: {2}px; }}</style>
                <div class=""fluid_container"">
                    <div class=""camera_wrap camera_azure_skin"" id=""showcase1"">
                        {0}
                    </div>
                </div>
                ";
            }
        }
    }
}
