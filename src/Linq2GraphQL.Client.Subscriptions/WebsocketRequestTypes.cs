using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linq2GraphQL.Client.Subscriptions
{
    internal class WebsocketRequestTypes
    {
        internal const string PING = "ping";
        internal const string PONG = "pong";
        internal const string CONNECTION_INIT = "connection_init";

    }
}
