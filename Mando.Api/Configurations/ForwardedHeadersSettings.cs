using System.ComponentModel.DataAnnotations;

namespace Mando.Api.Configurations;

public class ForwardedHeadersSettings
{
    public const string SectionName = "ForwardedHeaders";

    public bool Enabled { get; set; }

    [Range(1, 10)]
    public int ForwardLimit { get; set; } = 1;

    public List<string> KnownProxies { get; set; } = [];
}
