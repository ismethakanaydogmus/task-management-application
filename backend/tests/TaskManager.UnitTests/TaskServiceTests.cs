using FluentValidation;
using Microsoft.Extensions.Time.Testing;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Tasks;
using TaskManager.Domain.Entities;
using TaskManager.UnitTests.Fakes;

namespace TaskManager.UnitTests;

// UNIT TEST: TaskService'in İŞ KURALLARINI veritabanı/HTTP olmadan, milisaniyeler içinde test eder.
//
// xUnit her test metodu için sınıfın YENİ bir örneğini oluşturur → constructor her testten önce çalışır.
// Böylece testler birbirinin verisini görmez (test izolasyonu); çalışma sırası önemsizdir.
public class TaskServiceTests
{
    private const int Ali = 1;
    private const int Ayse = 2;

    private static readonly DateTimeOffset Start = new(2026, 10, 14, 11, 0, 0, TimeSpan.Zero);

    // FakeTimeProvider: "Şu an" BİZİM belirlediğimiz an. clock.Advance(...) ile zamanı ileri sarabiliriz.
    // DateTime.UtcNow kullansaydık "CompletedAt == şimdi mi?" sorusunu güvenilir şekilde soramazdık
    // (test çalışırken saat ilerler). Aşama 3'te TimeProvider'ı enjekte etmemizin karşılığı burada.
    private readonly FakeTimeProvider _clock = new(Start);
    private readonly InMemoryTaskRepository _repository = new();
    private readonly FakeCurrentUser _currentUser = new(Ali);
    private readonly TaskService _sut; // SUT = System Under Test (test edilen sistem)

    public TaskServiceTests()
    {
        _sut = new TaskService(
            _repository,
            _currentUser,
            _clock,
            new CreateTaskRequestValidator(),
            new UpdateTaskRequestValidator(),
            new TaskQueryValidator());
    }

    // ---------------------------------------------------------------- Create

    [Fact]
    public async Task Create_AssignsCurrentUserAsOwner_AndUsesClockForCreatedAt()
    {
        // Act
        var created = await _sut.CreateAsync(new CreateTaskRequest("Market", null));

        // Assert
        var stored = Assert.Single(_repository.Tasks);
        Assert.Equal(Ali, stored.UserId);
        Assert.Equal(Start.UtcDateTime, created.CreatedAt);
    }

    [Fact]
    public async Task Create_TrimsTitleAndDescription()
    {
        var created = await _sut.CreateAsync(new CreateTaskRequest("  Market  ", "  süt  "));

        Assert.Equal("Market", created.Title);
        Assert.Equal("süt", created.Description);
    }

