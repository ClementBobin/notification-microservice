namespace Genkan.Shared.Configuration;

/// <summary>One service whose OpenAPI document is merged into the gateway's combined document.</summary>
public sealed class SwaggerSource
{
    /// <summary>Display name of the service (also used to disambiguate clashing schema names).</summary>
    public string Name { get; set; } = "";

    /// <summary>Address of the service's OpenAPI document, reachable from the gateway (not from the client).</summary>
    public string Url { get; set; } = "";
}

/// <summary>
/// Section "Swagger" of the gateway configuration. The combined Swagger UI is off unless <see cref="Enabled"/> is true
/// (environment variable <c>Swagger__Enabled=true</c>, <c>SWAGGER_ENABLED</c> in the compose files). It is read at startup.
/// </summary>
public sealed class SwaggerOptions
{
    public const string Section = "Swagger";

    public bool Enabled { get; set; }

    public string Title { get; set; } = "Genkan API";

    /// <summary>How long the merged document is reused before the services are queried again (a partial result is kept for 5 s at most).</summary>
    public int CacheSeconds { get; set; } = 60;

    public List<SwaggerSource> Sources { get; set; } = [];
}
