using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace ApiCenterMcpFetcher.Contracts;

[JsonConverter(typeof(StringEnumConverter))]
public enum TransportType
{
    [EnumMember(Value = "streamable")]
    Streamable,

    [EnumMember(Value = "sse")]
    SSE,

    [EnumMember(Value = "ws")]
    WS
}
