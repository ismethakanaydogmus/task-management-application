using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Contracts;
using TaskManager.Api.Models;
using TaskManager.Api.Repositories;

namespace TaskManager.Api.Controllers;

// [ApiController] bize otomatik olarak şunları kazandırır:
//  - Karmaşık tipler (CreateTaskRequest) otomatik olarak body'den ([FromBody]) okunur.
//  - Model binding başarısız olursa (ör. bozuk JSON, "priority": "çokYüksek") action hiç çalışmaz,
//    otomatik 400 Bad Request + hata detayı (ProblemDetails) döner.
//  - Attribute routing zorunlu olur (aşağıdaki [Route]).
[ApiController]
[Route("api/tasks")]  // Neden "api/[controller]" değil? Sınıf adını değiştirince URL'nin (ve frontend'in) bozulmaması için.
public class TasksController(ITaskRepository repository) : ControllerBase
// ↑ "Primary constructor" (C# 12). DI container, Program.cs'teki kayda bakıp
//   ITaskRepository yerine InMemoryTaskRepository nesnesini buraya enjekte eder.
//
// Neden Controller değil de ControllerBase?
// Controller sınıfı View (HTML sayfa) desteği de içerir. Biz sadece JSON döndüren bir API'yiz.
{
    // READ (liste) → GET /api/tasks
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskItem>>> GetAll()
    {
        var tasks = await repository.GetAllAsync();
        return Ok(tasks); // 200 OK
    }

    // READ (tek kayıt) → GET /api/tasks/5
    // "{id:int}" bir route constraint: /api/tasks/abc gibi bir istek bu action'a hiç eşleşmez (404).
    [HttpGet("{id:int}", Name = nameof(GetById))]
    public async Task<ActionResult<TaskItem>> GetById(int id) // id → route'tan model binding ile gelir
    {
        var task = await repository.GetByIdAsync(id);
        if (task is null)
            return NotFound(); // 404 — kaynak yok

        return Ok(task);
    }

    // CREATE → POST /api/tasks
    [HttpPost]
    public async Task<ActionResult<TaskItem>> Create(CreateTaskRequest request) // body'deki JSON → request
    {
        // Geçici, basit kontrol. Aşama 4'te FluentValidation ile düzgün validasyon kuracağız.
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Title boş olamaz."); // 400

        var task = new TaskItem
        {
            Title = request.Title.Trim(),
            Description = request.Description,
            Priority = request.Priority,
            DueDate = request.DueDate
        };

        var created = await repository.AddAsync(task);

        // 201 Created + "Location: /api/tasks/{id}" header'ı + oluşan kaydın kendisi.
        // REST konvansiyonu: yeni kaynağın nerede olduğunu istemciye söyle.
        return CreatedAtRoute(nameof(GetById), new { id = created.Id }, created);
    }

    // UPDATE → PUT /api/tasks/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateTaskRequest request) // id route'tan, request body'den
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Title boş olamaz.");

        var task = await repository.GetByIdAsync(id);
        if (task is null)
            return NotFound();

        task.Title = request.Title.Trim();
        task.Description = request.Description;
        task.IsCompleted = request.IsCompleted;
        task.Priority = request.Priority;
        task.DueDate = request.DueDate;
        task.UpdatedAt = DateTime.UtcNow;

        await repository.UpdateAsync(task);

        // 204 No Content: "işlem başarılı, geri gönderecek bir şeyim yok".
        // Alternatif: 200 OK + güncel kaydı döndürmek. İkisi de yaygın; istemci güncel hali
        // zaten bildiği için (kendisi gönderdi) 204 yeterli.
        return NoContent();
    }

    // DELETE → DELETE /api/tasks/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await repository.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
