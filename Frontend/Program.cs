var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () =>
    Results.Content(
        """
        <!doctype html>
        <html lang="en">
        <head>
          <meta charset="utf-8" />
          <title>Web Idle RPG - Frontend</title>
        </head>
        <body>
          <h1>Web Idle RPG Frontend</h1>
          <p>This project is currently responsible for UI only.</p>
        </body>
        </html>
        """,
        "text/html"));

app.Run();
