using Legion.Registry.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<ServiceRegistryImpl>();
app.MapGet("/", () => "Legion.Registry is running. Use a gRPC client.");

app.Run();