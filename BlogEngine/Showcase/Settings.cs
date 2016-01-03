/* 
Author: rtur (http://rtur.net)
Showcase for BlogEngine.NET
*/

using BlogEngine.Core.Web.Extensions;

namespace rtur.net.Showcase
{
    /// <summary>
    /// Extension Settings
    /// </summary>
    public class Settings
    {
        static ExtensionSettings settings;

        public static ExtensionSettings ImageData
        {
            get
            {
                if (settings == null)
                {
                    var extensionSettings = new ExtensionSettings(Constants.ExtensionName);

                    extensionSettings.AddParameter(Constants.Img, "Image");
                    extensionSettings.AddParameter(Constants.Url, "Url");
                    extensionSettings.AddParameter(Constants.Title, "Title");

                    extensionSettings.AddValues(new[] { "bridge.jpg", "#", "Bridge" });
                    extensionSettings.AddValues(new[] { "leaf.jpg", "#", "Leaf" });
                    extensionSettings.AddValues(new[] { "road.jpg", "#", "" });

                    settings = ExtensionManager.InitSettings(Constants.ExtensionName, extensionSettings);

                    ExtensionManager.SetAdminPage(Constants.ExtensionName, Constants.AdminUrl);
                }
                return settings;
            }
        }
    }
}
