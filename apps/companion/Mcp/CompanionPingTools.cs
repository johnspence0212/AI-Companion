using System.ComponentModel;
using ModelContextProtocol.Server;

namespace EnterpriseStarter.Companion.Mcp;

[McpServerToolType]
public sealed class CompanionPingTools
{
    [McpServerTool(Name = "ping"), Description("Confirm the MCP adapter is authenticated.")]
    public static string Ping() => "ok";
}
