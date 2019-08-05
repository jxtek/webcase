using System.Data;
using System.Text;

namespace WebReady.Db
{
    public class DbSource : IKeyable<string>
    {
        readonly string _name;

        // IP host or unix domain socket
        readonly string host;

        // IP port
        readonly int port;

        // default database name
        readonly string database;

        readonly string username;

        readonly string password;

        readonly string _connstr;

        internal DbSource(string name, JObj s)
        {
            _name = name;

            s.Get(nameof(host), ref host);
            s.Get(nameof(port), ref port);
            s.Get(nameof(database), ref database);
            s.Get(nameof(username), ref username);
            s.Get(nameof(password), ref password);

            // initialize connection string
            //
            var sb = new StringBuilder();
            sb.Append("Host=").Append(host);
            sb.Append(";Port=").Append(port);
            sb.Append(";Database=").Append(database);
            sb.Append(";Username=").Append(username);
            sb.Append(";Password=").Append(password);
            sb.Append(";Read Buffer Size=").Append(1024 * 32);
            sb.Append(";Write Buffer Size=").Append(1024 * 32);
            sb.Append(";No Reset On Close=").Append(true);

            _connstr = sb.ToString();
        }

        public string Name => _name;

        public string Key => _name;

        public string ConnectionString => _connstr;

        public DbContext NewDbContext(IsolationLevel? level = null)
        {
            var dc = new DbContext(this);
            if (level != null)
            {
                dc.Begin(level.Value);
            }

            return dc;
        }
    }
}