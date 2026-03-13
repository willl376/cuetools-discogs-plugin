using CUETools.CDImage;
using HelperFunctionsLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;

using System.Runtime.InteropServices;
using System.Windows.Forms;


namespace MetadataPlugIn
{
    [Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890"),
    ClassInterface(ClassInterfaceType.None),
    ComSourceInterfaces(typeof(IMetadataRetriever)),
    ]
    public class MetadataRetriever : IMetadataRetriever
    {
        private const string UserToken = " nSvkFflhzxjDzYEfclpSpslSSPcaUJhDxQsAfSXG";
        private const string UserAgent = "CUEToolsDiscogsPlugin/1.0";

        public bool GetCDInformation(CCDMetadata data,
            bool cdinfo, bool cover, bool lyrics)
        {
            if (!cdinfo && !cover)
                return false;

            try
            {
                string search = data.AlbumArtist + " " + data.AlbumTitle;
                search = search.Trim();

                if (string.IsNullOrEmpty(search))
                {
                    search = ShowInputDialog(
                        "Enter album name to search on Discogs:");
                    if (string.IsNullOrEmpty(search))
                        return false;
                }

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var wc = new WebClient();
                wc.Headers.Add("User-Agent", UserAgent);
                wc.Headers.Add("Authorization",
                    "Discogs token=" + UserToken);

                string url = "https://api.discogs.com/database/search?q="
                    + Uri.EscapeDataString(search)
                    + "&type=release&per_page=10";

                string json = wc.DownloadString(url);
                var obj = JObject.Parse(json);
                var results = (JArray)obj["results"];

                if (results == null || results.Count == 0)
                {
                    MessageBox.Show("No results found on Discogs.",
                        "Discogs Plugin");
                    return false;
                }

                var choices = new List<string>();
                foreach (var r in results)
                    choices.Add(r["title"]?.ToString() ?? "Unknown");

                int idx = ShowSelectionDialog(choices);
                if (idx < 0) return false;

                var selected = results[idx];

                if (cdinfo)
                {
                    string fullTitle = selected["title"]?.ToString() ?? "";
                    string[] parts = fullTitle.Split(
                        new string[] { " - " },
                        StringSplitOptions.None);
                    data.AlbumArtist = parts.Length > 1 ? parts[0] : "";
                    data.AlbumTitle = parts.Length > 1 ? parts[1] : fullTitle;

                    string yearStr = selected["year"]?.ToString() ?? "";
                    int year;
                    data.Year = int.TryParse(yearStr, out year) ? year : -1;
                    data.FirstTrackNumber = 1;
                    data.TotalNumberOfCDs = 1;
                    data.CDNumber = 1;

                    string extra = "";
                    var labels = selected["label"] as JArray;
                    if (labels != null && labels.Count > 0)
                        extra += "Release label: " + labels[0] + "\r\n";
                    string catno = selected["catno"]?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(catno))
                        extra += "Release catalog#: " + catno + "\r\n";
                    string country = selected["country"]?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(country))
                        extra += "Release country: " + country + "\r\n";
                    data.ExtendedDiscInformation = extra;

                    string resourceUrl =
                        selected["resource_url"]?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(resourceUrl))
                    {
                        string releaseJson = wc.DownloadString(resourceUrl);
                        var release = JObject.Parse(releaseJson);
                        var tracklist = (JArray)release["tracklist"];

                        for (int i = 0; i < data.NumberOfTracks; i++)
                        {
                            if (tracklist != null && i < tracklist.Count)
                            {
                                data.SetTrackTitle(i,
                                    tracklist[i]["title"]?.ToString() ?? "");
                                data.SetTrackArtist(i, data.AlbumArtist);
                            }
                            else
                            {
                                data.SetTrackTitle(i, "");
                                data.SetTrackArtist(i, data.AlbumArtist);
                            }
                            data.SetExtendedTrackInformation(i, "");
                            data.SetTrackComposer(i, "");
                        }
                    }
                }

                if (cover)
                {
                    string coverUrl =
                        selected["cover_image"]?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(coverUrl))
                    {
                        try
                        {
                            data.CoverImage = wc.DownloadData(coverUrl);
                            data.CoverImageURL = coverUrl;
                        }
                        catch { }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Discogs error: " + ex.Message,
                    "Discogs Plugin");
                return false;
            }
        }

        private string ShowInputDialog(string prompt)
        {
            Form form = new Form();
            form.Text = "Discogs Search";
            form.Width = 400;
            form.Height = 150;
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;

            Label lbl = new Label();
            lbl.Text = prompt;
            lbl.Left = 10; lbl.Top = 10;
            lbl.Width = 360;

            TextBox tb = new TextBox();
            tb.Left = 10; tb.Top = 35;
            tb.Width = 360;

            Button btn = new Button();
            btn.Text = "Search";
            btn.Left = 150; btn.Top = 65;
            btn.Click += (s, e) =>
                form.DialogResult = DialogResult.OK;

            form.Controls.Add(lbl);
            form.Controls.Add(tb);
            form.Controls.Add(btn);

            if (form.ShowDialog() == DialogResult.OK)
                return tb.Text;
            return null;
        }

        private int ShowSelectionDialog(List<string> choices)
        {
            Form form = new Form();
            form.Text = "Select Discogs Release";
            form.Width = 500;
            form.Height = 350;
            form.StartPosition = FormStartPosition.CenterScreen;

            ListBox lb = new ListBox();
            lb.Dock = DockStyle.Fill;
            foreach (var c in choices) lb.Items.Add(c);
            if (lb.Items.Count > 0) lb.SelectedIndex = 0;

            Button btn = new Button();
            btn.Text = "Select This Release";
            btn.Dock = DockStyle.Bottom;
            btn.Click += (s, e) =>
                form.DialogResult = DialogResult.OK;

            form.Controls.Add(lb);
            form.Controls.Add(btn);

            if (form.ShowDialog() == DialogResult.OK
                && lb.SelectedIndex >= 0)
                return lb.SelectedIndex;
            return -1;
        }

        public string GetPluginGuid()
        {
            return ((GuidAttribute)Attribute.GetCustomAttribute(
                GetType(), typeof(GuidAttribute))).Value;
        }

        public Array GetPluginLogo() { return null; }

        public string GetPluginName()
        {
            return "Discogs Metadata Plugin V1.0";
        }

        public void ShowOptions()
        {
            MessageBox.Show(
                "Discogs Metadata Plugin V1.0\n\n" +
                "Fetches metadata from Discogs.com",
                "Discogs Plugin");
        }

        public bool SubmitCDInformation(IMetadataLookup data)
        {
            throw new NotSupportedException();
        }

        public bool SupportsCoverRetrieval() { return true; }
        public bool SupportsLyricsRetrieval() { return false; }
        public bool SupportsMetadataRetrieval() { return true; }
        public bool SupportsMetadataSubmission() { return false; }
        public int GetMP3MusicType(int freedbtype) { return -1; }
    }
}