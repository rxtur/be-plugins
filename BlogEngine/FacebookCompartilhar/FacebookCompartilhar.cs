using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BlogEngine.Core;
using BlogEngine.Core.Web.Controls;
using BlogEngine.Core.Web.Extensions;

/// <summary>
/// Caio Humberto Francisco
/// www.acainfo.com.br
/// 
/// Botão de compartilhamento do post no facebook
/// </summary>
[Extension("Botão de compartilhamento do post no facebook.", "3.0.0.1", "<a target=\"_blank\" href=\"http://www.acainfo.com.br\">ACA INFO</a>")]
public class FacebookCompartilhar
{
    private const string ExtensionName = "FacebookCompartilhar";
    static protected ExtensionSettings _settings = null;

    static protected Regex ER_IMG = new Regex("<img.*?src=\"(?<img>.*?)\".*?>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    static protected bool findImg
    {
        get
        {
            return Convert.ToBoolean(_settings.GetSingleValue("findImg"));
        }
    }

    static protected string ondeExibir
    {
        get
        {
            return _settings.GetSingleValue("ondeExibir");
        }
    }

    public FacebookCompartilhar()
    {
        BlogEngine.Core.Post.Serving += new EventHandler<ServingEventArgs>(PrepareFacebookCompartilhar);

        ExtensionSettings settings = new ExtensionSettings(ExtensionName);

        settings.AddParameter("findImg", "Exibir imagem", 1, true, false, ParameterType.Boolean);
        settings.AddParameter("ondeExibir", "Onde exibir", 10, true, false, ParameterType.DropDown);

        settings.AddValue("findImg", true);
        settings.AddValue("ondeExibir", new string[] { "Topo", "Rodape" }, "Rodapé");

        settings.Help = "<br />Botão de compartilhamento do post no facebook.<br /><a target=\"_blank\" href=\"http://www.acainfo.com.br\">ACA INFO</a>";
        settings.IsScalar = true;

        _settings = ExtensionManager.InitSettings(ExtensionName, settings);
    }

    private void PrepareFacebookCompartilhar(object sender, ServingEventArgs e)
    {
        if (e.Location == ServingLocation.PostList || e.Location == ServingLocation.SinglePost)
        {
            HttpContext context = HttpContext.Current;
            if (context.CurrentHandler is System.Web.UI.Page)
            {
                if (context.Items[ExtensionName] == null)
                {
                    var page = (System.Web.UI.Page)context.CurrentHandler;
                    var Script = new StringBuilder();
                    Script.AppendLine("");
                    Script.AppendLine("<!-- Inicio facebook compartilhamento -->");
                    Script.AppendLine("<script type=\"text/javascript\">");
                    Script.AppendLine("//<![CDATA[");
                    Script.AppendLine("function shareFunc(href, title, desc, img){");
                    Script.AppendLine("	//var url = \"http://www.facebook.com/share.php?u=\" + encodeURIComponent(href) + \"&t=\" + encodeURIComponent(title) + \"&d=\" + encodeURIComponent(desc) + \"&i=\" + encodeURIComponent(img);");
                    Script.AppendLine("	var url1 = \"http://www.facebook.com/share.php?u=\";");
                    //Script.AppendLine("	var url2 = encodeURIComponent(\"http://fb-share-control.com/?\" + \"t=\" + title + \"&d=\" + desc + \"&u=\" + href);");
                    Script.AppendLine("	var url2 = encodeURIComponent(\"http://fb-share-control.com/?\" + \"t=\" + title + \"&i=\" + img + \"&d=\" + desc + \"&u=\" + href);");
                    Script.AppendLine("	var url = url1 + \"\" + url2;");
                    Script.AppendLine("	//alert(url);");
                    Script.AppendLine("	window.open(url,\"Comparilhar\",\"width=640,height=300\");");
                    Script.AppendLine("}");
                    Script.AppendLine("//]]>");
                    Script.AppendLine("</script>");
                    Script.AppendLine("<style type=\"text/css\">.facebookCompartilhar{padding:2px 0 0 20px; height:16px; background:url(http://static.ak.fbcdn.net/images/share/facebook_share_icon.gif) no-repeat top left; color: #6D84B4;cursor: pointer;text-decoration: none;}</style>");

                    page.ClientScript.RegisterStartupScript(page.GetType(), "FacebookCompartilharScripts", Script.ToString(), false);
                    context.Items[ExtensionName] = 1;
                }

                var ListImagens = "";

                if (findImg)
                {
                    if (ER_IMG.IsMatch(e.Body))
                    {
                        MatchCollection imagens = ER_IMG.Matches(e.Body);
                        ListImagens = string.Format("http://{0}{1}", ((Post)sender).AbsoluteLink.Authority, imagens[0].Result("${img}"));
                    }
                }
                if (ondeExibir.Equals("Topo"))
                {
                    e.Body = string.Format("<p><a class=\"facebookCompartilhar\" onclick=\"shareFunc('{1}','{2}','{3}','{4}');\" >Compartilhar</a></p>{0}", e.Body, ((Post)sender).AbsoluteLink, ((Post)sender).Title, ((Post)sender).Description, ListImagens);
                }
                else
                {
                    e.Body = string.Format("{0}<p><a class=\"facebookCompartilhar\" onclick=\"shareFunc('{1}','{2}','{3}','{4}');\" >Compartilhar</a></p>", e.Body, ((Post)sender).AbsoluteLink, ((Post)sender).Title, ((Post)sender).Description, ListImagens);
                }
            }
        }
    }
}
