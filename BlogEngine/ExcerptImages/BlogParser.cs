using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using BlogEngine.Core;
using BlogEngine.Core.Web.Controls;
using BlogEngine.Core.Web.Extensions;
using System.Text.RegularExpressions;
/// <summary>
/// Author : John Thomas, Johns PC Repair, http://www.johnpcrepair.com.
/// Designed for BlogEngine.NET version 2.5 using .NET Framework 4.
/// </summary>
[Extension("Parses the Html in blog posts & pages as they are viewed. Used by other Extensions.", "1.1", "John Thomas")]
public class BlogParser
{
    #region Constants

    const string _ExtensionName = "BlogParser";
    const string _ExtensionAdminPath = "~/admin/Extensions/BlogParser/BlogParserAdmin.aspx";

    #endregion // Constants

    #region Fields

    // ----------------------------------------------------------------------
    /// <summary>
    /// A list of extensions that are subscribed to events from BlogParser
    /// </summary>
    static List<Subscriber> _Subscribers = new List<Subscriber>();

    // ----------------------------------------------------------------------
    // Settings variables
    static ExtensionSettings _Settings;
    static bool _SettingsInitialized = false;

    // ----------------------------------------------------------------------
    /// <summary>
    /// Used for multithread locking
    /// </summary>
    static object _Sync = new object();

    // ----------------------------------------------------------------------
    /// <summary>
    /// Defines tags that have no inner Html
    /// </summary>
    static string[] SelfClosingTags = new string[] { "img", "br", "/br", "br/" };

    #endregion // Fields
    
