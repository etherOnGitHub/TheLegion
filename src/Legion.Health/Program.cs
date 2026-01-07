using Legion.Health.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<HeartbeatImpl>();
app.MapGet("/", () => "Legion.Health is running. Use a gRPC client.");

app.Run();