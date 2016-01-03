/* 
Author: rtur (http://rtur.net)
Jcarousel implementation for BlogEngine.NET
*/

using BlogEngine.Core.Web.Extensions;

namespace rtur.net.Jcarousel
{
    /// <summary>
    /// Jcarousel extension Settings
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

                    extensionSettings.AddParameter(Constants.UID, "Album");
                    extensionSettings.AddParameter(Constants.Title, "Title");

                    extensionSettings.AddValues(new[] { "Album1:img1.jpg", "" });
                    extensionSettings.AddValues(new[] { "Album1:img2.jpg", "sample two" });
                    extensionSettings.AddValues(new[] { "Album1:img3.jpg", "" });
                    extensionSettings.AddValues(new[] { "Album1:img4.jpg", "sample four" });

                    settings = ExtensionManager.InitSettings(Constants.ExtensionName, extensionSettings);

                    ExtensionManager.SetAdminPage(Constants.ExtensionName, Constants.AdminUrl);
                }
                return settings;
            }
        }
    }
}
