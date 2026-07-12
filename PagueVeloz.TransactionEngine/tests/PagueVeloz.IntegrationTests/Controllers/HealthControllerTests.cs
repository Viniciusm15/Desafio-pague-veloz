using FluentAssertions;
using PagueVeloz.IntegrationTests.Common;
using System.Net;
using System.Text.Json;

namespace PagueVeloz.IntegrationTests.Controllers;

[Collection("Database")]
public class HealthControllerTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetHealth_ShouldReturnOkWithStatus()
    {
        // Act
        var response = await Client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.TryGetProperty("status", out var statusProp).Should().BeTrue();
        var status = statusProp.GetString();

        status.Should().BeOneOf("Healthy", "Degraded", "Unhealthy");

        if (root.TryGetProperty("entries", out var entries))
        {
            entries.ValueKind.Should().Be(JsonValueKind.Object);
        }
    }
}