    // [Theory] + [InlineData]: Aynı testi farklı girdilerle çalıştırır (her biri ayrı bir test olarak raporlanır).
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_RejectsEmptyTitle(string title)
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(new CreateTaskRequest(title, null)));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CreateTaskRequest.Title));
        Assert.Empty(_repository.Tasks); // geçersiz veri kaydedilmedi
    }

    [Fact]
    public async Task Create_RejectsDueDateWithoutTimeZone()
    {
        // Aşama 2'nin "bilinen sorunu": saat dilimsiz tarih Npgsql'de 500'e yol açıyordu.
        var unspecified = new DateTime(2026, 10, 20, 18, 0, 0, DateTimeKind.Unspecified);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _sut.CreateAsync(new CreateTaskRequest("Market", null, DueDate: unspecified)));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CreateTaskRequest.DueDate));
    }

    [Fact]
    public async Task Create_ConvertsLocalDueDateToUtc()
    {
        var local = new DateTime(2026, 10, 20, 18, 0, 0, DateTimeKind.Local);

        var created = await _sut.CreateAsync(new CreateTaskRequest("Market", null, DueDate: local));

        Assert.Equal(DateTimeKind.Utc, created.DueDate!.Value.Kind);
        Assert.Equal(local.ToUniversalTime(), created.DueDate);
    }

    // ---------------------------------------------------------------- CompletedAt iş kuralı (Aşama 3)

    [Fact]
    public async Task Update_SetsCompletedAt_WhenTaskBecomesCompleted()
    {
        var task = await _sut.CreateAsync(new CreateTaskRequest("Market", null));
        _clock.Advance(TimeSpan.FromHours(2));

        await _sut.UpdateAsync(task.Id, CompleteRequest(task.Title, isCompleted: true));

        var updated = await _sut.GetByIdAsync(task.Id);
        Assert.True(updated.IsCompleted);
        Assert.Equal(Start.AddHours(2).UtcDateTime, updated.CompletedAt);
    }

    [Fact]
    public async Task Update_KeepsOriginalCompletedAt_WhenAlreadyCompletedTaskIsUpdatedAgain()
    {
        var task = await _sut.CreateAsync(new CreateTaskRequest("Market", null));
        _clock.Advance(TimeSpan.FromHours(1));
        await _sut.UpdateAsync(task.Id, CompleteRequest(task.Title, isCompleted: true));

        _clock.Advance(TimeSpan.FromDays(3));
        await _sut.UpdateAsync(task.Id, CompleteRequest("Başlık değişti", isCompleted: true));

        var updated = await _sut.GetByIdAsync(task.Id);
        Assert.Equal(Start.AddHours(1).UtcDateTime, updated.CompletedAt); // ilk tamamlanma anı korunur
        Assert.Equal(Start.AddHours(1).AddDays(3).UtcDateTime, updated.UpdatedAt); // ama UpdatedAt ilerler
    }

    [Fact]
    public async Task Update_ClearsCompletedAt_WhenTaskIsReopened()
    {
        var task = await _sut.CreateAsync(new CreateTaskRequest("Market", null));
        await _sut.UpdateAsync(task.Id, CompleteRequest(task.Title, isCompleted: true));

        await _sut.UpdateAsync(task.Id, CompleteRequest(task.Title, isCompleted: false));

        var updated = await _sut.GetByIdAsync(task.Id);
        Assert.False(updated.IsCompleted);
        Assert.Null(updated.CompletedAt);
    }

    // ---------------------------------------------------------------- Sahiplik (Aşama 8)

    [Fact]
    public async Task GetById_ThrowsNotFound_ForAnotherUsersTask()
    {
        var aysesTask = await CreateAsAsync(Ayse, "Ayşe'nin görevi");

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(aysesTask.Id));
    }

    [Fact]
    public async Task Update_ThrowsNotFound_AndDoesNotModify_AnotherUsersTask()
    {
        var aysesTask = await CreateAsAsync(Ayse, "Ayşe'nin görevi");

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateAsync(aysesTask.Id, CompleteRequest("Ali değiştirdi", isCompleted: true)));

        var stored = _repository.Tasks.Single(t => t.Id == aysesTask.Id);
        Assert.Equal("Ayşe'nin görevi", stored.Title);
        Assert.False(stored.IsCompleted);
    }

    [Fact]
    public async Task Delete_ThrowsNotFound_AndKeeps_AnotherUsersTask()
    {
        var aysesTask = await CreateAsAsync(Ayse, "Ayşe'nin görevi");

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(aysesTask.Id));

        Assert.Contains(_repository.Tasks, t => t.Id == aysesTask.Id);
    }

    [Fact]
    public async Task GetPaged_ReturnsOnlyCurrentUsersTasks()
    {
        await CreateAsAsync(Ayse, "Ayşe 1");
        await CreateAsAsync(Ali, "Ali 1");
        await CreateAsAsync(Ali, "Ali 2");

        var result = await _sut.GetPagedAsync(new TaskQuery());

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, t => Assert.StartsWith("Ali", t.Title));
    }

    // ---------------------------------------------------------------- Sorgu validasyonu

    [Theory]
    [InlineData(0, 20)]   // sayfa 0
    [InlineData(1, 0)]    // boyut 0
    [InlineData(1, 101)]  // boyut üst sınır üstünde (DoS koruması)
    public async Task GetPaged_RejectsInvalidPaging(int page, int pageSize)
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.GetPagedAsync(new TaskQuery(Page: page, PageSize: pageSize)));
    }

    [Fact]
    public void PagedResult_CalculatesPages()
    {
        var result = new Application.Common.PagedResult<int>([], Page: 3, PageSize: 10, TotalCount: 21);

        Assert.Equal(3, result.TotalPages);
        Assert.False(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
    }

    // ---------------------------------------------------------------- Yardımcılar

    private static UpdateTaskRequest CompleteRequest(string title, bool isCompleted)
        => new(title, null, isCompleted, TaskPriority.Medium, null);

    private async Task<TaskResponse> CreateAsAsync(int userId, string title)
    {
        var previous = _currentUser.UserId;
        _currentUser.UserId = userId;
        try
        {
            return await _sut.CreateAsync(new CreateTaskRequest(title, null));
        }
        finally
        {
            _currentUser.UserId = previous;
        }
    }
}
