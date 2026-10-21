namespace Genkan.Shared.Configurations;

public static class GenkanSchema
{
    /// <summary>The one PostgreSQL schema shared by every service. Only Genkan.Migrator creates or alters it.</summary>
    public const string Name = "genkan";
}
