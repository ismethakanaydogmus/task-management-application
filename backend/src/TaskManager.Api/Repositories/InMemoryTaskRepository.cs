using System.Collections.Concurrent;
using TaskManager.Api.Models;

namespace TaskManager.Api.Repositories;

// Veriyi RAM'de tutan geçici repository. Uygulama yeniden başlayınca her şey silinir.
// Aşama 2'de PostgreSQL ile değiştirilecek.
//
// Bu sınıf SINGLETON olarak kaydedilir (Program.cs). Yani tüm HTTP istekleri AYNI nesneyi kullanır.
// ASP.NET Core istekleri paralel işler; iki kullanıcı aynı anda görev eklerse
// aynı koleksiyona aynı anda yazılır. Bu yüzden thread-safe yapılar kullanıyoruz:
//
//  - List<TaskItem> kullansaydık: eşzamanlı Add çağrıları listeyi bozabilir, kayıt kaybolabilir
//    veya "Collection was modified" hatası alınabilirdi.
//  - _nextId++ kullansaydık: iki istek aynı anda aynı Id'yi alabilirdi (race condition).
//    Interlocked.Increment bu artırmayı atomik (bölünemez) yapar.
public class InMemoryTaskRepository : ITaskRepository
{
    private readonly ConcurrentDictionary<int, TaskItem> _tasks = new();
    private int _nextId = 0;

    public Task<IReadOnlyList<TaskItem>> GetAllAsync()
    {
        IReadOnlyList<TaskItem> result = _tasks.Values
            .OrderByDescending(t => t.CreatedAt)
            .ToList();
        return Task.FromResult(result); // senkron sonucu Task'a sarmalıyoruz
    }

    public Task<TaskItem?> GetByIdAsync(int id)
    {
        _tasks.TryGetValue(id, out var task);
        return Task.FromResult(task);
    }

    public Task<TaskItem> AddAsync(TaskItem task)
    {
        task.Id = Interlocked.Increment(ref _nextId);
        _tasks[task.Id] = task;
        return Task.FromResult(task);
    }

    public Task<bool> UpdateAsync(TaskItem task)
    {
        if (!_tasks.ContainsKey(task.Id))
            return Task.FromResult(false);

        _tasks[task.Id] = task;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(int id)
    {
        return Task.FromResult(_tasks.TryRemove(id, out _));
    }
}
