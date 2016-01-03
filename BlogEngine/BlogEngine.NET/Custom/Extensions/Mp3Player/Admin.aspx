<%@ Page Language="C#" %>
<%@ Import Namespace="System" %>
<%@ Import Namespace="System.Web.UI" %>
<%@ Import Namespace="System.Web.UI.WebControls" %>
<%@ Import Namespace="BlogEngine.Core.Web.Extensions" %>
<%@ Import Namespace="System.Text.RegularExpressions" %>
<%@ Import Namespace="App_Code.Extensions" %>
<script language="c#" runat="server">
    protected void Page_Load(object sender, EventArgs e)
    {
        ErrorMsg.InnerHtml = string.Empty;
        ErrorMsg.Visible = false;

        if (!Page.IsPostBack)
        {
            Mp3Player.SetDefaultSettings();
            BindForm();
        }

        SetPlayer();
    }

    protected void BtnSaveClick(object sender, EventArgs e)
    {
        if (IsValidForm())
        {
            SaveSettings();
        }
    }

    protected void BindForm()
    {
        btnSave.Text = Resources.labels.saveSettings;

        Control_width.Text = Mp3Player.Settings.GetSingleValue(Mp3Player.Width);
        Control_height.Text = Mp3Player.Settings.GetSingleValue(Mp3Player.Height);
        Contrlo_background.Text = Mp3Player.Settings.GetSingleValue(Mp3Player.BgColor);
        Player_background.Text = Mp3Player.Settings.GetSingleValue(Mp3Player.Bg);
        Left_background.Text = Mp3Player.Settings.GetSingleValue(Mp3Player.Leftbg);
        Left_icon.Text = Mp3Player.Settings.GetSingleValue(Mp3Player.Lefticon);
        Right_background.Text = Mp3Player.Settings.GetSingleValue(Mp3Player.Rightbg);
        Right_background_hover.Text = Mp3Player.Settings.GetSingleValue(Mp3Player.Rightbghover);
        Right_icon.Text = Mp3Player.Settings.GetSingleValue(Mp3Player.Righticon);
        Right_icon_hover.Text = Mp3Player.Settings.GetSingleValue(Mp3Player.Righticonhover);
        Text_color.Text = Mp3Player.Settings.GetSingleValue(Mp3Player.Text);
        Slider.Text = Mp3Player.Settings.GetSingleValue(Mp3Player.Slider);
        Track.Text = Mp3Player.Settings.GetSingleValue(Mp3Player.Track);
        Border.Text = Mp3Player.Settings.GetSingleValue(Mp3Player.Border);
        Loader.Text = Mp3Player.Settings.GetSingleValue(Mp3Player.Loader);

        lblBgColor.Style["background-color"] = "#" + Mp3Player.Settings.GetSingleValue(Mp3Player.BgColor);
        lblBg.Style["background-color"] = "#" + Mp3Player.Settings.GetSingleValue(Mp3Player.Bg);
        lblLeftBg.Style["background-color"] = "#" + Mp3Player.Settings.GetSingleValue(Mp3Player.Leftbg);
        lblLeftIcon.Style["background-color"] = "#" + Mp3Player.Settings.GetSingleValue(Mp3Player.Lefticon);
        lblRightBg.Style["background-color"] = "#" + Mp3Player.Settings.GetSingleValue(Mp3Player.Rightbg);
        lblRightBgHvr.Style["background-color"] = "#" + Mp3Player.Settings.GetSingleValue(Mp3Player.Rightbghover);
        lblRightIcon.Style["background-color"] = "#" + Mp3Player.Settings.GetSingleValue(Mp3Player.Righticon);
        lblRightIconHvr.Style["background-color"] = "#" + Mp3Player.Settings.GetSingleValue(Mp3Player.Righticonhover);
        lblText.Style["background-color"] = "#" + Mp3Player.Settings.GetSingleValue(Mp3Player.Text);
        lblSlider.Style["background-color"] = "#" + Mp3Player.Settings.GetSingleValue(Mp3Player.Slider);
        lblTrack.Style["background-color"] = "#" + Mp3Player.Settings.GetSingleValue(Mp3Player.Track);
        lblBoarder.Style["background-color"] = "#" + Mp3Player.Settings.GetSingleValue(Mp3Player.Border);
        lblLoader.Style["background-color"] = "#" + Mp3Player.Settings.GetSingleValue(Mp3Player.Loader);
    }

    protected bool IsValidForm()
    {
        foreach (Control ctl in formContainer.Controls)
        {
            if (ctl.GetType().Name == "TextBox")
            {
                var box = (TextBox)ctl;

                if (box.Text.Trim().Length == 0)
                {
                    ErrorMsg.InnerHtml = "\"" + box.ID.Replace("_", " ") + "\" is a required field";
                    ErrorMsg.Visible = true;
                    break;
                }
                else
                {
                    if (box.ID == "Control_height" || box.ID == "Control_width")
                    {
                        if (!IsInteger(box.Text))
                        {
                            ErrorMsg.InnerHtml = "\"" + box.ID.Replace("_", " ") + "\" must be a number";
                            ErrorMsg.Visible = true;
                            break;
                        }
                    }
                }
            }
        }
        return ErrorMsg.InnerHtml.Length <= 0;
    }

    protected void SaveSettings()
    {
        Mp3Player.Settings.UpdateScalarValue(Mp3Player.Width, Control_width.Text);
        Mp3Player.Settings.UpdateScalarValue(Mp3Player.Height, Control_height.Text);
        Mp3Player.Settings.UpdateScalarValue(Mp3Player.BgColor, Contrlo_background.Text);
        Mp3Player.Settings.UpdateScalarValue(Mp3Player.Bg, Player_background.Text);
        Mp3Player.Settings.UpdateScalarValue(Mp3Player.Leftbg, Left_background.Text);
        Mp3Player.Settings.UpdateScalarValue(Mp3Player.Lefticon, Left_icon.Text);
        Mp3Player.Settings.UpdateScalarValue(Mp3Player.Rightbg, Right_background.Text);
        Mp3Player.Settings.UpdateScalarValue(Mp3Player.Rightbghover, Right_background_hover.Text);
        Mp3Player.Settings.UpdateScalarValue(Mp3Player.Righticon, Right_icon.Text);
        Mp3Player.Settings.UpdateScalarValue(Mp3Player.Righticonhover, Right_icon_hover.Text);
        Mp3Player.Settings.UpdateScalarValue(Mp3Player.Text, Text_color.Text);
        Mp3Player.Settings.UpdateScalarValue(Mp3Player.Slider, Slider.Text);
        Mp3Player.Settings.UpdateScalarValue(Mp3Player.Track, Track.Text);
        Mp3Player.Settings.UpdateScalarValue(Mp3Player.Border, Border.Text);
        Mp3Player.Settings.UpdateScalarValue(Mp3Player.Loader, Loader.Text);

        ExtensionManager.SaveSettings(Mp3Player.Ext, Mp3Player.Settings);
        Response.Redirect(Request.RawUrl);
    }

    protected void SetLabelColor(Label label, string color)
    {
        label.BackColor = System.Drawing.ColorTranslator.FromHtml("#" + color);
    }

    public static bool IsInteger(string theValue)
    {
        var isNumber = new Regex(@"^\d+$");
        var m = isNumber.Match(theValue);
        return m.Success;
    }

    protected void SetPlayer()
    {
        Mp3Player.AddJsToTheHeader();
        litPlayer.Text = Mp3Player.PlayerTag;
    }
