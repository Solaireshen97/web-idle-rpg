using System.Net.Http.Json;
using Shared.Players;

var builder = WebApplication.CreateBuilder(args);
var serverApiBaseUrl = builder.Configuration["ServerApiBaseUrl"] ?? "http://localhost:5238";

builder.Services.AddHttpClient("ServerApi", client =>
{
    client.BaseAddress = new Uri(serverApiBaseUrl);
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

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

app.MapPost("/api/players/{id:int}/use-food", async (IHttpClientFactory httpClientFactory, int id) =>
{
    var serverApi = httpClientFactory.CreateClient("ServerApi");
    var response = await serverApi.PostAsync($"/api/players/{id}/use-food", content: null);

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

app.MapPost("/api/players/{id:int}/buy-food", async (IHttpClientFactory httpClientFactory, int id) =>
{
    var serverApi = httpClientFactory.CreateClient("ServerApi");
    var response = await serverApi.PostAsync($"/api/players/{id}/buy-food", content: null);

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

app.MapPost("/api/players/{id:int}/preferred-enemy", async (IHttpClientFactory httpClientFactory, int id, SetPreferredEnemyRequest request) =>
{
    var serverApi = httpClientFactory.CreateClient("ServerApi");
    var response = await serverApi.PostAsJsonAsync($"/api/players/{id}/preferred-enemy", request);

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

app.MapPost("/api/players/{id:int}/power-strike", async (IHttpClientFactory httpClientFactory, int id, SetPowerStrikeRequest request) =>
{
    var serverApi = httpClientFactory.CreateClient("ServerApi");
    var response = await serverApi.PostAsJsonAsync($"/api/players/{id}/power-strike", request);

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
