using System.Net;
using System.Net.Http.Json;
using TaskManager.Application.Tasks;
using TaskManager.Domain.Entities;
using TaskManager.IntegrationTests.Infrastructure;

namespace TaskManager.IntegrationTests;

// IDOR / BOLA (Aşama 8): Ali, Ayşe'nin görevine HİÇBİR yoldan erişememeli.
// Bu testler gerçek HTTP + gerçek JWT + gerçek PostgreSQL sorgusuyla (WHERE user_id = @me) çalışır.
[Collection(ApiCollection.Name)]
public class TaskOwnershipTests(ApiFactory factory)
{
    [Fact]
    public async Task AnotherUsersTask_IsInvisibleAndUntouchable()
    {
        var (ali, _) = await factory.CreateUserClientAsync("ali");
        var (ayse, _) = await factory.CreateUserClientAsync("ayse");
        var aysesTask = await ayse.CreateTaskAsync(new CreateTaskRequest("Ayşe: gizli doktor randevusu", null, TaskPriority.High));

        // Ali'nin tüm denemeleri 404 — hiç var olmayan bir görevle AYNI cevap (varlığı sızmaz).
        Assert.Equal(HttpStatusCode.NotFound, (await ali.GetAsync($"/api/tasks/{aysesTask.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ali.PutAsJsonAsync($"/api/tasks/{aysesTask.Id}",
            new UpdateTaskRequest("Ali değiştirdi", null, true, TaskPriority.Low, null), TestClient.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ali.DeleteAsync($"/api/tasks/{aysesTask.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ali.GetAsync("/api/tasks/999999999")).StatusCode);

        // Ali'nin listesinde ve özetinde yok
        var alisList = await (await ali.GetAsync("/api/tasks?pageSize=100")).ReadAsync<PagedTasks>();
        Assert.DoesNotContain(alisList!.Items, t => t.Id == aysesTask.Id);
        Assert.Equal(new TaskSummary(0, 0, 0), await (await ali.GetAsync("/api/tasks/summary")).ReadAsync<TaskSummary>());

        // Ayşe'nin görevi değişmeden duruyor
        var stillThere = await (await ayse.GetAsync($"/api/tasks/{aysesTask.Id}")).ReadAsync<TaskResponse>();
        Assert.Equal("Ayşe: gizli doktor randevusu", stillThere!.Title);
        Assert.False(stillThere.IsCompleted);
    }

    [Fact]
    public async Task OverPosting_UserIdInBody_IsIgnored()
    {
        var (ali, _) = await factory.CreateUserClientAsync("ali");
        var (ayse, ayseAuth) = await factory.CreateUserClientAsync("ayse");

        // Ali, gövdeye Ayşe'nin id'sini koyarak onun adına görev oluşturmaya çalışıyor.
        var response = await ali.PostAsJsonAsync("/api/tasks", new { title = "Ayşe adına?", userId = ayseAuth.User.Id });
        response.EnsureSuccessStatusCode();

        Assert.Equal(0, (await (await ayse.GetAsync("/api/tasks/summary")).ReadAsync<TaskSummary>())!.All);
        Assert.Equal(1, (await (await ali.GetAsync("/api/tasks/summary")).ReadAsync<TaskSummary>())!.All);
    }

    [Theory]
    [InlineData("GET", "/api/tasks")]
    [InlineData("GET", "/api/tasks/summary")]
    [InlineData("POST", "/api/tasks")]
    [InlineData("DELETE", "/api/tasks/1")]
    public async Task TaskEndpoints_RequireAuthentication(string method, string url)
    {
        var anonymous = factory.CreateHttpsClient();

        var response = await anonymous.SendAsync(new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method == "POST" ? JsonContent.Create(new { title = "x" }) : null
        });

        // Fallback policy (Aşama 7): attribute'suz her endpoint giriş ister.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
