using ESRI.ArcGIS.Geodatabase;
using gbc.Constants;
using gbc.WebEOC7;
using log4net;
using SpatialConnect.Entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml.Linq;

using SCFields = SpatialConnect.Entity.Fields;

namespace gbc.DAL
{
    class DataRetriever
    {
        #region Members

        private static readonly ILog _log = LogManager.GetLogger(typeof(DataRetriever));

        private const string REST_ERROR_MSG = "Error encountered while attempting to retrieve data from REST endpoint {0}";

        #endregion


        #region Delegates

        public delegate void XDocumentHandler(object sender, XDocument doc);
        public delegate void StringHandler(object sender, string data);
        public delegate void FileHandler(object sender, string path);
        public delegate void XDocumentListHandler(object sender, List<XDocument> docs);
        public delegate void ICursorHandler(object sender, ICursor cursor);
        public delegate void DamageAssessmentHandler(object sender, string[] counties);

        #endregion


        #region Events

        public event XDocumentHandler OnGetDataAsXmlSuccess;
        public event XDocumentListHandler OnGetDataAsXmlListSuccess;
        public event StringHandler OnGetDataAsStringSuccess;
        public event FileHandler OnGetDataAsFileSuccess;
        public event XDocumentListHandler OnGetDataAsXmlDocsSuccess;
        public event ICursorHandler OnSdeGetDataAsIEnumerableSuccess;
        public event DamageAssessmentHandler OnGetDamageAssessmentSuccess;
        public event StringHandler OnGetJsonStringSuccess;

        #endregion


        #region Getters

        public void GetResponseAsXml(string Url)
        {
            retrieveXML(Url);
        }

        public void GetRestResponseAsXml(string Url)
        {
            retrieveRestXml(Url);
        }

        public void GetMultiFetchRestResponseAsXml(string Url, List<string> parameters)
        {
            retrieveMultiFetchRestXml(Url, parameters);
        }

        public void GetResponseAsString(string Url)
        {
            retrieveString(Url);
        }

        public void GetArcGISRestResponseAsString(string url, SCFields fields)
        {
            retrieveArcGISRestString(url, fields);
        }

        public void GetResponseAsFile(string Url, string Output)
        {
            retrieveFile(Url, Output);
        }

        public void GetFilteredWebEOCBoardDataAsString(WebEOCCredentials credentials, string board, string view, string ViewFilter)
        {
            retrieveWebEOC(credentials, board, view, ViewFilter);
        }

        public void GetWebEOCBoardDataAsString(WebEOCCredentials Credentials, string Board, string View)
        {
            retrieveWebEOC(Credentials, Board, View);
        }

        public void GetPasswordProtectedResponseAsXml(string Username, string Password, string Url)
        {
            retrievePasswordProtectedXML(Username, Password, Url);
        }

        public void GetPasswordProtectedResponseAsFile(NetworkCredential credentials, string url, string output)
        {
            retrievePasswordProtectedFile(credentials, url, output);
        }

        public void GetICursorFromSde(Config config, Container container)
        {
            retrieveSDECursor(config, container);
        }

        public void GetDamageAssessment()
        {
            retrieveDamageAssessment();
        }

        public void GetGzippedTarFile(string url, string output)
        {
            retrieveFile(url, output);
        }

        #endregion


        #region Retrievers

        /// <summary>
        /// Retrieves an IEnumerable from SDE
        /// </summary>
        /// <param name="sdeSource"></param>
        private void retrieveSDECursor(Config config, Container container)
        {
            var sdeManager = new SDEManager(config);

            if (OnSdeGetDataAsIEnumerableSuccess != null)
            {
                OnSdeGetDataAsIEnumerableSuccess(this, sdeManager.GetCursor(container.license_type, container.source, container.where_clause));
            }
        }

        /// <summary>
        /// Retrieves a resource in xml format
        /// </summary>
        /// <param name="url"></param>
        private void retrieveXML(string url)
        {
            var doc = XDocument.Load(url);
            if (OnGetDataAsXmlSuccess != null)
            {
                OnGetDataAsXmlSuccess(this, doc);
            }
        }

