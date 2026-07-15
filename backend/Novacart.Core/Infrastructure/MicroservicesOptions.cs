namespace Novacart.Api.Infrastructure;

public class MicroservicesOptions
{
    public const string SectionName = "Microservices";

    public bool IsolatedDatabases { get; set; }

    /// <summary>When true (default with isolated DBs), Cart/Order read catalog via Refit instead of ProductRead DB.</summary>
    public bool UseRefitCatalog { get; set; } = true;
}
