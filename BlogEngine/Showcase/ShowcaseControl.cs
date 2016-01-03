/* 
Author: rtur (http://rtur.net)
Showcase for BlogEngine.NET
*/

namespace rtur.net.Showcase
{
    using System.Web.UI;

    /// <summary>
    /// Showcase control
    /// </summary>
    public class ShowcaseControl : Control
    {
        public string Width = "970";
        public string Height = "300";
        public int MaxItems = 3;

        public override void RenderControl(HtmlTextWriter writer)
        {
            writer.Write(Repository.GetData(ID, Width, Height, MaxItems));
        }
    }
}
