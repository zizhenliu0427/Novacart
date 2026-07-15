var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var authDb = postgres.AddDatabase("novacart-auth", "novacart_auth");
var productDb = postgres.AddDatabase("novacart-product", "novacart_product");
var commerceDb = postgres.AddDatabase("novacart-commerce", "novacart_commerce");
var cartDb = postgres.AddDatabase("novacart-cart", "novacart_cart");

var redis = builder.AddRedis("redis");

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin();

var jaeger = builder.AddContainer("jaeger", "jaegertracing/all-in-one", "1.57")
    .WithEnvironment("COLLECTOR_OTLP_ENABLED", "true")
    .WithHttpEndpoint(16686, 16686, name: "ui")
    .WithEndpoint(4317, 4317, name: "otlp");

var elasticsearch = builder.AddContainer("elasticsearch", "docker.elastic.co/elasticsearch/elasticsearch", "8.15.0")
    .WithEnvironment("discovery.type", "single-node")
    .WithEnvironment("xpack.security.enabled", "false")
    .WithEnvironment("ES_JAVA_OPTS", "-Xms512m -Xmx512m")
    .WithHttpEndpoint(9200, 9200, name: "http");

const string otelEndpoint = "http://jaeger:4317";
const string elasticsearchUrl = "http://elasticsearch:9200";

var authApi = builder.AddProject<Projects.Novacart_Auth_Api>("auth-api")
    .WithReference(authDb)
    .WithReference(redis)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelEndpoint)
    .WithEnvironment("OTEL_SERVICE_NAME", "auth-api")
    .WaitFor(postgres)
    .WaitFor(redis)
    .WaitFor(jaeger);

var productApi = builder.AddProject<Projects.Novacart_Product_Api>("product-api")
    .WithReference(productDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelEndpoint)
    .WithEnvironment("OTEL_SERVICE_NAME", "product-api")
    .WithEnvironment("Elasticsearch__Enabled", "true")
    .WithEnvironment("Elasticsearch__Url", elasticsearchUrl)
    .WithEnvironment("Elasticsearch__IndexName", "novacart-products")
    .WaitFor(postgres)
    .WaitFor(redis)
    .WaitFor(rabbitmq)
    .WaitFor(jaeger)
    .WaitFor(elasticsearch);

var cartApi = builder.AddProject<Projects.Novacart_Cart_Api>("cart-api")
    .WithReference(cartDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(productApi)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelEndpoint)
    .WithEnvironment("OTEL_SERVICE_NAME", "cart-api")
    .WithEnvironment("Microservices__IsolatedDatabases", "true")
    .WithEnvironment("Microservices__UseRefitCatalog", "true")
    .WaitFor(postgres)
    .WaitFor(redis)
    .WaitFor(rabbitmq)
    .WaitFor(productApi)
    .WaitFor(jaeger);

var orderApi = builder.AddProject<Projects.Novacart_Order_Api>("order-api")
    .WithReference(commerceDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(productApi)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelEndpoint)
    .WithEnvironment("OTEL_SERVICE_NAME", "order-api")
    .WithEnvironment("Microservices__IsolatedDatabases", "true")
    .WithEnvironment("Microservices__UseRefitCatalog", "true")
    .WithEnvironment("RabbitMQ__ManagementUrl", "http://rabbitmq:15672")
    .WaitFor(postgres)
    .WaitFor(redis)
    .WaitFor(rabbitmq)
    .WaitFor(productApi)
    .WaitFor(jaeger);

builder.AddProject<Projects.Novacart_Gateway>("gateway")
    .WithReference(authApi)
    .WithReference(productApi)
    .WithReference(cartApi)
    .WithReference(orderApi)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelEndpoint)
    .WithEnvironment("OTEL_SERVICE_NAME", "gateway")
    .WithExternalHttpEndpoints()
    .WaitFor(jaeger);

builder.Build().Run();
