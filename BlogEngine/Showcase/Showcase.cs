/* 
Author: rtur (http://rtur.net)
Showcase for BlogEngine.NET
*/

namespace rtur.net.Showcase
{
    using BlogEngine.Core.Web.Controls;

    /// <summary>
    /// Showcase for BlogEngine.NET
    /// </summary>
    [Extension("Showcase Image Slider", "3.0.0.1", "<a href=\"http://rtur.net\">rtur.net</a>")]
    public class Showcase
    {
        public Showcase()
        {
            // initialize settings
            var s = Settings.ImageData;
        }
    }
}
