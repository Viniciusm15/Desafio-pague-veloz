using Npgsql;
using Respawn;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PagueVeloz.IntegrationTests.Common;

[Collection("Database")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly HttpClient Client;
    private readonly IntegrationTestWebAppFactory _factory;
    private Respawner _respawner = null!;
    protected readonly JsonSerializerOptions JsonOptions;

    protected IntegrationTestBase(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
        Client = factory.CreateClient();

        JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
        };
    }

    protected async Task<HttpResponseMessage> PostAsJsonAsync<T>(string requestUri, T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await Client.PostAsync(requestUri, content);
    }

    protected async Task<T?> ReadFromJsonAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public async Task InitializeAsync()
    {
        await using var conn = new NpgsqlConnection(_factory.ConnectionString);
        await conn.OpenAsync();

        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres
        });

        await _respawner.ResetAsync(conn);
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
