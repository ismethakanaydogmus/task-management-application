using System.Net;
using TaskManager.Application.Tasks;
using TaskManager.Domain.Entities;
using TaskManager.IntegrationTests.Infrastructure;

namespace TaskManager.IntegrationTests;

// Filtreleme / arama / sıralama / sayfalama — EF Core'un ürettiği SQL GERÇEK PostgreSQL'de doğru çalışıyor mu?
// (Unit testteki fake repository bunları test EDEMEZ: ILIKE, CASE, NULL sıralaması veritabanına özgü.)
[Collection(ApiCollection.Name)]
public class TaskQueryTests(ApiFactory factory)
{
    [Fact]
    public async Task SortByPriority_UsesMeaningfulOrder_NotAlphabetical()
    {
        var (client, _) = await factory.CreateUserClientAsync("sort");
        await client.CreateTaskAsync(new CreateTaskRequest("düşük", null, TaskPriority.Low));
        await client.CreateTaskAsync(new CreateTaskRequest("yüksek", null, TaskPriority.High));
        await client.CreateTaskAsync(new CreateTaskRequest("orta", null, TaskPriority.Medium));

        var result = await (await client.GetAsync("/api/tasks?sortBy=priority&sortDirection=desc")).ReadAsync<PagedTasks>();

        // Metin olarak sıralansaydı: High, Low, Medium (Aşama 8'deki tuzak)
        Assert.Equal([TaskPriority.High, TaskPriority.Medium, TaskPriority.Low], result!.Items.Select(t => t.Priority));
    }

    [Fact]
    public async Task SortByDueDate_PutsTasksWithoutDueDateLast_InBothDirections()
    {
        var (client, _) = await factory.CreateUserClientAsync("due");
        await client.CreateTaskAsync(new CreateTaskRequest("tarihsiz", null));
        await client.CreateTaskAsync(new CreateTaskRequest("erken", null, DueDate: new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc)));
        await client.CreateTaskAsync(new CreateTaskRequest("geç", null, DueDate: new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc)));

        var asc = await (await client.GetAsync("/api/tasks?sortBy=dueDate&sortDirection=asc")).ReadAsync<PagedTasks>();
        var desc = await (await client.GetAsync("/api/tasks?sortBy=dueDate&sortDirection=desc")).ReadAsync<PagedTasks>();

        Assert.Equal(["erken", "geç", "tarihsiz"], asc!.Items.Select(t => t.Title));
        Assert.Equal(["geç", "erken", "tarihsiz"], desc!.Items.Select(t => t.Title));
    }

    [Fact]
    public async Task Pagination_IsDeterministic_EvenWhenSortValuesAreEqual()
    {
        // 25 görevin hepsi AYNI öncelikte → sıralama değeri eşit. ThenBy(Id) olmasaydı sayfalar çakışabilirdi.
        var (client, _) = await factory.CreateUserClientAsync("paging");
        for (var i = 1; i <= 25; i++)
            await client.CreateTaskAsync(new CreateTaskRequest($"Görev {i}", null, TaskPriority.Medium));

        var ids = new List<int>();
        for (var page = 1; page <= 3; page++)
        {
            var result = await (await client.GetAsync($"/api/tasks?sortBy=priority&page={page}&pageSize=10")).ReadAsync<PagedTasks>();
            Assert.Equal(25, result!.TotalCount);
            Assert.Equal(3, result.TotalPages);
            ids.AddRange(result.Items.Select(t => t.Id));
        }

        Assert.Equal(25, ids.Count);
        Assert.Equal(25, ids.Distinct().Count()); // hiçbir görev iki sayfada görünmedi, hiçbiri kaybolmadı
    }

    [Fact]
    public async Task Search_IsCaseInsensitive_AndTreatsLikeWildcardsAsText()
    {
        var (client, _) = await factory.CreateUserClientAsync("search");
        await client.CreateTaskAsync(new CreateTaskRequest("Market alışverişi", null));
        await client.CreateTaskAsync(new CreateTaskRequest("İndirim %50", null));
        await client.CreateTaskAsync(new CreateTaskRequest("Rapor yaz", "açıklamada market geçiyor"));

        var market = await (await client.GetAsync("/api/tasks?search=MARKET")).ReadAsync<PagedTasks>();
        var percent = await (await client.GetAsync("/api/tasks?search=%25")).ReadAsync<PagedTasks>();   // "%"
        var underscore = await (await client.GetAsync("/api/tasks?search=_")).ReadAsync<PagedTasks>();

        Assert.Equal(2, market!.TotalCount);                  // başlıkta veya açıklamada, büyük/küçük harf duyarsız
        Assert.Equal("İndirim %50", Assert.Single(percent!.Items).Title); // escape olmasaydı 3 görevin hepsi dönerdi
        Assert.Equal(0, underscore!.TotalCount);
    }

    [Fact]
    public async Task StatusAndPriorityFilters_AreCombined()
    {
        var (client, _) = await factory.CreateUserClientAsync("filter");
        var done = await client.CreateTaskAsync(new CreateTaskRequest("tamam-yüksek", null, TaskPriority.High));
        await client.PutAsync($"/api/tasks/{done.Id}", System.Net.Http.Json.JsonContent.Create(
            new UpdateTaskRequest(done.Title, null, true, TaskPriority.High, null), options: TestClient.Json));
        await client.CreateTaskAsync(new CreateTaskRequest("aktif-yüksek", null, TaskPriority.High));
        await client.CreateTaskAsync(new CreateTaskRequest("aktif-düşük", null, TaskPriority.Low));

        var result = await (await client.GetAsync("/api/tasks?status=active&priority=high")).ReadAsync<PagedTasks>();
        var summary = await (await client.GetAsync("/api/tasks/summary")).ReadAsync<TaskSummary>();

        Assert.Equal("aktif-yüksek", Assert.Single(result!.Items).Title);
        Assert.Equal(new TaskSummary(3, 2, 1), summary);
    }

    [Theory]
    [InlineData("sortBy=passwordHash", "sortBy")]  // whitelist dışı alan
    [InlineData("status=silinmis", "status")]
    [InlineData("pageSize=1000", "pageSize")]       // DoS koruması
    [InlineData("page=0", "page")]
    public async Task InvalidQueryParameters_Return400_WithFieldError(string query, string field)
    {
        var (client, _) = await factory.CreateUserClientAsync("invalid");

        var response = await client.GetAsync($"/api/tasks?{query}");
        var problem = await response.ReadAsync<ProblemResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem!.Errors!.ContainsKey(field), $"Beklenen hata anahtarı: {field}, gelen: {string.Join(",", problem.Errors.Keys)}");
    }
}
