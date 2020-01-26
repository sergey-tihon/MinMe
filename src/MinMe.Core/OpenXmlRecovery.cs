using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace MinMe.Core
{
    public static class OpenXmlRecovery
    {
        // http://ericwhite.com/blog/handling-invalid-hyperlinks-openxmlpackageexception-in-the-open-xml-sdk/
        public static void FixInvalidUri(Stream fs)
        {
            var uriPlaceholder = "https://invalid.uri.com";

            XNamespace relNs = "http://schemas.openxmlformats.org/package/2006/relationships";
            using var za = new ZipArchive(fs, ZipArchiveMode.Update, leaveOpen: true);
            foreach (var entry in za.Entries.ToList())
            {
                if (!entry.Name.EndsWith(".rels"))
                    continue;
                var replaceEntry = false;
                XDocument entryXDoc = null;
                using (var entryStream = entry.Open())
                {
                    try
                    {
                        entryXDoc = XDocument.Load(entryStream);
                        if (entryXDoc.Root?.Name.Namespace == relNs)
                        {
                            var urisToCheck = entryXDoc
                                .Descendants(relNs + "Relationship")
                                .Where(r => r.Attribute("TargetMode")?.Value == "External");
                            foreach (var rel in urisToCheck)
                            {
                                if (rel.Attribute("Target") is {} attr)
                                {
                                    try
                                    {
                                        var _ = new Uri(attr.Value);
                                    }
                                    catch (UriFormatException)
                                    {
                                        attr.Value = uriPlaceholder;
                                        replaceEntry = true;
                                    }
                                }
                            }
                        }
                    }
                    catch (XmlException)
                    {
                        continue;
                    }
                }
                if (replaceEntry)
                {
                    var fullName = entry.FullName;
                    entry.Delete();
                    var newEntry = za.CreateEntry(fullName);
                    using var writer = new StreamWriter(newEntry.Open());
                    using var xmlWriter = XmlWriter.Create(writer);
                    entryXDoc.WriteTo(xmlWriter);
                }
            }
        }
    }
}