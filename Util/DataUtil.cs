using ESRI.ArcGIS.esriSystem;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using SpatialConnect.Entity;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace gbc.Util
{
    public sealed class DataUtil
    {
        public string Endpoint { get; set; }

        public delegate void NewMessageHandler(string msg);

        public DataUtil()
        {

        }

        #region "PropertySet"
        /// <summary>
        /// Populates and returns a PropertySet
        /// </summary>
        /// <param name="config">The Application Configuration</param>
        /// <returns></returns>
        public IPropertySet GetPropertySet(Config config)
        {
            IPropertySet _propertySet = new PropertySet();

            _propertySet.SetProperty("USER", config.arcgis_username);
            _propertySet.SetProperty("PASSWORD", config.arcgis_password);
            _propertySet.SetProperty("SERVER", config.arcgis_server);
            _propertySet.SetProperty("DATABASE", config.arcgis_database);
            _propertySet.SetProperty("INSTANCE", config.arcgis_instance);
            _propertySet.SetProperty("VERSION", config.arcgis_version);
            _propertySet.SetProperty("KEYWORD", config.arcgis_keyword);

            return _propertySet;
        }
        #endregion

 
        /// <summary>
        /// Converts an object into an XElement
        /// </summary>
        /// <param name="data">object data (the object to be converted)</param>
        /// <param name="xname">string xname (XName for root element of the object</param>
        /// <returns>Returns an XElement representation of your supplied object</returns>
        public static XElement ObjToElement(object data, string xname)
        {
            XElement x = new XElement(xname);
            Type t = data.GetType();
            PropertyInfo[] props = t.GetProperties();
            foreach (PropertyInfo prop in props)
            {
                object val = prop.GetValue(data, null);
                x.Add(new XElement(prop.Name.ToLower(), val.ToString()));
            }
            return x;
        }

        /// <summary>
        /// Filters out duplicate entries with matching MD5 ids
        /// </summary>
        /// <param name="doc">XDocument doc</param>
        /// <returns>XDocument doc</returns>
        public static XDocument RemoveDuplicatesByMD5(XDocument doc)
        {
            XElement records = new XElement("records");

            var recs = doc.Root.Element("records").Elements("record");
            foreach (XElement r in recs)
            {
                //  search the new 'records' collection for the currect record by id
                var s = from p in records.Descendants("record")
                        where p.Attribute("id").Value == r.Attribute("id").Value
                        select p;
                if (s.Count() < 1)
                {
                    records.Add(r);
                }
            }
            doc.Root.Element("records").Remove();
            doc.Root.Add(records);

            return doc;
        }

        /// <summary>
        /// Returns true if the object passed has a null value
        /// for any of its properties
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Boolean HasEmptyProperties(object data)
        {
            Type t = data.GetType();
            PropertyInfo[] props = t.GetProperties();
            foreach (PropertyInfo p in props)
            {
                string pName = p.Name;
                object pValue = p.GetValue(data, null);
                if (pValue == null)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Creates a new XDocument with the supplied root
        /// </summary>
        /// <param name="root">string root</param>
        /// <returns></returns>
        public static XDocument CreateXDoc(string root)
        {
            XDocument doc = new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                    new XElement(root)
            );
            return doc;
        }

        /// <summary>
        /// Creates a blank Update document
        /// </summary>
        /// <returns></returns>
        public static XDocument CreateBlankUpdate()
        {
            XDocument output = new XDocument(
                            new XDeclaration("1.0", "utf-8", "true"),
                            new XElement("update",
                            new XElement("sde"),
                            new XElement("records")
                    ));
            return output;
        }

        /// <summary>
        /// Cleans up an input by removing newlines and carriage returns
        /// </summary>
        /// <param name="input">string input</param>
        /// <returns>string input (cleaned of newline)</returns>
        public static string RemoveNewline(string input)
        {
            input = input.Replace("&#xA", "");
            input = input.Replace("\r\n", "");
            input = input.Replace("\n", "");
            input = input.Trim();
            return input;
        }

        /// <summary>
        /// Cleans up an input by removing newlines and carriage returns and subsituting an empty
        /// character.
        /// </summary>
        /// <param name="input">string input</param>
        /// <returns>string input (cleaned of newline)</returns>
        public static string RemoveNewline2(string input)
        {
            input = Regex.Replace(input, "s/\u0009/g", "");
            input = Regex.Replace(input, "s/\u000D/g", "");
            input = Regex.Replace(input, "s/\u000A/g", "");

            input = input.Trim();

            return input;
        }

        /// <summary>
        /// Formats an input kml coordinate linestring for use with GeoBoards
        /// </summary>
        /// <param name="input">string input</param>
        /// <returns>string input (coordinate string in proper geoboards format [X,Y,Z])</returns>
        public static string FormatCoordinatePairs(string input)
        {
            string pattern = @"\s*,\s*";
            string replacement = @",";
            Regex regexp = new Regex(pattern);
            input = regexp.Replace(input, replacement);
            return input;
        }

        /// <summary>
        /// Compares an input XElements dateTime element value to the current
        /// DateTime value
        /// </summary>
        /// <param name="elementToSearch"></param>
        /// <param name="record"></param>
        /// <returns></returns>
        public static Boolean IsOldData(string elementToSearch, XElement record)
        {
            var matchedElement = (from p in record.Descendants()
                                  where p.Name.LocalName.ToLower() == elementToSearch.ToLower()
                                  select p).FirstOrDefault();
            if (matchedElement != null)
            {
                long n, t;
                //var now = Int64.TryParse(GetComparableDateTime(), out n) == true ? n : 0;
                //var then = Int64.TryParse(matchedElement.Value, out t) == true ? t : 1;
                var now = Int64.TryParse(GetComparableDateTime(), out n);
                var then = Int64.TryParse(matchedElement.Value, out t);

                //	if this element had a validday to parse
                if (then)
                {
                    if (n > t)
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns a numeric comparable representation of the current DateTime
        /// </summary>
        /// <returns></returns>
        public static string GetComparableDateTime()
        {
            var dtPattern = @"yyyyMMdd";
            var dt = DateTime.UtcNow.ToLocalTime();
            var dtString = dt.ToString(dtPattern);
            return dtString;
        }

        public static string GetWebEOCDateTime()
        {
            var dtPattern = @"yyyy-MM-ddThh:mm:ss";
            var dt = DateTime.UtcNow.ToLocalTime();
            var dtString = dt.ToString(dtPattern);
            return dtString;
        }

        /// <summary>
        /// Modifies an input coordinate which lacks a decimal point
        /// </summary>
        /// <param name="input"></param>
        /// <param name="type"></param>
        public static string ModifyCoordinate(string input, string type)
        {
            try
            {
                string output = String.Empty;

                switch (type)
                {
                    case "latitude":
                    case "secondarylatitude":
                        {
                            output = input.Substring(0, 2) + "." + input.Substring(2, input.Length - 2);
                            break;
                        }
                    case "longitude":
                    case "secondarylongitude":
                        {
                            output = input.Substring(0, 3) + "." + input.Substring(3, input.Length - 3);
                            break;
                        }
                }
                return output;
            }
            catch
            {
                return "0";
            }
        }

        /// <summary>
        /// GZIP utility methods.
        /// </summary>
        public static class GZipTool
        {
            /// <summary>
            /// Checks the first two bytes in a GZIP file, which must be 31 and 139.
            /// </summary>
            public static bool IsGZipHeader(string filePath)
            {
                byte[] gzip = File.ReadAllBytes(filePath);

                return gzip.Length >= 2 &&
                    gzip[0] == 31 &&
                    gzip[1] == 139;
            }

            public static void FixGZipHeader(string filePath)
            {
                byte[] gzip = File.ReadAllBytes(filePath);

                if (gzip.Length > 2)
                {
                    gzip[0] = 31;
                    gzip[1] = 139;
                }

                File.WriteAllBytes(filePath, gzip);
            }
        }

        /// <summary>
        /// Unzips a Gzip file containing a tar
        /// </summary>
        /// <param name="file"></param>		
        public static void UnzipTgz(string file)
        {
            //	extract the tar from the gzip
            byte[] _buffer = new byte[4096];
            string _fnOut = Path.Combine(@"output\", Path.GetFileNameWithoutExtension(file) + ".tar");

            Stream _inStream = File.OpenRead(file);
            using (GZipInputStream _gZipStream = new GZipInputStream(_inStream))
            {
                using (System.IO.FileStream _fsOut = File.Create(_fnOut))
                {
                    StreamUtils.Copy(_gZipStream, _fsOut, _buffer);
                }
            }
        }

        /// <summary>
        /// Decompresses the contents of a TGZ file
        /// </summary>
        /// <param name="url">string url (path to the tgz file)</param>
        public static void DecompressTar(string gzipFileName, string tempDir)
        {
            // Use a 4K buffer. Any larger is a waste.    
            byte[] dataBuffer = new byte[4096];

            using (System.IO.Stream fs = new System.IO.FileStream(gzipFileName, FileMode.Open, FileAccess.Read))
            {
                fs.Seek(0, SeekOrigin.Begin);

                using (GZipInputStream gzipStream = new GZipInputStream(fs))
                {
                    // Change this to your needs
                    string fnOut = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(gzipFileName) + ".tar");

                    using (System.IO.FileStream fsOut = File.Create(fnOut))
                    {
                        StreamUtils.Copy(gzipStream, fsOut, dataBuffer);
                    }
                }
            }
        }

        public static void DecompressAndExtractTGZ(string gzipFileName, string tempDir)
        {
            //if (!GZipTool.IsGZipHeader(gzipFileName))
            //{
            //    GZipTool.FixGZipHeader(gzipFileName);
            //}

            Stream inStream = File.OpenRead(gzipFileName);

            TarArchive tarArchive = TarArchive.CreateInputTarArchive(inStream);
            tarArchive.ExtractContents(tempDir);
            tarArchive.Close();

            inStream.Close();
        }

        public static void Decompress(string gzipFilePath)
        {
            if (!GZipTool.IsGZipHeader(gzipFilePath))
            {
                GZipTool.FixGZipHeader(gzipFilePath);
            }

            FileInfo fileToDecompress = new FileInfo(gzipFilePath);

            using (System.IO.FileStream originalFileStream = fileToDecompress.OpenRead())
            {
                string currentFileName = fileToDecompress.FullName;
                string newFileName = currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length);

                using (System.IO.FileStream decompressedFileStream = File.Create(newFileName))
                {
                    using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream);
                        Console.WriteLine("Decompressed: {0}", fileToDecompress.Name);
                    }
                }
            }
        }

        public static void ExtractTar(string file, string tempDir)
        {
            string fnIn = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(file) + ".tar");

            using (System.IO.Stream fs = new System.IO.FileStream(file, FileMode.Open, FileAccess.Read))
            {
                using (GZipInputStream gzipStream = new GZipInputStream(fs))
                {
                    TarArchive _tarArchive = TarArchive.CreateInputTarArchive(gzipStream);
                    _tarArchive.ExtractContents(tempDir);
                    _tarArchive.Close();
                }
            }
        }

        public static void ExtractZip(string file, string tempDir)
        {
            string fnIn = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(file) + ".zip");

            using (System.IO.Stream fs = new System.IO.FileStream(file, FileMode.Open, FileAccess.Read))
            {
                using (ZipInputStream zipInputStream = new ZipInputStream(fs))
                {
                    ZipEntry zipEntry = zipInputStream.GetNextEntry();

                    while (zipEntry != null)
                    {
                        String entryFileName = zipEntry.Name;
                        // to remove the folder from the entry:- entryFileName = Path.GetFileName(entryFileName);
                        // Optionally match entrynames against a selection list here to skip as desired.
                        // The unpacked length is available in the zipEntry.Size property.

                        byte[] buffer = new byte[4096];     // 4K is optimum

                        // Manipulate the output filename here as desired.
                        String fullZipToPath = Path.Combine(tempDir, entryFileName);
                        string directoryName = Path.GetDirectoryName(fullZipToPath);
                        if (directoryName.Length > 0)
                        {
                            Directory.CreateDirectory(directoryName);
                        }

                        // Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
                        // of the file, but does not waste memory.
                        // The "using" will close the stream even if an exception occurs.
                        using (System.IO.FileStream streamWriter = File.Create(fullZipToPath))
                        {
                            StreamUtils.Copy(zipInputStream, streamWriter, buffer);
                        }

                        zipEntry = zipInputStream.GetNextEntry();
                    }
                }
            }
        }

        /// <summary>
        /// Saves a stream object to an output file
        /// </summary>
        /// <param name="path"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static bool SaveStreamToFile(string path, Stream stream)
        {
            if (stream.Length == 0) { return false; }

            using (System.IO.FileStream _fileStream = System.IO.File.Create(path, (int)stream.Length))
            {
                byte[] _bytesInStream = new byte[stream.Length];
                stream.Read(_bytesInStream, 0, (int)_bytesInStream.Length);
                _fileStream.Write(_bytesInStream, 0, _bytesInStream.Length);
            }

            return true;
        }

        /// <summary>
        ///	Writes the input string to the specified output file
        /// </summary>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool SaveStringToFile(string path, string data)
        {
            if (String.IsNullOrEmpty(data))
            {
                return false;
            }

            System.IO.File.WriteAllText(path, data);

            return true;
        }

        public static T GetObjFromJson<T>(string path) where T : class, new()
        {
            T obj = null;

            using (StreamReader reader = new StreamReader(path))
            {
                string json = reader.ReadToEnd();
                obj = JsonConvert.DeserializeObject<T>(json);
            }

            if (obj == null)
            {
                throw new Exception("Could not parse object from json at: " + path);
            }

            return obj;
        }

        public static T GetAndDeserializeJsonFromWeb<T>(string url, log4net.ILog log) where T : class
        {
            try
            {
                //  create the request and set the credentials from user input
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream receiveStream = response.GetResponseStream();
                Encoding encoding = System.Text.Encoding.GetEncoding("utf-8");
                StreamReader streamReader = new StreamReader(receiveStream, encoding);

                //  read the response from the stream->reader into a string for a XDocument.Parse();
                string data = streamReader.ReadToEnd();

                response.Close();
                streamReader.Close();

                return JsonConvert.DeserializeObject<T>(data);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message, ex);

                return null;
            }
        }
    }
}
