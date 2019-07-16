using NLua;
using System;
using System.Collections.Generic;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Client;
using System.Threading;
using System.Threading.Tasks;

namespace nlua_test
{
    public enum ExitCode : int
    {
        Ok = 0,
        ErrorCreateApplication = 0x11,
        ErrorDiscoverEndpoints = 0x12,
        ErrorCreateSession = 0x13,
        ErrorBrowseNamespace = 0x14,
        ErrorCreateSubscription = 0x15,
        ErrorMonitoredItem = 0x16,
        ErrorAddSubscription = 0x17,
        ErrorRunning = 0x18,
        ErrorNoKeepAlive = 0x30,
        ErrorInvalidCommandLine = 0x100
    };

    class OpcUa : IDisposable
    {
        private Session session;
        private Lua lua;

        public OpcUa(Lua lua)
        {
            this.lua = lua;
        }

        public void Dispose()
        {
            if (session != null && session.Connected)
                session.Close();
        }

        public bool open(string endpoint)
        {
            var application = new ApplicationInstance
            {
                ApplicationName = "OPC UA Client Lua",
                ApplicationType = ApplicationType.Client,
                ConfigSectionName = "Opc.Ua.Client.Lua"
            };

            // load the application configuration.
            var taskConfig = application.LoadApplicationConfiguration(false);
            taskConfig.Wait();
            var config = taskConfig.Result;
            var selectedEndpoint = CoreClientUtils.SelectEndpoint(endpoint, false, 15000);
            var endpointConfiguration = EndpointConfiguration.Create(config);
            var __endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);
            var taskSession = Session.Create(config, __endpoint, false, "OPC UA Client Lua", 60000, new UserIdentity(new AnonymousIdentityToken()), null);
            taskSession.Wait();
            this.session = taskSession.Result;
            return true;
        }

        public LuaTable read(LuaTable ids)
        {
            var key = "__opcua__tmp";
            lua.NewTable(key);
            var tmp = lua[key] as LuaTable;
            lua[key] = null;

            if (this.session == null)
                return tmp;

            var items = new ReadValueIdCollection();

            foreach(var k in ids.Keys)
            {
                var id = ids[k].ToString();

                if (string.IsNullOrEmpty(id))
                    continue;

                items.Add(new ReadValueId()
                {
                    NodeId = new NodeId(id),
                    AttributeId = Attributes.Value,
                    IndexRange = null,
                    DataEncoding = null,
                });
            }

            var results = new DataValueCollection();
            var diagnosticInfos = new DiagnosticInfoCollection();

            this.session.Read(
                null,
                0,
                TimestampsToReturn.Both,
                items,
                out results,
                out diagnosticInfos
            );

            for(int i = 0;i < items.Count;i++)
            {
                var id = items[i].NodeId.ToString();
                var value = results[i].Value;
                tmp[id] = value;
            }

            return tmp;
        }
    }

    class Program
    {
        static int Main(string[] args)
        {
            using (var lua = new Lua())
            {
                using (var opcua = new OpcUa(lua))
                {
                    lua["opcua"] = opcua;
                    lua.DoFile("opcua.lua");
                }
            }
            return 0;
        }
    }
}