</script>

<div class="content-box-outer">
    <div class="content-box-full">
        <div>
            <h1>Settings: Mp3 Flash Audio Player</h1>
            <div id="ErrorMsg" runat="server" style="color:Red; display:block;"></div>
            <div id="InfoMsg" runat="server" style="color:Green; display:block;"></div>
    
            <div style="margin-left:127px">
                <asp:Literal ID="litPlayer" runat="server"></asp:Literal>
            </div>
    
            <div id="formContainer" runat="server" class="mgr">
                <div ID="lblBgColor" style="width:120px; height:20px; float:left; margin-top: 8px" runat="server"></div>&nbsp;
                <asp:TextBox ID="Contrlo_background" runat="server" MaxLength="6"></asp:TextBox>
                <asp:Label ID="Label2" runat="server">Control background color</asp:Label>
                <br />
                <div ID="lblBg" style="width:120px; height:20px; float:left; margin-top: 8px" runat="server"></div>&nbsp;
                <asp:TextBox ID="Player_background" runat="server" MaxLength="6"></asp:TextBox>
                <asp:Label ID="Label14" runat="server">Player background color</asp:Label>
                <br />
                <div ID="lblLeftBg" style="width:120px; height:20px; float:left; margin-top: 8px" runat="server"></div>&nbsp;
                <asp:TextBox ID="Left_background" runat="server" MaxLength="6"></asp:TextBox>
                <asp:Label ID="Label15" runat="server">Left background color</asp:Label>
                <br />
                <div ID="lblRightBg" style="width:120px; height:20px; float:left; margin-top: 8px" runat="server"></div>&nbsp;
                <asp:TextBox ID="Right_background" runat="server" MaxLength="6"></asp:TextBox>
                <asp:Label ID="Label1" runat="server">Right background color</asp:Label>
                <br />
        
                <div ID="lblRightBgHvr" style="width:120px; height:20px; float:left; margin-top: 8px" runat="server"></div>&nbsp;
                <asp:TextBox ID="Right_background_hover" runat="server" MaxLength="6"></asp:TextBox>
                <asp:Label ID="Label3" runat="server">Right background (hover)</asp:Label>
                <br />
                <div ID="lblLeftIcon" style="width:120px; height:20px; float:left; margin-top: 8px" runat="server"></div>&nbsp;
                <asp:TextBox ID="Left_icon" runat="server" MaxLength="6"></asp:TextBox>
                <asp:Label ID="Label16" runat="server">Left icon color</asp:Label>
                <br />
                <div ID="lblRightIcon" style="width:120px; height:20px; float:left; margin-top: 8px" runat="server"></div>&nbsp;
                <asp:TextBox ID="Right_icon" runat="server" MaxLength="6"></asp:TextBox>
                <asp:Label ID="Label17" runat="server">Right icon color</asp:Label>
                <br />
                <div ID="lblRightIconHvr" style="width:120px; height:20px; float:left; margin-top: 8px" runat="server"></div>&nbsp;
                <asp:TextBox ID="Right_icon_hover" runat="server" MaxLength="6"></asp:TextBox>
                <asp:Label ID="Label18" runat="server">Right icon color (hover)</asp:Label>
                <br />
                <div ID="lblText" style="width:120px; height:20px; float:left; margin-top: 8px" runat="server"></div>&nbsp;
                <asp:TextBox ID="Text_color" runat="server" MaxLength="6"></asp:TextBox>
                <asp:Label ID="Label19" runat="server">Text color</asp:Label>
                <br />
                <div ID="lblSlider" style="width:120px; height:20px; float:left; margin-top: 8px" runat="server"></div>&nbsp;
                <asp:TextBox ID="Slider" runat="server" MaxLength="6"></asp:TextBox>
                <asp:Label ID="Label20" runat="server">Slider color</asp:Label>
                <br />
                <div ID="lblLoader" style="width:120px; height:20px; float:left; margin-top: 8px" runat="server"></div>&nbsp;
                <asp:TextBox ID="Loader" runat="server" MaxLength="6"></asp:TextBox>
                <asp:Label ID="Label21" runat="server">Loader bar color</asp:Label>
                <br />
                <div ID="lblTrack" style="width:120px; height:20px; float:left; margin-top: 8px" runat="server"></div>&nbsp;
                <asp:TextBox ID="Track" runat="server" MaxLength="6"></asp:TextBox>
                <asp:Label ID="Label22" runat="server">Progress track color</asp:Label>
                <br />
                <div ID="lblBoarder" style="width:120px; height:20px; float:left; margin-top: 8px" runat="server"></div>&nbsp;
                <asp:TextBox ID="Border" runat="server" MaxLength="6"></asp:TextBox>
                <asp:Label ID="Label23" runat="server">Progress track border color</asp:Label>
                <br />
                <div ID="Label4" style="width:120px; height:20px; float:left; margin-top: 8px" runat="server"></div>&nbsp;
                <asp:TextBox ID="Control_width" runat="server" MaxLength="4"></asp:TextBox>
                <asp:Label ID="Label5" runat="server">Control width</asp:Label>
                <br />
                <div ID="Label6" style="width:120px; height:20px; float:left; margin-top: 8px" runat="server"></div>&nbsp;
                <asp:TextBox ID="Control_height" runat="server" MaxLength="4"></asp:TextBox>
                <asp:Label ID="Label7" runat="server">Control height</asp:Label>
                <br />
            
                <br />
                <div style="margin-left:128px">
                    <asp:Button ID="btnSave" CssClass="btn primary" runat="server" onclick="BtnSaveClick" />
                </div>
            </div>
        </div>
    </div>
</div>