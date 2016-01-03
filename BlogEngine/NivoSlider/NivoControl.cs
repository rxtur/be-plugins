/* 
Author: rtur (http://rtur.net)
NivoSlider implementation for BlogEngine.NET (http://nivo.dev7studios.com)
*/

namespace rtur.net.NivoSlider
{
    using System.Web.UI;

    /// <summary>
    /// NivoSlider control
    /// </summary>
    public class NivoControl : Control
    {
        public string Width = "960";
        public string Height = "370";

        public override void RenderControl(HtmlTextWriter writer)
        {
            writer.Write(Repository.GetSlider(ID, Width, Height));
        }
    }
}
