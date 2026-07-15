using Novacart.Api.Infrastructure;
using Novacart.Microservice.Hosting;

var app = MicroserviceBootstrap.BuildProductService(args);
await app.MigrateAndSeedAsync();
app.Run();

public partial class Program { }
