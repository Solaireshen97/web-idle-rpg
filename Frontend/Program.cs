using System.Net.Http.Json;
using Shared.Players;

var builder = WebApplication.CreateBuilder(args);
var serverApiBaseUrl = builder.Configuration["ServerApiBaseUrl"] ?? "http://localhost:5238";

builder.Services.AddHttpClient("ServerApi", client =>
{
    client.BaseAddress = new Uri(serverApiBaseUrl);
});

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
          <p>Minimal player view using current Server API.</p>

          <section>
            <h2>Create Player</h2>
            <input id="playerName" type="text" placeholder="player name" />
            <button id="createPlayerButton" type="button">Create</button>
          </section>

          <section>
            <h2>Load Player</h2>
            <input id="playerId" type="number" min="1" value="1" />
            <button id="loadPlayerButton" type="button">Load</button>
          </section>

          <h2>Result</h2>
          <pre id="result">No player loaded.</pre>

          <script>
            const resultElement = document.getElementById("result");
            const playerIdInput = document.getElementById("playerId");
            const playerNameInput = document.getElementById("playerName");

            function showResult(data) {
              resultElement.textContent = JSON.stringify(data, null, 2);
            }

            async function loadPlayer() {
              const id = playerIdInput.value;
              const response = await fetch(`/api/players/${encodeURIComponent(id)}`);
              const text = await response.text();

              if (!response.ok) {
                showResult({ error: `Load failed (${response.status})`, detail: text });
                return;
              }

              showResult(JSON.parse(text));
            }

            async function createPlayer() {
              const name = playerNameInput.value.trim();
              const response = await fetch("/api/players", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ name })
              });

              const text = await response.text();
              if (!response.ok) {
                showResult({ error: `Create failed (${response.status})`, detail: text });
                return;
              }

              const player = JSON.parse(text);
              playerIdInput.value = player.id;
              showResult(player);
            }

            document.getElementById("loadPlayerButton").addEventListener("click", loadPlayer);
            document.getElementById("createPlayerButton").addEventListener("click", createPlayer);
          </script>
        </body>
        </html>
        """,
        "text/html"));

app.MapPost("/api/players", async (IHttpClientFactory httpClientFactory, CreatePlayerRequest request) =>
{
    var serverApi = httpClientFactory.CreateClient("ServerApi");
    var response = await serverApi.PostAsJsonAsync("/api/players", request);

    if (!response.IsSuccessStatusCode)
    {
        return await ForwardResponseAsContentAsync(response);
    }

    var player = await response.Content.ReadFromJsonAsync<PlayerDto>();
    var location = response.Headers.Location?.ToString() ?? (player is null ? "/api/players" : $"/api/players/{player.Id}");
    return player is null
        ? Results.StatusCode(StatusCodes.Status502BadGateway)
        : Results.Created(location, player);
});

app.MapGet("/api/players/{id:int}", async (IHttpClientFactory httpClientFactory, int id) =>
{
    var serverApi = httpClientFactory.CreateClient("ServerApi");
    var response = await serverApi.GetAsync($"/api/players/{id}");

    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        return Results.NotFound();
    }

    if (!response.IsSuccessStatusCode)
    {
        return await ForwardResponseAsContentAsync(response);
    }

    var player = await response.Content.ReadFromJsonAsync<PlayerDto>();
    return player is null
        ? Results.StatusCode(StatusCodes.Status502BadGateway)
        : Results.Ok(player);
});

app.Run();

static async Task<IResult> ForwardResponseAsContentAsync(HttpResponseMessage response)
{
    var content = await response.Content.ReadAsStringAsync();
    return Results.Content(
        content,
        contentType: response.Content.Headers.ContentType?.MediaType ?? "application/json",
        statusCode: (int)response.StatusCode);
}
