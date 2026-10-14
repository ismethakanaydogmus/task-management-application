using System.Net;
using TaskManager.IntegrationTests.Infrastructure;

namespace TaskManager.IntegrationTests;

[Collection(ApiCollection.Name)]
public class HealthTests(ApiFactory factory)
{
    // Orkestratörün (Docker, Kubernetes) token'ı yok: health endpoint'leri fallback policy'ye takılmamalı.
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")] // veritabanı (Testcontainers) ayakta → Healthy
    public async Task HealthEndpoints_AreAnonymous_AndHealthy(string url)
    {
        var response = await factory.CreateHttpsClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