    #region Public Methods
    // ----------------------------------------------------------------------
    /// <summary>
    /// This is called to update the order that
    /// Extensions receive events. This is normally
    /// used internally and by the admin page.
    /// </summary>
    public static void UpdatePriorities()
    {
        _Subscribers.Sort(delegate(Subscriber s1, Subscriber s2) { return s1.Priority.CompareTo(s2.Priority); });
        
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// Registers a plugin with BlogParser
    /// </summary>
    /// <param name="pluginName">The name to register the plugin under</param>
    /// <param name="subscriber">The Plugin</param>
    /// <returns>True if added, false if another matching name was already registered</returns>
    public static bool RegisterSubscriber(string pluginName, ISubscriber subscriber)
    {
        for (int i = 0; i < _Subscribers.Count; i++)
        {
            if (String.Compare(_Subscribers[i].Name, pluginName, true) == 0)
            {
                return false;
            }
        }
        Subscriber s = new Subscriber(pluginName, subscriber);
        _Subscribers.Add(s);
        UpdatePriorities();
        return true;
    }

    // ----------------------------------------------------------------------
    public static ExtensionSettings GetSettings()
    {
        ExtensionSettings extensionSettings = new ExtensionSettings(_ExtensionName);
        extensionSettings.ShowAdd = false;
        extensionSettings.ShowDelete = false;
        extensionSettings.ShowEdit = false;
        extensionSettings.AddParameter("ExtensionName", "Extension Name", 20, true, false, ParameterType.String);
        extensionSettings.AddParameter("Priority", "Priority", 3, true, false, ParameterType.Integer);
        foreach (Subscriber s in _Subscribers)
        {
            extensionSettings.AddValues(new string[] { s.Name, s.Priority.ToString() });
        }
        return extensionSettings;
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// Removes a subscriber from BlogParser
    /// </summary>
    /// <param name="pluginName">The subscribers registered name</param>
    /// <returns>True if found and removed, otherwise false</returns>
    public static bool RemoveSubscriber(string pluginName)
    {
        for (int i = 0; i < _Subscribers.Count; i++)
        {
            if (String.Compare(_Subscribers[i].Name, pluginName, true) == 0)
            {
                _Subscribers.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// Add a setting for a subscriber
    /// </summary>
    /// <param name="pluginName">The subscriber registered name</param>
    /// <param name="settingName">The name of the setting</param>
    /// <param name="values">The values for the setting</param>
    public static void AddSubscriberSetting(string pluginName, string settingName, params string[] values)
    {
        for (int i = 0; i < _Subscribers.Count; i++)
        {
            if (String.Compare(_Subscribers[i].Name, pluginName, true) == 0)
            {
                _Subscribers[i].Add(new SubscriberSetting(settingName, values));
            }
        }
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// Replaces Linq's array.Contains<> so that it 
    /// is easier to make this class backward
    /// compatible.
    /// </summary>
    public static bool ArrayContainsString(IEnumerable<string> arry, string str)
    {
        return ArrayContainsString(arry, str, false);
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// Replaces Linq's array.Contains<> so that it 
    /// is easier to make this class backward
    /// compatible.
    /// </summary>
    public static bool ArrayContainsString(IEnumerable<string> arry, string str, bool ignoreCase)
    {
        foreach (string s in arry)
            if (String.Compare(s, str, ignoreCase) == 0)
                return true;
        return false;
    }

    public static bool IsSelfClosingTag(IHtmlTag tag)
    {
        if (ArrayContainsString(SelfClosingTags, tag.Name, true))
            return true;

        if ((tag as HtmlTag).ContainsAttribute("/"))
            return true;
        if (tag.Name.Contains("/"))
            return true;

        return false;
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// Parses html content into Document Object Models
    /// </summary>
    public static HtmlDom ParseForDoms(string content)
    {
        HtmlDom objectsParsed = new HtmlDom();
        objectsParsed.OriginalDocLength = content.Length;
        BlogParser.HtmlParser parser = new BlogParser.HtmlParser(content);
        StringBuilder output = new StringBuilder(content.Length / 2);
        while (!parser.EndOfSource)
        {
            char ch = parser.Parse();
            if (ch == (char)0)
            {
                if (output.Length > 0)
                {
                    objectsParsed.Add(output.ToString());
                    output.Length = 0;
                }
                HtmlTag tag = parser.GetTag() as BlogParser.HtmlTag;
                if (!IsSelfClosingTag(tag))
                {
                    tag.InnerHtml.AddRange(ParseForDomsRecursive(tag.Name, output, parser));
                }
                objectsParsed.Add(tag);
            }
            else
            {
                output.Append(ch);
            }
        }
        if (output.Length > 0)
            objectsParsed.Add(output.ToString());
        return objectsParsed;
    }

    #endregion // Public Methods

    #region Protected Methods

    // ----------------------------------------------------------------------
    /// <summary>
    /// Gets the extension settings.
    /// </summary>
    /// <value>The settings.</value>
    protected static ExtensionSettings Settings
    {
        get
        {
            if (!_SettingsInitialized)
            {
                lock (_Sync)
                {
                    // create settings object. You need to pass exactly your
                    // extension class name (case sencitive)
                    ExtensionSettings extensionSettings = new ExtensionSettings(_ExtensionName);
                    extensionSettings.ShowAdd = false;
                    extensionSettings.ShowDelete = false;
                    extensionSettings.ShowEdit = false;
                    extensionSettings.AddParameter("ExtensionName", "Extension Name", 20, true, false, ParameterType.String);
                    extensionSettings.AddParameter("Priority", "Priority", 3, true, false, ParameterType.Integer);

                    //                        extensionSettings.Help = "Converts BBCode to XHTML in the comments. Close tag is optional.";

                    // ------------------------------------------------------
                    ExtensionManager.ImportSettings(extensionSettings);
                    _Settings = ExtensionManager.GetSettings(_ExtensionName);
                }
                _SettingsInitialized = true;
            }

            return _Settings;
        }
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// Page.Serving event
    /// </summary>
    protected static void Page_Serving(object sender, ServingEventArgs e)
    {
        DoParseEvents(sender, e);
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// Post.Serving event
    /// </summary>
    protected static void Post_Serving(object sender, ServingEventArgs e)
    {
        DoParseEvents(sender, e);
    }

    #endregion // Protected Methods

    #region Private Methods

    // ----------------------------------------------------------------------
    private static void NotifySubscribersTagFound(HtmlTagArgs args)
    {
        for (int i = 0; i < _Subscribers.Count; i++)
        {
            SubscriberSetting setting = _Subscribers[i]["SubscribeToTag"];
            if (setting != null && setting.Values != null && setting.Values.Length > 0)
            {
                for (int j = 0; j < setting.Values.Length; j++)
                {
                    if (String.Compare(setting.Values[j], args.Tag.Name, true) == 0)
                        _Subscribers[i].Instance.TagFound(args);
                }
            }
        }
    }

    // ----------------------------------------------------------------------
    private static void NotifySubscribersDomReady(HtmlDomArgs args, ServingEventArgs e)
    {
        for (int i = 0; i < _Subscribers.Count; i++)
        {
            SubscriberSetting setting = _Subscribers[i]["SubscribeToDom"];
            if (setting != null && setting.Values != null && setting.Values.Length > 0)
            {
                bool notify = false;
                if (ArrayContainsString(setting.Values, "Email", true) && args.Location == ServingLocation.Email)
                    notify = true;
                else if (ArrayContainsString(setting.Values, "Feed", true) && args.Location == ServingLocation.Feed)
                    notify = true;
                else if (ArrayContainsString(setting.Values, "None", true) && args.Location == ServingLocation.None)
                    notify = true;
                else if (ArrayContainsString(setting.Values, "Other", true) && args.Location == ServingLocation.Other)
                    notify = true;
                else if (ArrayContainsString(setting.Values, "PostList", true) && args.Location == ServingLocation.PostList)
                    notify = true;
                else if (ArrayContainsString(setting.Values, "SinglePage", true) && args.Location == ServingLocation.SinglePage)
                    notify = true;
                else if (ArrayContainsString(setting.Values, "SinglePost", true) && args.Location == ServingLocation.SinglePost)
                    notify = true;

                if (notify)
                {
                    _Subscribers[i].Instance.DomReady(args);
                    if (args.Cancel) break;
                }
            }
        }
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// Parses pages and post and retrieves Doms. Serves
    /// other extensions via events.
    /// </summary>
    private static void DoParseEvents(object sender, ServingEventArgs e)
    {
        if (!ExtensionManager.ExtensionEnabled(_ExtensionName))
            return;

        BusinessBase<Post, Guid> page = sender as BusinessBase<Post, Guid>;

        HtmlDom doms = null;
        try
        {
            doms = ParseForDoms(e.Body);
        }
        catch (StackOverflowException)
        {
        }
        if (doms != null)
        {
            HtmlDomArgs args = new HtmlDomArgs(page, e, doms);
            NotifySubscribersDomReady(args, e);

            // TODO: dont parse for tags if no one is subscribed to tags
            ParseForTags(page, e, doms);
            e.Body = args.Dom.ToString();
        }
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// Recursive method for ParseForDom method
    /// </summary>
    private static HtmlDom ParseForDomsRecursive(string closeTag, StringBuilder output, HtmlParser parser)
    {
        HtmlDom objectsParsed = new HtmlDom();
        while (!parser.EndOfSource)
        {
            char ch = parser.Parse();
            if (ch == (char)0)
            {
                if (output.Length > 0)
                {
                    objectsParsed.Add(output.ToString());
                    output.Length = 0;
                }
                HtmlTag tag = parser.GetTag() as BlogParser.HtmlTag;
                if (IsClosingTagCompare(tag.Name, closeTag))
                {
                    break;
                }
                if (!IsSelfClosingTag(tag))
                {
                    tag.InnerHtml.AddRange(ParseForDomsRecursive(tag.Name, output, parser));
                }
                objectsParsed.Add(tag);
            }
            else
            {
                output.Append(ch);
            }
        }
        return objectsParsed;
    }

    private static bool IsClosingTagCompare(string tagName, string searchTag)
    {
        bool isClosing = false;
        if (!String.IsNullOrEmpty(tagName))
            isClosing = tagName[0] == '/';

        if (string.Compare(Regex.Replace(tagName, @"[\W]", ""), searchTag, true) == 0)
            return isClosing;

        return false;
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// Parses Dom for tags and activates TagFound
    /// event when a tag is found.
    /// </summary>
    /// <param name="post">original event sender</param>
    /// <param name="e">Serving events</param>
    private static void ParseForTags(BusinessBase<Post, Guid> post, ServingEventArgs e, HtmlDom dom)
    {
        HtmlTag tag = null;
        foreach (Object obj in dom)
        {
            tag = obj as HtmlTag;
            if (tag != null)
            {
                if (tag.InnerHtml != null)
                {
                    ParseForTagsRecursive(post, e, dom, tag.InnerHtml);
                }
                NotifySubscribersTagFound(new HtmlTagArgs(post, e, tag, dom));
            }

        }
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// Recursive method for ParseForTags method
    /// </summary>
    private static void ParseForTagsRecursive(BusinessBase<Post, Guid> post, ServingEventArgs e, HtmlDom dom, HtmlDom innerHtml)
    {
        HtmlTag tag = null;
        foreach (Object obj in innerHtml)
        {
            tag = obj as HtmlTag;
            if (tag != null)
            {
                if (tag.InnerHtml != null)
                {
                    ParseForTagsRecursive(post, e, dom, tag.InnerHtml);
                }
                NotifySubscribersTagFound(new HtmlTagArgs(post, e, tag, dom));
            }

        }
    }

    #endregion // Private Methods

    #region Constructor

    // ----------------------------------------------------------------------
    /// <summary>
    /// Static constructor
    /// </summary>
    static BlogParser()
    {
        Post.Serving += new EventHandler<ServingEventArgs>(Post_Serving);
        Page.Serving += new EventHandler<ServingEventArgs>(Page_Serving);
        ExtensionManager.SetAdminPage(_ExtensionName, _ExtensionAdminPath);
        ExtensionSettings s = Settings;

    }

    #endregion // Constructor

    #region Classes

    // ----------------------------------------------------------------------
    /// <summary>
    /// Tag Arguments for TagFound event
    /// </summary>
    public class HtmlTagArgs : HtmlDomArgs
    {
        #region Fields

        protected HtmlTag _Tag;

        #endregion // Fields

        #region Public Properties

        // ----------------------------------------------------------------------
        /// <summary>
        /// Tag object found by parser
        /// </summary>
        public HtmlTag Tag
        {
            get { return _Tag; }
            private set { _Tag = value; }
        }

        #endregion // Public Properties

        #region Constructor

        // ----------------------------------------------------------------------
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="post">Sender from the original serving event</param>
        /// <param name="tag">Tag found by parser</param>
        /// <param name="servingEvent">Event args from the original serving event</param>
        public HtmlTagArgs(BusinessBase<Post, Guid> post, ServingEventArgs servingEvent, IHtmlTag tag, HtmlDom dom)
        {
            _Post = post;
            _Tag = tag as HtmlTag;
            _ServingEvent = servingEvent;
            _Dom = dom;
        }

        #endregion // Constructor
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// Dom Arguments for Dom related events
    /// </summary>
    public class HtmlDomArgs
    {
        #region Fields

        protected BusinessBase<Post, Guid> _Post;
        protected HtmlDom _Dom;
        protected ServingEventArgs _ServingEvent;

        #endregion // Fields

        #region Public Properties

        // ----------------------------------------------------------------------
        /// <summary>
        /// Sender from the original serving event
        /// </summary>
        public BusinessBase<Post, Guid> Post
        {
            get { return _Post; }
            private set { _Post = value; }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Document object model 
        /// </summary>
        public HtmlDom Dom
        {
            get { return _Dom; }
            private set { _Dom = value; }
        }
        
        // ----------------------------------------------------------------------
        /// <summary>
        /// Gets or sets a value of whether or not
        /// the served item should be cancelled or
        /// not. If it is cancelled, it will not be
        /// displayed.
        /// </summary>
        public bool Cancel
        {
            get { return _ServingEvent.Cancel; }
            set { _ServingEvent.Cancel = value; }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Gets the Body of the post as it was
        /// when BlogParser parsed it. If you need
        /// to make changes to the body, you must do
        /// it using the Dom property
        /// </summary>
        public string Body
        {
            get { return _ServingEvent.Body; }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Gets or sets the criteria by which the content is served
        /// </summary>
        public ServingContentBy ContentBy
        {
            get { return _ServingEvent.ContentBy; }
            set { _ServingEvent.ContentBy = value; }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Gets or sets the serving event location
        /// </summary>
        public ServingLocation Location
        {
            get { return _ServingEvent.Location; }
            set { _ServingEvent.Location = value; }
        }

        #endregion // Public Properties

        #region Constructors

        // ----------------------------------------------------------------------
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="post">Sender from the original serving event</param>
        /// <param name="dom">Dom constructed by the parser</param>
        /// <param name="servingEvent">Event args from the original serving event</param>
        public HtmlDomArgs(BusinessBase<Post, Guid> post, ServingEventArgs servingEvent, HtmlDom dom)
        {
            Post = post;
            Dom = dom;
            _ServingEvent = servingEvent;
            
        }
        
        public HtmlDomArgs() { }

        #endregion // Constructors
    }
    
    // ----------------------------------------------------------------------
    /// <summary>
    /// Parses Html documents
    /// </summary>
    public class HtmlParser : HtmlTag
    {
        #region Fields

        // ----------------------------------------------------------------------
        protected string _Source, _CurrentAttributeName, _CurrentAttributeValue, _CurrentTag;
        int _IndexPosition;
        char _CurrentDelimeter;
        int _CurrentTagIndex = 0;

        #endregion // Fields

        #region Public Methods

        // ----------------------------------------------------------------------
        /// <summary>
        /// Returns the most recently parsed Html tag
        /// as an HtmlTag object.
        /// </summary>
        public HtmlTag GetTag()
        {
            HtmlTag tag = new HtmlTag(_CurrentTag, _CurrentTagIndex);
            tag.Name = _CurrentTag;
            foreach (IHtmlAttribute attribute in AttributeList) tag.Add(attribute.ToHtmlAttribute());
            return tag;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Returns the next character in the Html document.
        /// If a tag is found, returns (Char)0.
        /// </summary>
        public char Parse()
        {
            if (CurrentChar() == '<')
            {
                Advance();
                ParseTag();
                return (char)0;
            }
            else
            {
                char ch = CurrentChar();
                Advance();
                return ch;
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Returns true if the end of the Html
        /// document is reached.
        /// </summary>
        public bool EndOfSource
        {
            get { return (_IndexPosition >= _Source.Length); }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Gets the current index position of the parser
        /// </summary>
        public int IndexPosition
        {
            get { return _IndexPosition; }
        }

        #endregion // Public Methods

        #region Private Methods

        // ----------------------------------------------------------------------
        private void ParseTag()
        {
            _CurrentTag = String.Empty;
            Clear();
            bool tagEnded = false;
            while (!EndOfSource)
            {
                if (IsEmptySpace(CurrentChar()))
                    break;
                if (CurrentChar() == '>')
                {
                    tagEnded = true;
                    break;
                }
                _CurrentTag += CurrentChar();
                Advance();
            }
            _CurrentTag = _CurrentTag.ToLower();
            Advance();
            if (!tagEnded)
            {
                SkipEmpty();
                while (!EndOfSource)
                {
                    if (CurrentChar() == '>') break;
                    _CurrentAttributeName = String.Empty;
                    _CurrentAttributeValue = String.Empty;
                    _CurrentDelimeter = (char)0;
                    ParseAttributeName();
                    if (CurrentChar() == '>') { Add(new HtmlAttribute(_CurrentAttributeName, _CurrentAttributeValue)); break; }
                    ParseAttributeValue();
                    Add(new HtmlAttribute(_CurrentAttributeName, _CurrentAttributeValue));
                }
            }
            if (!tagEnded) Advance();
            _CurrentTagIndex++;
        }

        // ----------------------------------------------------------------------
        private void SkipEmpty()
        {
            while (!EndOfSource)
            {
                if (!IsEmptySpace(CurrentChar())) return;
                Advance();
            }
        }

        // ----------------------------------------------------------------------
        private void ParseAttributeName()
        {
            SkipEmpty();
            while (!EndOfSource)
            {
                if (CurrentChar() == '=' || CurrentChar() == '>') break;
                _CurrentAttributeName += CurrentChar();
                Advance();
            }
            _CurrentAttributeName = _CurrentAttributeName.ToLower();
            SkipEmpty();
        }

        // ----------------------------------------------------------------------
        private void ParseAttributeValue()
        {
            if (_CurrentDelimeter != 0) return;
            if (CurrentChar() == '=')
            {
                _IndexPosition++;
                SkipEmpty();
                if (CurrentChar() == '\'' || CurrentChar() == '\"')
                {
                    _CurrentDelimeter = CurrentChar();
                    Advance();
                    while (!EndOfSource && CurrentChar() != _CurrentDelimeter)
                    {
                        _CurrentAttributeValue += CurrentChar();
                        Advance();
                    }
                    Advance();
                }
                else
                {
                    while (!EndOfSource && CurrentChar() != '>' && !IsEmptySpace(CurrentChar()))
                    {
                        _CurrentAttributeValue += CurrentChar();
                        Advance();
                    }
                }
                SkipEmpty();
            }
        }

        // ----------------------------------------------------------------------
        private void Advance() { _IndexPosition++; }

        // ----------------------------------------------------------------------
        private char CurrentChar() { return _Source[_IndexPosition]; }

        // ----------------------------------------------------------------------
        private bool IsEmptySpace(char ch) { return ("\t\n\r ".IndexOf(ch) != -1); }

        #endregion // Private Methods

        #region Constructor

        // ----------------------------------------------------------------------
        public HtmlParser(string source) { _Source = source; }

        #endregion // Constructor
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// A list of Html objects that can be
    /// either a string of text or an IHtmlTag
    /// </summary>
    public class HtmlDom : ICloneable, IEnumerator, IEnumerable
    {
        #region Fields

        protected List<object> _Objects = new List<object>();
        int _OriginalDocLength;

        #endregion // Fields

        #region Public Properties

        // ----------------------------------------------------------------------
        /// <summary>
        /// Gets the number of Html objects
        /// </summary>
        public int Count
        {
            get { return _Objects.Count; }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Gets the Html object at the specified index
        /// </summary>
        /// <param name="index">The index of the Html object</param>
        /// <returns>Returns a string text or IHtmlTag object</returns>
        public object this[int index]
        {
            get { return _Objects[index]; }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Set this to the original docs length to 
        /// increase performance when rebuilding the html
        /// </summary>
        public int OriginalDocLength
        {
            get { return _OriginalDocLength; }
            set { _OriginalDocLength = value; }
        }

        #endregion // Public Properties

        #region Public Methods

        // ----------------------------------------------------------------------
        /// <summary>
        /// Add the elements of another dom
        /// </summary>
        /// <param name="tag">a BlogParserDom object to add</param>
        public void Add(HtmlDom dom)
        {
            foreach (object obj in dom)
            {
                if (IsTag(obj))
                    _Objects.Add(obj as HtmlTag);
                else
                    _Objects.Add(obj as string);
            }
        }

        /// <summary>
        /// Adds text to the Html object list
        /// </summary>
        /// <param name="text">The string text to add</param>
        public void Add(string text)
        {
            _Objects.Add(new HtmlText(text));
        }

        /// <summary>
        /// Adds text to the Html object list
        /// </summary>
        /// <param name="text">The string text to add</param>
        public void Add(HtmlText text)
        {
            _Objects.Add(text);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Adds a range of Html objects from another HtmlObjects list
        /// </summary>
        /// <param name="objects">An HtmlObjects</param>
        public void AddRange(HtmlDom objects)
        {
            foreach (Object obj in objects)
            {
                _Objects.Add(obj);
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Adds an Html tag to the Html object list
        /// </summary>
        /// <param name="tag">The Html tag to add</param>
        public void Add(IHtmlTag tag)
        {
            _Objects.Add(tag);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Inserts an IHtmlTag at the specified index
        /// </summary>
        /// <param name="index">the index to insert at</param>
        /// <param name="tag">The IHtmlTag to insert</param>
        public void Insert(int index, IHtmlTag tag)
        {
            _Objects.Insert(index, tag);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Inserts an IHtmlText at the specified index
        /// </summary>
        /// <param name="index">the index to insert at</param>
        /// <param name="tag">The IHtmlText to insert</param>
        public void Insert(int index, IHtmlText text)
        {
            _Objects.Insert(index, text);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Inserts a string (as an IHtmlText) at the specified index
        /// </summary>
        /// <param name="index">the index to insert at</param>
        /// <param name="tag">The string to insert</param>
        public void Insert(int index, string text)
        {
            _Objects.Insert(index, new HtmlText(text));
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Removes an Html object
        /// </summary>
        /// <param name="index">The index location of the object to remove</param>
        public void RemoveAt(int index)
        {
            _Objects.RemoveAt(index);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Determine if an object is an HtmlTag object
        /// </summary>
        /// <param name="domObj">The object to test</param>
        /// <returns>True if the object is an HtmlTag, false if it is not</returns>
        public static bool IsTag(object domObj)
        {
            if (domObj is IHtmlTag)
                return true;
            else
                return false;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Gets a tag by its index
        /// </summary>
        /// <param name="dom">The HtmlDom to search in</param>
        /// <param name="index">The index of the tag to find</param>
        /// <returns>The HtmlTag with the specified index, null if none found</returns>
        public static HtmlTag GetTag(HtmlDom dom, int index)
        {
            HtmlTag tagr = null;
            foreach (object obj in dom)
            {
                IHtmlTag tag = obj as IHtmlTag;
                if (tag != null)
                {
                    if (tag.TagIndex == index)
                        return tag as HtmlTag;
                    else if (tag.InnerHtml != null)
                    {
                        tagr = GetTag(tag.InnerHtml, index);
                        if (tagr != null) return tagr;
                    }
                }
            }
            return null;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Gets a list of tags by tag name
        /// </summary>
        /// <param name="dom">The HtmlDom to search in</param>
        /// <param name="name">The tag name to test against</param>
        /// <returns>List of tags</returns>
        public static List<HtmlTag> GetTagByTagName(HtmlDom dom, string name)
        {
            List<HtmlTag> tags = new List<HtmlTag>();
            foreach (object obj in dom)
            {
                IHtmlTag tag = obj as IHtmlTag;
                if (tag != null)
                {
                    if (String.Compare(tag.Name, name, true) == 0)
                        tags.Add(tag as HtmlTag);

                    if (tag.InnerHtml != null)
                    {
                        tags.AddRange(GetTagByTagName(tag.InnerHtml, name));
                    }
                }
            }
            return tags;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Gets a tag by its Id attribute
        /// </summary>
        /// <param name="dom">The HtmlDom to search in</param>
        /// <param name="id">The id to find</param>
        /// <param name="ignoreCase">set if the case sensitivity is important or not</param>
        /// <returns>Returns an HtmlTag with the specified Id attribute or null</returns>
        public static HtmlTag GetTagByIdAttribute(HtmlDom dom, string id, bool ignoreCase)
        {
            HtmlTag tagr = null;
            foreach (object obj in dom)
            {
                IHtmlTag tag = obj as IHtmlTag;
                if (tag != null)
                {
                    if (String.Compare(tag["id"].Value, id, ignoreCase) == 0)
                        return tag as HtmlTag;
                    else if (tag.InnerHtml != null)
                    {
                        tagr = GetTagByIdAttribute(tag.InnerHtml, id, ignoreCase);
                        if (tagr != null) return tagr;
                    }
                }
            }
            return null;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Create a clone of this object instance
        /// </summary>
        /// <returns>Returns the cloned object instance.</returns>
        public object Clone()
        {
            HtmlDom dom = new HtmlDom();
            foreach (object obj in this._Objects)
            {
                HtmlTag tagToClone = obj as HtmlTag;
                if (IsTag(obj))
                {
                    IHtmlTag tag = tagToClone.Clone() as HtmlTag;
                    if (tagToClone.InnerHtml != null)
                    {
                        tag.InnerHtml.AddRange(tagToClone.InnerHtml.Clone() as HtmlDom);
                    }
                    dom.Add(tag);
                }
                else
                {
                    string text = obj as String;
                    dom.Add(text);
                }
            }
            return dom;
        }

        // ----------------------------------------------------------------------        
        /// <summary>
        /// Create a string Html document from the Dom
        /// </summary>
        /// <returns>Returns the Dom as an html document string</returns>
        public override string ToString()
        {
            StringBuilder output = new StringBuilder(_OriginalDocLength);
            for (int i = 0; i < Count; i++)
            {
                HtmlTag tag = _Objects[i] as HtmlTag;
                if (tag != null)
                {
                    output.Append(tag.Html);
                    if (!IsSelfClosingTag(tag) && !tag.IsModifiedOutput)
                    {
                        if (tag.InnerHtml != null)
                            output.Append(tag.InnerHtml.ToString());
                        output.Append("</" + tag.Name + ">");
                    }

                }
                else
                {
                    HtmlText text = _Objects[i] as HtmlText;
                    if (text != null)
                        output.Append(text.Text);
                }
            }
            string test = output.ToString();
            return test;
        }

        #endregion // Public Methods

        #region IEnumerator, IEnumerable Interface Implementation

        // ----------------------------------------------------------------------
        // IEnumerator Stuff
        private int position = -1;

        // ----------------------------------------------------------------------
        public object Current
        {
            get
            {
                return _Objects[position];
            }
        }

        // ----------------------------------------------------------------------
        public bool MoveNext()
        {
            position++;
            return (position < _Objects.Count);
        }

        // ----------------------------------------------------------------------
        public void Reset()
        {
            position = -1;
        }

        // ----------------------------------------------------------------------
        //IEnumerable stuff
        public IEnumerator GetEnumerator()
        {
            return (IEnumerator)this;
        }

        #endregion // IEnumerator, IEnumerable Interface Implementation
    }


    // ----------------------------------------------------------------------
    /// <summary>
    /// Represents an html tag
    /// </summary>
    public class HtmlTag : IHtmlTag, ICloneable
    {
        #region Fields

        protected string _Name, _AltHtml;
        protected int _TagIndex;
        protected string[] _AttributesAllowedEmpty;
        protected HtmlDom _InnerHtml;
        protected Guid _Id; 
        protected List<IHtmlAttribute> _AttributeList;

        #endregion // Fields

        #region Public Properties
        // ----------------------------------------------------------------------
        /// <summary>
        /// The name of this tag
        /// </summary>
        public virtual string Name
        {
            get { return _Name; }
            set { _Name = value; }
        }
        
        // ----------------------------------------------------------------------
        /// <summary>
        /// The index order of the tag within a document
        /// </summary>
        public virtual int TagIndex
        {
            get { return _TagIndex; }
            private set { _TagIndex = value; }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Gets the unique identifier for this tag
        /// </summary>
        public virtual Guid Id
        {
            get { return _Id; }
            private set { _Id = value; }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Get or set a string array of attribute names that are allowed to have an empty value
        /// </summary>
        public virtual string[] AttributesAllowedEmpty
        {
            get { return _AttributesAllowedEmpty; }
            set { _AttributesAllowedEmpty = value; }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Gets the InnerHtml of this HtmlTag.
        /// Returns null if the tag is self closing.
        /// </summary>
        public virtual HtmlDom InnerHtml
        {
            get { return _InnerHtml; }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Returns true if the string output has
        /// been modified
        /// </summary>
        public virtual bool IsModifiedOutput
        {
            get { return _AltHtml != null; }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Returns the number of attributes in this tag.
        /// </summary>
        public virtual int Count
        {
            get { return _AttributeList.Count; }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// A list of the attributes in this AttributeList
        /// </summary>
        public virtual List<IHtmlAttribute> AttributeList
        {
            get
            {
                return _AttributeList;
            }
        }


        // ----------------------------------------------------------------------
        /// <summary>
        /// Get the individual attributes by index
        /// </summary>
        public virtual IHtmlAttribute this[int index]
        {
            get
            {
                if (index < _AttributeList.Count)
                    return _AttributeList[index] as HtmlAttribute;
                else
                    return null;
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Get the individual attributes by name.
        /// </summary>
        public virtual IHtmlAttribute this[string name]
        {
            get
            {
                int i = 0;
                while (this[i] != null)
                {
                    if (String.Compare(this[i].Name, name, true) == 0)
                        return this[i];
                    i++;
                }
                IHtmlAttribute newAtt = new HtmlAttribute(name, String.Empty);
                _AttributeList.Insert(0, newAtt);
                return newAtt;
            }
        }

        #endregion // Public Properties

        #region Public Methods

        // ----------------------------------------------------------------------
        /// <summary>
        /// Add the specified attribute to the list of attributes.
        /// </summary>
        /// <param name="a">An attribute to add to this
        /// AttributeList.</paramv
        public virtual void Add(IHtmlAttribute attribute)
        {
            _AttributeList.Add(attribute);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Add new or modify attribute to the list of attributes
        /// </summary>
        /// <param name="attributeName">The attributes name</param>
        /// <param name="attributeValue">The value of the attribute</param>
        public virtual void Add(string attributeName, string attributeValue)
        {
            foreach (HtmlAttribute att in _AttributeList)
            {
                if (String.Compare(att.Name, attributeName, true) == 0)
                {
                    att.Value = attributeValue;
                    return;
                }
            }

            HtmlAttribute attribute = new HtmlAttribute(attributeName, attributeValue);
            _AttributeList.Add(attribute);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Remove the specified attribute from the list of attributes.
        /// </summary>
        /// <param name="attribute">The attribute to remove.</param>
        public virtual void Remove(IHtmlAttribute attribute)
        {
            _AttributeList.Remove(attribute);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Remove the specified attribute from the list of attributes.
        /// </summary>
        /// <param name="attribute">The attribute to remove.</param>
        public virtual void Remove(string attribute)
        {
            //foreach (IHtmlAttribute attr in _AttributeList)
            for (int i = 0; i < _AttributeList.Count; i++)
            {
                if (String.Compare(_AttributeList[i].Name, attribute, true) == 0)
                {
                    _AttributeList.RemoveAt(i);
                    return;
                }
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Insert the specified attribute into the specified index location
        /// </summary>
        /// <param name="index">The index location to insert the attribute</param>
        /// <param name="attribute">The attribute to insert</param>
        public virtual void Insert(int index, IHtmlAttribute attribute)
        {
            _AttributeList.Insert(index, attribute);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Clears the list of attributes
        /// </summary>
        public virtual void Clear()
        {
            _AttributeList.Clear();
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Creates the html string equivalent of this Tag object
        /// </summary>
        ///<returns>Html string representing this tag object</returns>
        public virtual string ToHtml()
        {
            string tag = "<" + Name;
            foreach (IHtmlAttribute attribute in _AttributeList)
            {
                if (!String.IsNullOrEmpty(attribute.Value) || ArrayContainsString(AttributesAllowedEmpty, attribute.Name))
                {
                    tag += " " + attribute.ToHtmlReady();
                }
            }
            tag += ">";
            if (_InnerHtml != null)
            {
                foreach (IHtmlTag inner in _InnerHtml)
                {
                    tag += inner.ToString();
                }
            }

            if (!IsSelfClosingTag(this))
                tag += "</" + Name + ">";

            return tag;
        }

        public override string ToString()
        {
            return this.ToHtml();
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Changes the Html property output. 
        /// Does not affect any other values.
        /// </summary>
        /// <param name="html"></param>
        public virtual void SetHtml(string html)
        {
            _AltHtml = html;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Resets the Html property to output the html tag
        /// that this class instance represents.
        /// </summary>
        public virtual void ResetHtml()
        {
            _AltHtml = null;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Return the html of this tag, or alternative
        /// Html if it has been set using SetHtml()
        /// </summary>
        public virtual string Html
        {
            get
            {
                if (_AltHtml == null)
                {
                    string tag = "<" + Name;
                    foreach (IHtmlAttribute attribute in _AttributeList)
                    {
                        if (!String.IsNullOrEmpty(attribute.Value) || ArrayContainsString(AttributesAllowedEmpty, attribute.Name))
                        {
                            tag += " " + attribute.ToHtmlReady();
                        }
                    }
                    return tag + ">";
                }
                else
                    return _AltHtml;
            }
        }

        /// <summary>
        /// Determines if the given attribute name is contained
        /// in this HtmlTag
        /// </summary>
        /// <param name="attributeName">The attribute name to test for.</param>
        /// <returns>True if found, false if not</returns>
        public virtual bool ContainsAttribute(string attributeName)
        {
            for (int i = 0; i < _AttributeList.Count; i++)
            {
                if (String.Compare(_AttributeList[i].Name, attributeName, true) == 0)
                    return true;
            }
            return false;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Clone this object
        /// </summary>
        public object Clone()
        {
            HtmlTag tag = new HtmlTag();
            foreach (IHtmlAttribute att in _AttributeList)
            {
                tag.Add(att.ToHtmlAttribute());
            }
            tag.Name = this.Name;
            return tag;
        }

        #endregion // Public Methods

        #region Constructors

        // ----------------------------------------------------------------------
        /// <summary>
        /// HtmlTag Constructor
        /// </summary>
        public HtmlTag()
            : this(String.Empty, -1, null)
        { }

        // ----------------------------------------------------------------------
        /// <summary>
        /// HtmlTag Constructor
        /// </summary>
        public HtmlTag(string tagName, params IHtmlAttribute[] attributes)
            : this(tagName, -1, attributes)
        { }

        // ----------------------------------------------------------------------
        /// <summary>
        /// HtmlTag Constructor
        /// </summary>
        public HtmlTag(string tagName, int tagIndex, params IHtmlAttribute[] attributes)
        {
            Id = Guid.NewGuid();
            this._AttributeList = new List<IHtmlAttribute>();
            this.AttributesAllowedEmpty = new string[] { "/", "async" };
            this.Name = tagName;
            if (attributes != null)
            {
                _AttributeList.AddRange(attributes);
            }
            this.TagIndex = tagIndex;
            if (IsSelfClosingTag(this)) return; // return if self closing so that _InnerHtml will be null;
            _InnerHtml = new HtmlDom();
        }

        #endregion // Constructors
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// Represents a single attribute from an html tag
    /// </summary>
    public class HtmlAttribute : IHtmlAttribute, ICloneable
    {
        #region Fields

        protected string _Name, _Value;

        #endregion // Fields

        #region Public Properties

        // ----------------------------------------------------------------------
        /// <summary>
        /// The name of this attribute.
        /// </summary>
        public virtual string Name { get { return _Name; } set { _Name = value; } }

        // ----------------------------------------------------------------------
        /// <summary>
        /// The value of this attribute.
        /// </summary>
        public virtual string Value { get { return _Value; } set { _Value = value; } }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Gets the recommended quote character for the value
        /// </summary>
        public virtual char RecommendedQuotation
        {
            get
            {
                if (Value.Contains("\""))
                    return '\'';
                else
                    return '\"';
            }
        }

        #endregion // Public Properties

        #region Public Methods

        // ----------------------------------------------------------------------        
        /// <summary>
        /// Constructs a string that is ready to
        /// be placed into an html tag
        /// </summary>
        /// <returns>A string ready to be placed into an html tag</returns>
        public virtual string ToHtmlReady()
        {
            if (!String.IsNullOrEmpty(this.Value))
                return this.Name + '=' + this.RecommendedQuotation + this.Value + this.RecommendedQuotation;
            else
                return this.Name;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Converts this object into a cloned generic HtmlAttribute
        /// </summary>
        /// <returns>HtmlAttribute object</returns>
        public virtual HtmlAttribute ToHtmlAttribute()
        {
            return new HtmlAttribute(this.Name, this.Value);
        }
        
        // ----------------------------------------------------------------------
        /// <summary>
        /// Creates a duplicate attribute object
        /// </summary>
        public object Clone() { return new HtmlAttribute(this.Name, this.Value); }

        #endregion // Public Methods

        #region Constructors

        // ----------------------------------------------------------------------
        /// <summary>
        /// Construct a blank attribute.
        /// </summary>
        public HtmlAttribute() : this(String.Empty, String.Empty) { }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Construct a new html attribute object.
        /// </summary>
        /// <param name="propertyName">The name of this attribute.</param>
        /// <param name="value">The value of this attribute.</param>
        /// </param>
        public HtmlAttribute(string attributeName, string value)
        {
            this.Name = attributeName;
            this.Value = value;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Construct a new html attribute object from another.
        /// </summary>
        /// <param name="attribute">The html attribute to copy.</param>
        public HtmlAttribute(IHtmlAttribute attribute)
        {
            this.Name = attribute.Name;
            this.Value = attribute.Value;
        }

        #endregion // Constructors
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// Makes working with the style attribute easier
    /// </summary>
    public class HtmlStyleAttribute : IHtmlAttribute, IEnumerator, IEnumerable
    {
        protected List<CssStyleProperty> _Styles = new List<CssStyleProperty>();

        #region Public Properties

        // ----------------------------------------------------------------------
        /// <summary>
        /// Gets or sets the value of the specified style name
        /// </summary>
        /// <param name="styleName">The name of the style</param>
        /// <returns>The styles value</returns>
        public virtual string this[string styleName]
        {
            get
            {
                for (int i = 0; i < _Styles.Count; i++)
                {
                    if (String.Compare(_Styles[i].Name, styleName, true) == 0)
                        return _Styles[i].Value;
                }
                return String.Empty;
            }
            set
            {
                for (int i = 0; i < _Styles.Count; i++)
                {
                    if (String.Compare(_Styles[i].Name, styleName, true) == 0)
                    {
                        _Styles[i].Value = value;
                        return;
                    }
                }
                this.Add(styleName, value, true);
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Returns the name of this attribute
        /// </summary>
        public virtual string Name
        {
            get
            {
                return "style";
            }
            set { }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Returns a string value for the Style
        /// attribute
        /// </summary>
        public virtual string Value
        {
            get
            {
                string styles = String.Empty;
                foreach (CssStyleProperty style in _Styles)
                {
                    styles += style.ToString();
                }
                return styles;
            }
            set
            {
                _Styles.Clear();
                this.AddRange(value);
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Required Interface Implementation.
        /// Not Used
        /// </summary>
        public virtual char RecommendedQuotation
        {
            get { return '\''; }
        }

        #endregion // Public Properties

        #region Public Methods

        // ----------------------------------------------------------------------
        /// <summary>
        /// Adds a style to this style attribute. If the style name
        /// </summary>
        /// <param name="styleName">Style property name</param>
        /// <param name="styleValue">Style Value</param>
        /// <param name="force">true = replace existing value</param>
        public virtual void Add(string styleName, string styleValue, bool force)
        {
            this.Add(new CssStyleProperty(styleName, styleValue), force);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Adds a style to this style attribute.
        /// </summary>
        /// <param name="style">Style property</param>
        /// <param name="force">true = replace existing value</param>
        public virtual void Add(CssStyleProperty style, bool force)
        {
            for (int i = 0; i < _Styles.Count; i++)
            {
                if (String.Compare(_Styles[i].Name, style.Name, true) == 0)
                {
                    if (force)
                    {
                        _Styles.RemoveAt(i);
                        break;
                    }
                    return;
                }
            }
            _Styles.Add(style);
        }
                
        // ----------------------------------------------------------------------
        /// <summary>
        /// Adds a range of styles from a style attribute string
        /// to this style attribute. If the style name
        /// is already present, then the style is not added
        /// </summary>
        /// <param name="style">string style attribute value</param>
        public virtual void AddRange(string styles)
        {
            string[] values;
            string[] properties = styles.Split(';');
            for (int i = 0; i < properties.Length; i++)
            {
                values = properties[i].Split(':');
                if (values.Length == 2)
                    this.Add(values[0].Trim(), values[1].Trim(), false);
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Adds a range of styles from a style attribute string
        /// to this style attribute. If the style name
        /// is already present, then the style is not added
        /// </summary>
        /// <param name="style">string style attribute value</param>
        public virtual void AddRange(IEnumerable<CssStyleProperty> styles)
        {
            foreach (CssStyleProperty style in styles)
            {
                this.Add(style, false);
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Removes the specified style by Name
        /// </summary>
        /// <param name="stylename">The name of the style</param>
        /// <param name="ignoreCase">Chooses if the style name is case sensitive or not</param>
        public virtual void Remove(string styleName, bool ignoreCase)
        {
            for (int i = 0; i < _Styles.Count; i++)
            {
                if (String.Compare(_Styles[i].Name, styleName, ignoreCase) == 0)
                {
                    _Styles.RemoveAt(i);
                    return;
                }
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Removes the specified style by Name
        /// </summary>
        /// <param name="stylename">The name of the style</param>
        public virtual void Remove(string styleName)
        {
            this.Remove(styleName, true);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Removes the specified style by Name
        /// </summary>
        /// <param name="stylename">The name of the style</param>
        public virtual void Remove(CssStyleProperty style)
        {
            this.Remove(style.Name, true);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Places the styles in a formatted string that
        /// can be inserted into an Html tag
        /// </summary>
        public virtual string ToHtmlReady()
        {
            string styles = String.Empty;
            foreach (CssStyleProperty style in _Styles)
            {
                styles += style.ToString();
            }
            return "style=\"" + styles + "\"";
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Returns an HtmlAttribute object
        /// </summary>
        public virtual HtmlAttribute ToHtmlAttribute()
        {
            return new HtmlAttribute("style", this.ToHtmlReady());
        }

        #endregion // Public Methods

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="values">CssStyleProperty param</param>
        public HtmlStyleAttribute(params CssStyleProperty[] values)
        {
            if (values != null)
            {
                for (int i = 0; i < values.Length; i++)
                    _Styles.Add(values[i]);
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="values">IEnumerable CssStyleProperty array</param>
        public HtmlStyleAttribute(IEnumerable<CssStyleProperty> values)
        {
            foreach (CssStyleProperty style in values)
                this.Add(style, true);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="style">A string of Css style properties and values</param>
        public HtmlStyleAttribute(string styles)
        {
            string[] properties = styles.Split(';');
            string[] components;
            for (int i = 0; i < properties.Length; i++)
            {
                components = properties[i].Split(':');
                if (components.Length == 2)
                {
                    Add(components[0], components[1], true);
                }
            }
        }

        #endregion // Constructors

        #region IEnumerable, IEnumerator Interface Implementation

        // ----------------------------------------------------------------------
        // IEnumerator Stuff
        private int position = -1;

        // ----------------------------------------------------------------------
        public object Current
        {
            get
            {
                return _Styles[position];
            }
        }

        // ----------------------------------------------------------------------
        public bool MoveNext()
        {
            position++;
            return (position < _Styles.Count);
        }

        // ----------------------------------------------------------------------
        public void Reset()
        {
            position = -1;
        }

        // ----------------------------------------------------------------------
        //IEnumerable stuff
        public IEnumerator GetEnumerator()
        {
            return (IEnumerator)this;
        }

        #endregion // IEnumerable, IEnumerator Interface Implementation
        
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// A Css style attribute
    /// </summary>
    public class CssStyleProperty
    {
        #region Fields

        string _Name;
        string _Value;

        #endregion // Fields

        #region Public Properties

        // ----------------------------------------------------------------------
        /// <summary>
        /// Gets or sets the property name
        /// </summary>
        public virtual string Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Gets or sets the property value
        /// </summary>
        public virtual string Value
        {
            get { return _Value; }
            set { _Value = value; }
        }

        #endregion // Public Properties

        #region Public Methods

        // ----------------------------------------------------------------------
        public override string ToString()
        {
            return _Name + ':' + _Value + ';';
        }

        #endregion // Public Methods

        #region Constructors

        // ----------------------------------------------------------------------
        public CssStyleProperty() { }
        public CssStyleProperty(string name, string value)
        {
            _Name = name;
            _Value = value;
        }

        #endregion // Constructors
    }

    /// <summary>
    /// Html Attribute object whose value is delimeted
    /// </summary>
    public class HtmlDelimValueAttribute : IHtmlAttribute, ICloneable
    {
        #region Fields

        protected List<string> _DelimValues = new List<string>();
        protected string _Name;
        protected char _ValueDelimeter;

        #endregion // Fields

        #region Public Properties

        // ----------------------------------------------------------------------
        /// <summary>
        /// A list of the value collection
        /// </summary>
        public virtual List<string> DelimValues
        {
            get { return _DelimValues; }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// The name of this attribute.
        /// </summary>
        public virtual string Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// The value of this attribute.
        /// </summary>
        public virtual string Value
        {
            get
            {
                string returnValue = String.Empty;
                for (int i = 0; i < _DelimValues.Count; i++)
                {
                    returnValue += _DelimValues[i];
                    if (i < _DelimValues.Count - 1)
                        returnValue += ValueDelimeter;
                }
                return returnValue;
            }
            set
            {
                string[] split = value.Split(ValueDelimeter);
                foreach (string s in split)
                    _DelimValues.Add(s.Trim());
            }
        }

        /// <summary>
        /// Gets the number of values
        /// </summary>
        public virtual int Count
        {
            get { return _DelimValues.Count; }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Gets the recommended quote character for the value
        /// </summary>
        public virtual char RecommendedQuotation
        {
            get
            {
                char r = '\"';
                string rs = r.ToString();
                foreach (string s in _DelimValues)
                    if (s.Contains(rs)) return '\'';
                return r;
            }
        }
        
        // ----------------------------------------------------------------------
        /// <summary>
        /// The delimeter of the collection value.
        /// (ie comma, as in name="value1,value2,value3" )
        /// </summary>
        public virtual char ValueDelimeter
        {
            get { return _ValueDelimeter; }
            set { _ValueDelimeter = value; }
        }

        #endregion // Public Properties

        #region Public Methods

        /// <summary>
        /// Adds a value to this attribute
        /// </summary>
        /// <param name="value">The value to add</param>
        public virtual void Add(string value)
        {
            if (!String.IsNullOrEmpty(value))
            {
                value = value.Replace(ValueDelimeter.ToString(), String.Empty);
                _DelimValues.Add(value);
            }
        }

        /// <summary>
        /// Removes the specified value
        /// </summary>
        /// <param name="value">The string value to remove</param>
        /// <param name="ignoreCase">Chooses if the value is case senstive or not</param>
        public virtual void Remove(string value, bool ignoreCase)
        {
            if (!String.IsNullOrEmpty(value))
            {
                for (int i = 0; i < _DelimValues.Count; i++)
                    if (String.Compare(_DelimValues[i], value, ignoreCase) == 0)
                    {
                        _DelimValues.RemoveAt(i);
                        return;
                    }
            }
        }

        // ----------------------------------------------------------------------        
        /// <summary>
        /// Constructs a string that is ready to
        /// be placed into an html tag
        /// </summary>
        /// <returns>A string ready to be placed into an html tag</returns>
        public virtual string ToHtmlReady()
        {
            if (!String.IsNullOrEmpty(this.Value))
                return this.Name + '=' + this.RecommendedQuotation + this.Value + this.RecommendedQuotation;
            else
                return this.Name;

        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Converts this object into a generic HtmlAttribute
        /// </summary>
        /// <returns>HtmlAttribute object</returns>
        public virtual HtmlAttribute ToHtmlAttribute()
        {
            return new HtmlAttribute(this.Name, this.Value);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Clone this instance
        /// </summary>
        public virtual object Clone()
        {
            HtmlDelimValueAttribute delim = new HtmlDelimValueAttribute(this.Name, this.ValueDelimeter);
            delim.Value = this.Value;
            return delim;
        }

        #endregion // Public Methods

        #region Constructors

        // ----------------------------------------------------------------------
        /// <summary>
        /// Construct a blank attribute.
        /// </summary>
        public HtmlDelimValueAttribute() : this(String.Empty, (char)0) { }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Construct an attribute with a default delimiter.
        /// </summary>
        /// <param name="propertyName">The name of this attribute.</param>
        /// <param name="value">The value of this attribute.</param>
        public HtmlDelimValueAttribute(String attributeName, String value) : this(attributeName, ',', value) { }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Construct a new html attribute object with a delimeted value.
        /// </summary>
        /// <param name="attributeName">The name of the attribute</param>
        /// <param name="values">The attribute value</param>
        /// <param name="valuedelimeter">The values delimeter character</param>
        public HtmlDelimValueAttribute(string attributeName, char valuedelimeter, params string[] values)
        {
            Name = attributeName;
            if (values != null)
                _DelimValues.AddRange(values);
            ValueDelimeter = valuedelimeter;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Construct a new html attribute object with a delimeted value.
        /// </summary>
        /// <param name="attributeName">The name of the attribute</param>
        /// <param name="values">A collection of values</param>
        /// <param name="valuedelimeter">The values delimeter character</param>
        public HtmlDelimValueAttribute(string attributeName, IEnumerable<string> values, char valuedelimeter)
        {
            Name = attributeName;
            _DelimValues.AddRange(values);
            ValueDelimeter = valuedelimeter;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Construct a new delimeted value Html object
        /// from another html attribute.
        /// </summary>
        /// <param name="attribute">The html attribute to copy</param>
        /// <param name="valueDelimeter">The value delimeter to use</param>
        public HtmlDelimValueAttribute(IHtmlAttribute attribute, char valueDelimeter)
        {
            this.Name = attribute.Name;
            this.Value = attribute.Value;
            this.ValueDelimeter = valueDelimeter;
        }

        #endregion // Constructors

    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// Represents plain text in an Html Document
    /// </summary>
    public class HtmlText : IHtmlText
    {
        #region Fields

        protected string _Text = String.Empty;
        protected Guid _Id;

        #endregion // Fields

        #region Public Properties

        // ----------------------------------------------------------------------
        /// <summary>
        /// Html string text
        /// </summary>
        public virtual string Text
        {
            get { return _Text; }
            set
            {
                if (value == null)
                    _Text = String.Empty;
                else
                    _Text = value;
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Unique identifier for this object
        /// </summary>
        public virtual Guid Id
        {
            get { return _Id; }
            private set { _Id = value; }
        }

        #endregion // Public Properties

        #region Public Methods

        // ----------------------------------------------------------------------
        public override string ToString()
        {
            return this.Text;
        }

        #endregion // Public Methods

        #region Constructors

        // ----------------------------------------------------------------------
        public HtmlText() : this(String.Empty) { }

        // ----------------------------------------------------------------------
        public HtmlText(string text)
        {
            this.Text = text;
            this.Id = Guid.NewGuid();
        }

        #endregion // Contructors
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// Represents an Extension that is subscribed to
    /// BlogParser Events
    /// </summary>
    public class Subscriber
    {
        #region Fields

        List<SubscriberSetting> _Settings = new List<SubscriberSetting>();
        string _Name; ISubscriber _Instance;
        ManagedExtension _Extension;

        #endregion // Fields

        #region Public Properties

        // ----------------------------------------------------------------------
        /// <summary>
        /// Gets the subscriber setting at the
        /// specified index.
        /// </summary>
        /// <param name="index">The index of the subscriber setting.</param>
        /// <returns>Subscriber setting</returns>
        public SubscriberSetting this[int index]
        {
            get { return _Settings[index]; }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Gets the subscriber setting by name.
        /// </summary>
        /// <param name="settingName">The name of the subscriber setting</param>
        /// <returns>Subscriber setting</returns>
        public SubscriberSetting this[string settingName]
        {
            get
            {
                for (int i = 0; i < _Settings.Count; i++)
                {
                    if (String.Compare(_Settings[i].Name, settingName, true) == 0)
                    {
                        return _Settings[i];
                    }
                }
                return null;
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Gets the Name of the Extension
        /// </summary>
        public string Name
        {
            get { return _Name; }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Gets the Priority of the Extension
        /// </summary>
        public int Priority
        {
            get
            {
                if (_Extension == null)
                {
                    _Extension = ExtensionManager.GetExtension(_Name);
                }
                if (_Extension != null)
                {
                    return _Extension.Priority;
                }
                return 0;
            }

        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Gets the object instance of the Extension
        /// </summary>
        public ISubscriber Instance
        {
            get { return _Instance; }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Gets the number of events the Extension
        /// is subscribed to.
        /// </summary>
        public int Count
        {
            get { return _Settings.Count; }
        }

        #endregion // Public Properties

        #region Public Methods

        // ----------------------------------------------------------------------
        /// <summary>
        /// Adds a subscriber setting for the extension
        /// this object represents.
        /// </summary>
        /// <param name="setting">SubscriberSetting object</param>
        public void Add(SubscriberSetting setting)
        {
            for (int i = 0; i < _Settings.Count; i++)
            {
                if (String.Compare(_Settings[i].Name, setting.Name, true) == 0)
                {
                    _Settings.RemoveAt(i);
                    break;
                }
            }
            _Settings.Add(setting);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Removes a subscriber settings for the extension
        /// this object represents.
        /// </summary>
        /// <param name="settingName">The name of the setting</param>
        public void Remove(string settingName)
        {
            for (int i = 0; i < _Settings.Count; i++)
            {
                if (String.Compare(_Settings[i].Name, settingName, true) == 0)
                {
                    _Settings.RemoveAt(i);
                    return;
                }
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Removes a subscriber settings at the 
        /// specified index.
        /// </summary>
        /// <param name="index">The index of the setting</param>
        public void RemoveAt(int index)
        {
            _Settings.RemoveAt(index);
        }

        #endregion // Public Methods

        #region Constructor

        // ----------------------------------------------------------------------
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">The name of the extension</param>
        /// <param name="instance">The instance of the extension</param>
        public Subscriber(string name, ISubscriber instance)
        {
            _Name = name;
            _Instance = instance;
        }

        #endregion // Constructor
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// Holds a setting for a subscribing extension
    /// </summary>
    public class SubscriberSetting
    {
        #region Fields

        string _Name;
        string[] _Values;

        #endregion // Fields

        #region Public Properties

        // ----------------------------------------------------------------------
        /// <summary>
        /// Gets the name of the setting.
        /// </summary>
        public string Name
        {
            get { return _Name; }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Gets the values assigned to this setting.
        /// </summary>
        public string[] Values
        {
            get { return _Values; }
        }

        #endregion // Public Properties

        #region Constructor

        // ----------------------------------------------------------------------
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="settingName">The name of the setting</param>
        /// <param name="values">The string values assigned to this setting</param>
        public SubscriberSetting(string settingName, params string[] values)
        {
            _Name = settingName;
            _Values = values.Clone() as string[];
        }

        #endregion // Constructor
    }


    #endregion // Classes

    #region Interfaces

    /// <summary>
    /// Interface for HtmlAttribute
    /// </summary>
    public interface IHtmlAttribute
    {
        /// <summary>
        /// The name of the attribute
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// The value of the attribute
        /// </summary>
        string Value { get; set; }

        /// <summary>
        /// Gets the recommended quote character for the value
        /// </summary>
        char RecommendedQuotation { get; }

        /// <summary>
        /// Constructs a string that is ready to
        /// be placed into an html tag
        /// </summary>
        /// <returns>A string ready to be placed into an html tag</returns>
        string ToHtmlReady();

        /// <summary>
        /// Converts this object into a generic HtmlAttribute
        /// </summary>
        /// <returns>HtmlAttribute object</returns>
        HtmlAttribute ToHtmlAttribute();

    }

    /// <summary>
    /// Interface for HtmlText
    /// </summary>
    public interface IHtmlText
    {
        /// <summary>
        /// Gets or sets the string text
        /// </summary>
        string Text { get; set; }

        /// <summary>
        /// Gets the Guid id for this object
        /// </summary>
        Guid Id { get; }
    }

    /// <summary>
    /// Interface for HtmlTag
    /// </summary>
    public interface IHtmlTag
    {
        /// <summary>
        /// The name of this tag
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// The index order of the tag within a document
        /// </summary>
        int TagIndex { get; }

        /// <summary>
        /// A unique Id assigned to the tag
        /// when it is created.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Returns the number of attributes in this tag.
        /// </summary>
        int Count { get; }


        /// <summary>
        /// A list of the attributes in this AttributeList
        /// </summary>
        List<IHtmlAttribute> AttributeList { get; }

        /// <summary>
        /// The inner Html objects of this tag.
        /// </summary>
        HtmlDom InnerHtml { get; }

        /// <summary>
        /// Get the individual attributes by index
        /// </summary>
        IHtmlAttribute this[int index] { get; }

        /// <summary>
        /// Get the individual attributes by name.
        /// </summary>
        IHtmlAttribute this[string name] { get; }

        /// <summary>
        /// Add the specified attribute to the list of attributes.
        /// </summary>
        /// <param name="a">An attribute to add to this
        /// AttributeList.</param>
        void Add(IHtmlAttribute attribute);

        /// <summary>
        /// Remove the specified attribute from the list of attributes.
        /// </summary>
        /// <param name="attribute">The attribute to remove.</param>
        void Remove(IHtmlAttribute attribute);

        /// <summary>
        /// Insert the specified attribute into the specified index location
        /// </summary>
        /// <param name="index">The index location to insert the attribute</param>
        /// <param name="attribute">The attribute to insert</param>
        void Insert(int index, IHtmlAttribute attribute);
        
        /// <summary>
        /// Clears the list of attributes
        /// </summary>
        void Clear();
    }

    

    /// <summary>
    /// Interface for BlogParser Subscribers
    /// </summary>
    public interface ISubscriber
    {
        /// <summary>
        /// Fired when a tag is found in content to be displayed
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="e"></param>
        void TagFound(HtmlTagArgs e);
        /// <summary>
        /// Fired when a HtmlDOM object is ready from served content
        /// </summary>
        /// <param name="dom"></param>
        /// <param name="e"></param>
        void DomReady(HtmlDomArgs e);

    }

    #endregion // Interfaces

}
