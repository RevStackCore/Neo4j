using System;
using Neo4jClient;

namespace RevStackCore.Neo4j
{
    public class Neo4jDbContext
    {
        private const string DEFAULT_CONNECTION = "http://localhost:7474/db/data";
        private GraphClient _client;

        public Neo4jDbContext()
        {
            _client = new GraphClient(new Uri(DEFAULT_CONNECTION));
            _client.Connect();
        }

        public Neo4jDbContext(string uri)
        {
            _client = new GraphClient(new Uri(uri));
            _client.Connect();
        }

        public Neo4jDbContext(string uri,string user, string password)
        {
            _client = new GraphClient(new Uri(uri), user, password);
            _client.Connect();
        }

        public GraphClient GraphClient
        {
            get
            {
                if (!_client.IsConnected) _client.Connect();
                return _client;
            }
        }

    }
}
