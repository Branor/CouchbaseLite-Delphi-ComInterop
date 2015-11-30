using Couchbase.Lite;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Couchbase.Lite.Auth;

namespace CouchbaseLiteManager
{

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("6df7e4da-7a81-457a-b0af-7b80d7f10694")]
    public class CouchbaseLiteFacade : ICouchbaseLiteFacade
    {

        private Manager _manager;
        private Database _database;

        private const string DB_NAME = "delphidb";
        private const string TAG = "CouchbaseLiteDemo";

        private Replication pull;
        private Replication push;

        public CouchbaseLiteFacade()
        {
            _manager = Manager.SharedInstance;

            _database = _manager.GetDatabase(DB_NAME + DateTime.Now.GetHashCode());
        }

        public string Insert(string docId, string propertiesJson)
        {
            Document document;

            if (string.IsNullOrEmpty(docId))
                document = _database.CreateDocument();
            else
                document = _database.GetDocument(docId);

            if (document == null)
                return "Error creating new document.";
            if (document.Properties != null && document.Properties.Count > 0)
                return "Document already exists.";

            var properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(propertiesJson);
            var revision = document.PutProperties(properties);

            return document.Id;
        }

        public string Get(string docId)
        {
            var doc = _database.GetExistingDocument(docId);

            if (doc != null)
                return JsonConvert.SerializeObject(doc.Properties, Formatting.Indented);
            else
                return null;
        }

        public string Update(string documentId, string updatedPropertiesJson)
        {
            var currentDocument = _database.GetDocument(documentId);
            var properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(updatedPropertiesJson);

            var updatedProperties = new Dictionary<string,object>(currentDocument.Properties);

            foreach (var propKey in properties.Keys)
            {
                if (updatedProperties.ContainsKey(propKey))
                {
                    updatedProperties[propKey] = properties[propKey];
                }
            }

            var updatedRev = currentDocument.PutProperties(updatedProperties);
            return JsonConvert.SerializeObject(updatedRev.Properties, Formatting.Indented);
        }

        public bool Delete(string documentId)
        {
            var document = _database.GetDocument(documentId);
            document.Delete();

            return document.Deleted;
        }

        public void StartSyncGateway(string url = "")
        {
            pull = _database.CreatePullReplication(CreateSyncUri());
            push = _database.CreatePushReplication(CreateSyncUri());

            var authenticator = AuthenticatorFactory.CreateBasicAuthenticator("david", "12345");
            pull.Authenticator = authenticator;
            push.Authenticator = authenticator;

            pull.Continuous = true;
            push.Continuous = true;

            pull.Changed += Pull_Changed;
            push.Changed += Push_Changed;

            pull.Start();
            push.Start();
        }

        private void Push_Changed(object sender, ReplicationChangeEventArgs e)
        {
            Console.WriteLine("Push: " + e.Status + " " + e.ChangesCount);
        }

        private void Pull_Changed(object sender, ReplicationChangeEventArgs e)
        {
            Console.WriteLine("Pull: " + e.Status, " " + e.ChangesCount);
        }

        public void StopSyncGateway()
        {
            pull.Stop();
            push.Stop();
        }

        private Uri CreateSyncUri()
        {
            Uri syncUri = null;
            string scheme = "http";
            string host = "localhost";
            int port = 4984;
            string dbName = "delphi";
            try
            {
                var uriBuilder = new UriBuilder(scheme, host, port, dbName);
                syncUri = uriBuilder.Uri;
            }
            catch (UriFormatException e)
            {
                Debug.WriteLine("{0}: Cannot create sync uri = {1}", TAG, e.Message);
            }
            return syncUri;
        }
    }
}
