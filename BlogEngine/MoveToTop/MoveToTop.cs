    #region Usings
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using BlogEngine.Core;
    using BlogEngine.Core.Web.Controls;
    using BlogEngine.Core.Web.Extensions;
    using System.Xml.Linq;
    using System.Web.Hosting;
    using System.IO;
    using System.Text;
    using System.Web.Configuration;
    using System.Configuration;
    using System.Security.Permissions;
    using System.Collections.Specialized;
    #endregion

    #region Extension Class
    [Extension("Creates a Scroll To Top button widget.", "3.0.0.1", "ReviewResults.in", 1)]
    public class MoveToTop
    {
        #region Properties & Fields
        /// <summary>
        /// Gets or sets the settings.
        /// </summary>
        /// <value>The settings.</value>
        protected static ExtensionSettings Settings { get; set; }
        static string _MTImage = Utils.RelativeWebRoot + "Custom/Extensions/MoveToTop/MovetoTop.png";
        #endregion

        #region Ctor
        /// <summary>
        /// initializes our Post.Serving & Page.Serving event handlers, and builds the required settings.
        /// </summary>
        public MoveToTop()
        {
            Post.Serving += new EventHandler<ServingEventArgs>(Post_Serving);
            Page.Serving += new EventHandler<ServingEventArgs>(Page_Serving);
            buildSettings();
        }
        #endregion

        #region Subscribed Events
        /// <summary>
        /// event for our page serving
        /// </summary>
        /// <param name="sender">the page</param>
        /// <param name="e">the event args</param>
        void Page_Serving(object sender, ServingEventArgs e)
        {
            if (HttpContext.Current == null)
                return;
            var ctx = HttpContext.Current;
              var post = (Page)sender;
            AppendContext(ctx);
        }


        /// <summary>
        /// event method for the post serving
        /// </summary>
        /// <param name="sender">the post</param>
        /// <param name="e">the event args</param>
        void Post_Serving(object sender, ServingEventArgs e)
        {
            if (HttpContext.Current == null)
                return;
            var post = (Post)sender;
            var ctx = HttpContext.Current;
            AppendContext(ctx);
        }
        #endregion

        #region Methods

        /// <summary>
        /// creates the widgets HTML and required script(s)
        /// </summary>
        /// <param name="ctx">the http context</param>
        private void AppendContext(HttpContext ctx)
        {
            var p = new System.Web.UI.Page();
            try
            {
                p = (System.Web.UI.Page)ctx.Handler;
            }
            catch { return; }
            //if MoveToTop already added to the script manager then we need to return as we only want one per page.
            if (p.ClientScript.IsClientScriptBlockRegistered("MoveToTop"))
                return;
            StringBuilder sb = new StringBuilder();
            _MTImage = Utils.RelativeWebRoot + Settings.GetSingleValue("MTImagePath");

            sb.Append(@"
                <script type='text/javascript' >
                var scrolltotop = {
                    setting: { startline: 100, scrollto: 0, scrollduration: 1000, fadeduration: [500, 100] },
                    controlHTML: '<img runat=""server"" src=""");
                        sb.Append(_MTImage);
                        sb.Append(@"""/>',
                    controlattrs: { offsetx: 5, offsety: 5 }, 
                    anchorkeyword: '#top', 
                    state: { isvisible: false, shouldvisible: false },
                    scrollup: function () {
                        if (!this.cssfixedsupport) 
                            this.$control.css({ opacity: 0 }) 
                        var dest = isNaN(this.setting.scrollto) ? this.setting.scrollto : parseInt(this.setting.scrollto)
                        if (typeof dest == 'string' && jQuery('#' + dest).length == 1) 
                            dest = jQuery('#' + dest).offset().top
                        else
                            dest = 0
                        this.$body.animate({ scrollTop: dest }, this.setting.scrollduration);
                    },
                    keepfixed: function () {
                        var $window = jQuery(window)
                        var controlx = $window.scrollLeft() + $window.width() - this.$control.width() - this.controlattrs.offsetx
                        var controly = $window.scrollTop() + $window.height() - this.$control.height() - this.controlattrs.offsety
                        this.$control.css({ left: controlx + 'px', top: controly + 'px' })
                    },
                    togglecontrol: function () {
                        var scrolltop = jQuery(window).scrollTop()
                        if (!this.cssfixedsupport)
                            this.keepfixed()
                        this.state.shouldvisible = (scrolltop >= this.setting.startline) ? true : false
                        if (this.state.shouldvisible && !this.state.isvisible) {
                            this.$control.stop().animate({ opacity: 1 }, this.setting.fadeduration[0])
                            this.state.isvisible = true
                        }
                        else if (this.state.shouldvisible == false && this.state.isvisible) {
                            this.$control.stop().animate({ opacity: 0 }, this.setting.fadeduration[1])
                            this.state.isvisible = false
                        }
                    },

                    init: function () {
                        jQuery(document).ready(function ($) {
                            var mainobj = scrolltotop
                            var iebrws = document.all
                            mainobj.cssfixedsupport = !iebrws || iebrws && document.compatMode == 'CSS1Compat' && window.XMLHttpRequest 
                            mainobj.$body = (window.opera) ? (document.compatMode == 'CSS1Compat' ? $('html') : $('body')) : $('html,body')
                            mainobj.$control = $('<div id=""topcontrol"">' + mainobj.controlHTML + '</div>')
                            .css({ position: mainobj.cssfixedsupport ? 'fixed' : 'absolute', bottom: mainobj.controlattrs.offsety, right: mainobj.controlattrs.offsetx, opacity: 0, cursor: 'pointer' })
                            .attr({ title: 'Scroll Back to Top' })
                            .click(function () { mainobj.scrollup(); return false })
                            .appendTo('body')
                            if (document.all && !window.XMLHttpRequest && mainobj.$control.text() != '') 
                                mainobj.$control.css({ width: mainobj.$control.width() }) 
                            mainobj.togglecontrol()
                            $('a[href="""" + mainobj.anchorkeyword + """"]').click(function () {
                                mainobj.scrollup()
                                return false
                            })
                            $(window).bind('scroll resize', function (e) {
                                mainobj.togglecontrol()
                            })
                        })
                    }
                }
                scrolltotop.init()
            </script>");
            p.ClientScript.RegisterClientScriptBlock(p.GetType(), "MoveToTop", sb.ToString(), false);

        }

        /// <summary>
        /// Builds the main settings, this creates the various locations
        /// </summary>
        private void buildSettings()
        {
            ExtensionSettings settings = new ExtensionSettings(this);
            settings.IsScalar = true;
            settings.AddParameter("MTImagePath", "Floating Move To Top Image Path", 2000, true, true, ParameterType.String);
            settings.AddValue("MTImagePath", "Custom/Extensions/MoveToTop/MovetoTop.png"); 
            Settings = ExtensionManager.InitSettings(this.GetType().Name, settings);
        }
        #endregion
    }
    #endregion
