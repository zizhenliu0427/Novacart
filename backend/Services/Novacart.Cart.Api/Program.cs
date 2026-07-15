using Novacart.Api.Infrastructure;
using Novacart.Microservice.Hosting;

var app = MicroserviceBootstrap.BuildCartService(args);
await app.MigrateAndSeedAsync();
app.Run();

public partial class Program { }
