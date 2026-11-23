using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace PathfinderElements.Api.IntegrationTests;

public class LivenessTests
{
    [Fact]
    public async Task GetLivenessReturnsOkWithEmptyBody()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
    }
}
