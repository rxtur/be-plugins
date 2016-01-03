using BlogEngine.Core;
using BlogEngine.Core.Web.Controls;
using BlogEngine.Core.Web.HttpHandlers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

/// <summary>
/// Simple Download Counter
/// </summary>
[Extension("Simple Download Counter", "2.9.0.1", "<a href=\"http://www.nyveldt.com/blog/\">Al Nyveldt</a>")]
public class SimpleDownloadCounter
{
  private static string dataSource = System.Web.HttpContext.Current.Server.MapPath(Blog.CurrentInstance.StorageLocation) + "downloadcounts.xml";

  public SimpleDownloadCounter()
  {
    Post.Serving += new EventHandler<ServingEventArgs>(Post_Serving);
    Page.Serving += new EventHandler<ServingEventArgs>(Page_Serving);
    FileHandler.Served += new EventHandler<EventArgs>(FileHandler_Served);
  }

  /// <summary>
  /// Handles the Post.Serving event to display file counts when logged in.
  /// </summary>
  private void Post_Serving(object sender, ServingEventArgs e)
  {
    if (System.Threading.Thread.CurrentPrincipal.Identity.IsAuthenticated)
    {
      e.Body = UpdateDisplay(e.Body);
    }
  }

  /// <summary>
  /// Handles the PageServing event to display file counts when logged in.
  /// </summary>
  private void Page_Serving(object sender, ServingEventArgs e)
  {
    if (System.Threading.Thread.CurrentPrincipal.Identity.IsAuthenticated)
    {
      e.Body = UpdateDisplay(e.Body);
    }
  }

  /// <summary>
  /// Handles FileHandler_Served event to count files downloaded
  /// </summary>
  /// <param name="sender"></param>
  /// <param name="e"></param>
  private void FileHandler_Served(object sender, EventArgs e)
  {
    List<DownloadedFile> files = new List<DownloadedFile>();
    string newFile = (string)sender;
    bool fileFound = false;

    // Read it in
    if (File.Exists(dataSource))
    {
      XmlDocument doc = new XmlDocument();
      doc.Load(dataSource);

      foreach (XmlNode node in doc.SelectNodes("files/file"))
      {
        int count = int.Parse(node.Attributes["count"].InnerText);
        string name = node.InnerText;

        DownloadedFile temp = new DownloadedFile(name, count);

        if (temp.Name == newFile)
        {
          // Increment the total and don't add filename to list
          temp.Count++;
          fileFound = true;
        }

        files.Add(temp);
      }
    }
    
    // Add new file in if not in file
    // Also covers case where file does not exist.
    if (!fileFound)
    {
      DownloadedFile temp = new DownloadedFile(newFile, 1);
      files.Add(temp);
    }

    // Write it out
    using (XmlTextWriter writer = new XmlTextWriter(dataSource, System.Text.Encoding.UTF8))
    {
      writer.Formatting = Formatting.Indented;
      writer.Indentation = 4;
      writer.WriteStartDocument(true);
      writer.WriteStartElement("files");

      foreach (DownloadedFile file in files)
      {
        writer.WriteStartElement("file");
        writer.WriteAttributeString("count", file.Count.ToString());
        writer.WriteValue(file.Name);
        writer.WriteEndElement();
      }

      writer.WriteEndElement();
    }
  }

  /// <summary>
  /// Convert links to include download counts
  /// </summary>
  /// <param name="body">Original Post/Page body</param>
  /// <returns>Updated Post/Page body</returns>
  private string UpdateDisplay(string body)
  {
    if (body.Contains("file.axd?file="))
    {
      int pos = body.IndexOf("file.axd?file=");
      while (pos > 0)
      {
        pos = pos + 14;
        string filename = body.Substring(pos, body.IndexOf("\"", pos) - pos);
        int count = GetFileCount(filename);
        int linkTextEnds = body.IndexOf("</a>", pos);
        body = body.Insert(linkTextEnds, " [Downloads: " + count.ToString() + "]");
        pos = body.IndexOf("file.axd?file=", pos);
      }
    }
    if (body.Contains(".axdx"))
    {
        body = body.Replace("%2f", "/");
        int pos = body.IndexOf("/FILES/");
        while (pos > 0)
        {
            pos = pos + 6;
            string filename = body.Substring(pos, body.IndexOf(".axdx", pos) - pos);
            int count = GetFileCount(filename);
            int linkTextEnds = body.IndexOf("</a>", pos);
            body = body.Insert(linkTextEnds, " [Downloads: " + count.ToString() + "]");
            pos = body.IndexOf("/FILES/", pos);
        }
    }
    return body;
  }

  /// <summary>
  /// Retrieve download count from storage
  /// </summary>
  /// <param name="fileName">name of file to look up</param>
  /// <returns></returns>
  private int GetFileCount(string fileName)
  {
    int fileCount = 0;

    if (File.Exists(dataSource))
    {
      XmlDocument doc = new XmlDocument();
      doc.Load(dataSource);

      foreach (XmlNode node in doc.SelectNodes("files/file"))
      {
        int count = int.Parse(node.Attributes["count"].InnerText);

        string name = RemoveEncoding(node.InnerText);
        string file = RemoveEncoding(fileName);

        if (name == file)
        {
          fileCount = count;
          break;
        }
      }
    }

    return fileCount;
  }

  private string RemoveEncoding(string text)
  {
      var str = text.Replace("%2f", "/");
      str = str.Replace("+", " ");

      if (str.StartsWith("/"))
          str = str.Substring(1);

      return str.ToUpper();
  }

  /// <summary>
  /// Simple class to hold Download File info
  /// </summary>
  private class DownloadedFile
  {
    public DownloadedFile(string name, int count)
    {
      _name = name;
      _count = count;
    }

    private string _name;

    public string Name
    {
      get { return _name; }
      set { _name = value; }
    }

    private int _count;

    public int Count
    {
      get { return _count; }
      set { _count = value; }
    }
  }
}
