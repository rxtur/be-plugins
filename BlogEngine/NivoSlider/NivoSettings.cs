/* 
Author: rtur (http://rtur.net)
NivoSlider implementation for BlogEngine.NET (http://nivo.dev7studios.com)
*/

namespace rtur.net.NivoSlider
{
    using BlogEngine.Core.Web.Extensions;

    /// <summary>
    /// Settings for NivoSlider
    /// </summary>
    public class NivoSettings
    {
        static ExtensionSettings settings;

        public static ExtensionSettings ImageData
        {
            get
            {
                if (settings == null)
                {
                    var extensionSettings = new ExtensionSettings(Constants.ExtensionName);
                    
                    extensionSettings.AddParameter(Constants.UID, "UID");
                    extensionSettings.AddParameter(Constants.Url, "Url");
                    extensionSettings.AddParameter(Constants.Title, "Title");

                    extensionSettings.AddValues(new[] { "slider1:sample1.png", "#", "" });
                    extensionSettings.AddValues(new[] { "slider1:sample2.png", "#", "sample two" });

                    settings = ExtensionManager.InitSettings(Constants.ExtensionName, extensionSettings);

                    ExtensionManager.SetAdminPage(Constants.ExtensionName, Constants.AdminUrl);
                }
                return settings;
            }
        }
    }
}
