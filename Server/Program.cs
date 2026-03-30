var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/api/ping", () => Results.Ok(new { message = "pong" }));

app.Run();
