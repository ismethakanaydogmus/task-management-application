using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using TaskManager.Application.Auth;
using TaskManager.Application.Tasks;

namespace TaskManager.IntegrationTests.Infrastructure;

public static class TestClient
{
    // API'nin JSON ayarlarıyla aynı: camelCase + enum'lar metin.
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    // HTTPS: refresh token cookie'si "Secure" bayraklı; HTTP üzerinden cookie container onu geri GÖNDERMEZ.
    // (TestServer gerçek TLS kullanmaz; sadece şemayı https olarak görür.)
    public static HttpClient CreateHttpsClient(this ApiFactory factory, bool handleCookies = true) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = handleCookies,
        });

    // Benzersiz bir kullanıcı kaydeder ve client'ın tüm isteklerine Bearer token ekler.
    public static async Task<(HttpClient Client, AuthResponse Auth)> CreateUserClientAsync(this ApiFactory factory, string name)
    {
        var client = factory.CreateHttpsClient();
        var email = $"{name}-{Guid.NewGuid():N}@test.com";

        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "test-sifresi-123"), Json);
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>(Json))!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth);
    }

    public static async Task<TaskResponse> CreateTaskAsync(this HttpClient client, CreateTaskRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/tasks", request, Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TaskResponse>(Json))!;
    }

    public static Task<T?> ReadAsync<T>(this HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<T>(Json);
}

// Sadece testlerde okumak için (Application'daki PagedResult'ın hesaplanan alanları JSON'dan geri okunamaz).
public sealed record PagedTasks(List<TaskResponse> Items, int Page, int PageSize, int TotalCount, int TotalPages);

public sealed record ProblemResponse(string? Title, int Status, string? Detail, Dictionary<string, string[]>? Errors, string? TraceId);
