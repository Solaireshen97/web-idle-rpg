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
            <button id="addGoldButton" type="button">Add +10 Gold</button>
            <button id="fightButton" type="button">Fight</button>
          </section>

          <section>
            <h2>Player Status</h2>
            <div>Id: <span id="playerStatusId">-</span></div>
            <div>Name: <span id="playerStatusName">-</span></div>
            <div>Gold: <span id="playerStatusGold">-</span></div>
            <div>Attack: <span id="playerStatusAttack">-</span></div>
            <div>MaxHp: <span id="playerStatusMaxHp">-</span></div>
            <div>CurrentHp: <span id="playerStatusCurrentHp">-</span></div>
            <div>CreatedAt: <span id="playerStatusCreatedAt">-</span></div>
            <div>UpdatedAt: <span id="playerStatusUpdatedAt">-</span></div>
          </section>

          <section>
            <h2>Fight Result</h2>
            <div id="fightResult">No fight yet.</div>
          </section>

          <h2>Debug</h2>
          <pre id="result">No player loaded.</pre>

          <script>
            const resultElement = document.getElementById("result");
            const playerIdInput = document.getElementById("playerId");
            const playerNameInput = document.getElementById("playerName");
            const playerStatusIdElement = document.getElementById("playerStatusId");
            const playerStatusNameElement = document.getElementById("playerStatusName");
            const playerStatusGoldElement = document.getElementById("playerStatusGold");
            const playerStatusAttackElement = document.getElementById("playerStatusAttack");
            const playerStatusMaxHpElement = document.getElementById("playerStatusMaxHp");
            const playerStatusCurrentHpElement = document.getElementById("playerStatusCurrentHp");
            const playerStatusCreatedAtElement = document.getElementById("playerStatusCreatedAt");
            const playerStatusUpdatedAtElement = document.getElementById("playerStatusUpdatedAt");
            const fightResultElement = document.getElementById("fightResult");

            function showResult(data) {
              resultElement.textContent = JSON.stringify(data, null, 2);
            }

            function showPlayerStatus(player) {
              if (!player) {
                playerStatusIdElement.textContent = "-";
                playerStatusNameElement.textContent = "-";
                playerStatusGoldElement.textContent = "-";
                playerStatusAttackElement.textContent = "-";
                playerStatusMaxHpElement.textContent = "-";
                playerStatusCurrentHpElement.textContent = "-";
                playerStatusCreatedAtElement.textContent = "-";
                playerStatusUpdatedAtElement.textContent = "-";
                return;
              }

              playerStatusIdElement.textContent = player.id;
              playerStatusNameElement.textContent = player.name;
              playerStatusGoldElement.textContent = player.gold;
              playerStatusAttackElement.textContent = player.attack;
              playerStatusMaxHpElement.textContent = player.maxHp;
              playerStatusCurrentHpElement.textContent = player.currentHp;
              playerStatusCreatedAtElement.textContent = player.createdAt;
              playerStatusUpdatedAtElement.textContent = player.updatedAt;
            }

            async function loadPlayer() {
              const id = playerIdInput.value;
              const response = await fetch(`/api/players/${encodeURIComponent(id)}`);
              const text = await response.text();

              if (!response.ok) {
                showPlayerStatus(null);
                fightResultElement.textContent = "Load failed.";
                showResult({ error: `Load failed (${response.status})`, detail: text });
                return;
              }

              const player = JSON.parse(text);
              showPlayerStatus(player);
              showResult(player);
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
                showPlayerStatus(null);
                fightResultElement.textContent = "Create failed.";
                showResult({ error: `Create failed (${response.status})`, detail: text });
                return;
              }

              const player = JSON.parse(text);
              playerIdInput.value = player.id;
              showPlayerStatus(player);
              fightResultElement.textContent = "Player created.";
              showResult(player);
            }

            async function addGold() {
              const id = playerIdInput.value;
              const response = await fetch(`/api/players/${encodeURIComponent(id)}/gold`, {
                method: "POST"
              });

              const text = await response.text();
              if (!response.ok) {
                showPlayerStatus(null);
                fightResultElement.textContent = "Add gold failed.";
                showResult({ error: `Add gold failed (${response.status})`, detail: text });
                return;
              }

              const player = JSON.parse(text);
              showPlayerStatus(player);
              fightResultElement.textContent = "Gold +10 applied.";
              showResult(player);
            }

            async function fight() {
              const id = playerIdInput.value;
              const response = await fetch(`/api/players/${encodeURIComponent(id)}/fight`, {
                method: "POST"
              });

              const text = await response.text();
              if (!response.ok) {
                showPlayerStatus(null);
                fightResultElement.textContent = "Fight failed.";
                showResult({ error: `Fight failed (${response.status})`, detail: text });
                return;
              }

              const fightResult = JSON.parse(text);
              showPlayerStatus(fightResult.player);
              const statusText = fightResult.isVictory ? "Yes" : "No";
              const enemyText = `Enemy: ${fightResult.enemyName} (HP ${fightResult.enemyMaxHp}, ATK ${fightResult.enemyAttack})`;
              const resultText = `Victory: ${statusText} | Gold Reward: ${fightResult.goldReward} | Player HP: ${fightResult.player.currentHp}/${fightResult.player.maxHp}`;
              fightResultElement.textContent = `${enemyText} | ${resultText} | ${fightResult.summary}`;
              showResult(fightResult);
            }

            document.getElementById("loadPlayerButton").addEventListener("click", loadPlayer);
            document.getElementById("createPlayerButton").addEventListener("click", createPlayer);
            document.getElementById("addGoldButton").addEventListener("click", addGold);
            document.getElementById("fightButton").addEventListener("click", fight);
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
    var serverLocation = response.Headers.Location?.ToString();
    var location = "/api/players";
    if (player is not null)
    {
        location = $"/api/players/{player.Id}";
    }

    if (!string.IsNullOrWhiteSpace(serverLocation))
    {
        location = serverLocation;
    }

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

app.MapPost("/api/players/{id:int}/gold", async (IHttpClientFactory httpClientFactory, int id) =>
{
    var serverApi = httpClientFactory.CreateClient("ServerApi");
    var response = await serverApi.PostAsync($"/api/players/{id}/gold", content: null);

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

app.MapPost("/api/players/{id:int}/fight", async (IHttpClientFactory httpClientFactory, int id) =>
{
    var serverApi = httpClientFactory.CreateClient("ServerApi");
    var response = await serverApi.PostAsync($"/api/players/{id}/fight", content: null);

    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        return Results.NotFound();
    }

    if (!response.IsSuccessStatusCode)
    {
        return await ForwardResponseAsContentAsync(response);
    }

    var fightResult = await response.Content.ReadFromJsonAsync<FightResultDto>();
    return fightResult is null
        ? Results.StatusCode(StatusCodes.Status502BadGateway)
        : Results.Ok(fightResult);
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