        /// <summary>
        /// Retrieves a REST Api response in XML format
        /// </summary>
        /// <param name="url"></param>
        private void retrieveRestXml(string url)
        {
            var responseString = string.Empty;

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ContentLength = 0;
            request.ContentType = "text/xml";

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    //  grab the stream
                    using (var stream = response.GetResponseStream())
                    {
                        //  read the stream
                        using (var reader = new StreamReader(stream))
                        {
                            responseString = reader.ReadToEnd();
                        }
                    }
                }
            }

            if (OnGetDataAsXmlSuccess != null)
            {
                OnGetDataAsXmlSuccess(this, XDocument.Parse(responseString));
            }
        }

        private void retrieveMultiFetchRestXml(string url, List<string> parameters)
        {
            var restResponses = new List<XDocument>();
            var requestUrl = string.Empty;

            foreach (var param in parameters)
            {
                try
                {
                    var responseString = string.Empty;
                    requestUrl = string.Format(url, param);
                    var request = (HttpWebRequest)WebRequest.Create(requestUrl);
                    request.Method = "GET";
                    request.ContentLength = 0;
                    request.ContentType = "text/xml";

                    using (var response = (HttpWebResponse)request.GetResponse())
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            //  grab the stream
                            using (var stream = response.GetResponseStream())
                            {
                                //  read the stream
                                using (var reader = new StreamReader(stream))
                                {
                                    responseString = reader.ReadToEnd();
                                }
                            }
                        }
                    }

                    restResponses.Add(XDocument.Parse(responseString));
                }
                catch (Exception ex)
                {
                    var msg = string.Format(REST_ERROR_MSG, requestUrl);
                    throw new Exception(msg, ex.InnerException);
                }
            }

            if (OnGetDataAsXmlListSuccess != null)
            {
                OnGetDataAsXmlListSuccess(this, restResponses);
            }
        }

        /// <summary>
        /// Retrieves a resource in string format
        /// </summary>
        /// <param name="url"></param>
        private void retrieveString(string url)
        {
            string _data = string.Empty;

            using (WebClient client = new WebClient())
            {
                _data = client.DownloadString(url);
            }

            _log.Debug("Download of client data complete.");

            //	invoke the getdatasuccessasstring event
            if (OnGetDataAsStringSuccess != null)
            {
                OnGetDataAsStringSuccess(this, _data);
            }
        }

        private void retrieveArcGISRestString(string url, SCFields fields)
        {
            StringBuilder sBuilder = new StringBuilder();

            string encodedComma = "%2C";
            string _data = string.Empty;

            url = url + "/query?where=OBJECTID+>+0&returnGeometry=true&outFields={0}&f=pjson";

            foreach (Mapping mapping in fields.mappings)
            {
                sBuilder.Append(mapping.field_from);
                sBuilder.Append(encodedComma);
            }
            url = string.Format(url, sBuilder.ToString());

            using (WebClient client = new WebClient())
            {
                _data = client.DownloadString(url);
            }

            _log.Debug("Download of client data complete.");

            //	invoke the getdatasuccessasstring event
            if (OnGetDataAsStringSuccess != null)
            {
                OnGetDataAsStringSuccess(this, _data);
            }
        }

        /// <summary>
        /// Retrieves a file from the specified url and places it with at the
        /// position of the output parameter.
        /// </summary>
        /// <param name="url">string url (resource location)</param>
        /// <param name="output">string output (resource save path with filename)</param>
        private void retrieveFile(string url, string output)
        {
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(url, output);
            }

            if (OnGetDataAsFileSuccess != null)
            {
                OnGetDataAsFileSuccess(this, output);
            }
        }

        /// <summary>
        ///	Retrieves WebEOC board data using the supplied credentials object
        ///	and board/view combination.
        /// </summary>
        /// <param name="url"></param>
        private void retrieveWebEOC(WebEOCCredentials credentials, string board, string view, string viewFilter)
        {
            var api = new WebEOC7.APISoapClient();

            string response = api.GetFilteredData(credentials, board, view, new[] { viewFilter }, "");

            response = response.Replace("\n", " ");

            //  this removes some random formatting garbage, no longer needed but leaving 'just in case'
            //response = response.Replace("&#xD;", " ");
            //response = response.Replace("&#xA;", " ");
            //response = response.Replace("&quot;", "");
            //response = response.Replace("\x00", "");
            //response.Trim(' ', '\0');

            response = CleanResponse(response);

            if (response != null && !String.IsNullOrEmpty(response))
            {
                if (OnGetDataAsStringSuccess != null)
                {
                    OnGetDataAsStringSuccess(this, response);
                }
            }
        }

        private string CleanResponse(string response)
        {
            var strBuilder = new StringBuilder();
            foreach (char c in response)
            {
                strBuilder.Append(Char.IsControl(c) ? ' ' : c);
            }
            return strBuilder.ToString();
        }

        /// <summary>
        ///	Retrieves WebEOC board data using the supplied credentials object
        ///	and board/view combination.
        /// </summary>
        /// <param name="url"></param>
        private void retrieveWebEOC(WebEOCCredentials credentials, string board, string view)
        {
            WebEOC7.APISoapClient client = new WebEOC7.APISoapClient();
            WebEOC7.WebEOCCredentials webEoc7Credentials = new WebEOC7.WebEOCCredentials()
            {
                Incident = credentials.Incident,
                Jurisdiction = credentials.Jurisdiction,
                Password = credentials.Password,
                Position = credentials.Position,
                Username = credentials.Username
            };

            string response = client.GetData(webEoc7Credentials, board, view);

            //var api = new API();

            //string response = api.GetData(credentials, board, view);

            //response = response.Replace("\n", " ");
            //response = response.Replace("&#xD;", " ");
            //response = response.Replace("&#xA;", " ");
            //response = response.Replace("&quot;", " ");
            //response = response.Replace("&#x0;", " ");
            //response = response.Replace("\0", " ");

            //_log.Debug(string.Format("Got WebEOC Response: {0}", response));

            if (response != null && !String.IsNullOrEmpty(response))
            {
                if (OnGetDataAsStringSuccess != null)
                {
                    OnGetDataAsStringSuccess(this, response);
                }
            }
        }

        /// <summary>
        /// Retrieves XML from a password protected URL
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="url"></param>
        private void retrievePasswordProtectedXML(string username, string password, string url)
        {
            NetworkCredential credentials = new NetworkCredential(username, password);

            //  create the request and set the credentials from user input
            var _request = (HttpWebRequest)WebRequest.Create(url);
            _request.Credentials = credentials;

            //  create the proxy
            _request.Proxy = WebRequest.DefaultWebProxy;
            _request.Proxy.Credentials = credentials;

            var _response = (HttpWebResponse)_request.GetResponse();
            var _receiveStream = _response.GetResponseStream();
            var _encode = System.Text.Encoding.GetEncoding("utf-8");
            var _reader = new StreamReader(_receiveStream, _encode);

            //  read the response from the stream->reader into a string for a XDocument.Parse();
            var _data = _reader.ReadToEnd();

            _response.Close();
            _reader.Close();

            _log.Debug("Download of client data complete.");

            if (!String.IsNullOrEmpty(_data))
            {
                if (OnGetDataAsXmlSuccess != null)
                {
                    OnGetDataAsXmlSuccess(this, XDocument.Parse(_data));
                }
            }
        }

        /// <summary>
        /// Retrieves a password protected file from supplied URL
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="url"></param>
        private void retrievePasswordProtectedFile(NetworkCredential credentials,
            string url, string output)
        {
            //	retrieve the file
            using (var _client = new WebClient())
            {
                _client.Credentials = credentials;
                _client.DownloadFile(url, output);
            }

            //	fire event if listeners are subscribed
            if (OnGetDataAsFileSuccess != null)
            {
                OnGetDataAsFileSuccess(this, output);
            }
        }

        /// <summary>
        /// Retrieves Damage assessment data
        /// </summary>
        private void retrieveDamageAssessment()
        {
            //  have to pull this for now until I can find the damage assessment service info again
            //try
            //{
            //    HsDamageWebServiceClient client = new HsDamageWebServiceClient();
            //    client.Open();

            //    string[] counties = client.getAfftectedCounties();

            //    if (counties != null)
            //    {
            //        if (OnGetDamageAssessmentSuccess != null)
            //        {
            //            OnGetDamageAssessmentSuccess(this, counties);
            //        }
            //    }
            //    else
            //    {
            //        throw new Exception(ExceptionConstants.ResponseExceptions.NO_DATA_RETURNED);
            //    }
            //}
            //catch
            //{
            //    throw new Exception(ExceptionConstants.ResponseExceptions.ERROR_CONTACTING_REMOTE_SERVER);
            //}
        }

        #endregion
    }
}
