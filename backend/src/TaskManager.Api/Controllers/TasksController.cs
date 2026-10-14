using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Tasks;

namespace TaskManager.Api.Controllers;

// Controller'ın TEK görevi HTTP'yi konuşmak:
//   isteği al → service'i çağır → sonucu doğru HTTP durum koduna çevir.
// İş kuralı yok, veritabanı yok, entity yok. Sadece DTO'lar ve ITaskService.
//
// [ApiController]:
//  - Karmaşık tipler (CreateTaskRequest) otomatik olarak body'den okunur.
//  - Model binding başarısız olursa (bozuk JSON, geçersiz enum) action çalışmadan otomatik 400 döner.
[ApiController]
[Route("api/tasks")]
public class TasksController(ITaskService taskService) : ControllerBase
{
    // CancellationToken parametresi: ASP.NET Core bunu otomatik olarak HttpContext.RequestAborted'a bağlar.
    // İstemci bağlantıyı kapatırsa token iptal olur → service → repository → EF → Npgsql zinciriyle
    // veritabanındaki sorgu da iptal edilir. Boşuna çalışan sorgu kalmaz.

    // GET /api/tasks
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> GetAll(CancellationToken ct)
    {
        return Ok(await taskService.GetAllAsync(ct));
    }

    // GET /api/tasks/5
    [HttpGet("{id:int}", Name = nameof(GetById))]
    public async Task<ActionResult<TaskResponse>> GetById(int id, CancellationToken ct)
    {
        var task = await taskService.GetByIdAsync(id, ct);
        return task is null ? NotFound() : Ok(task);
    }

    // POST /api/tasks
    [HttpPost]
    public async Task<ActionResult<TaskResponse>> Create(CreateTaskRequest request, CancellationToken ct)
    {
        // Girdi doğrulaması (input validation) — iş kuralı değil, "istek düzgün mü?" kontrolü.
        // Aşama 4'te FluentValidation'a taşınacak.
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Title boş olamaz.");

        var created = await taskService.CreateAsync(request, ct);
        return CreatedAtRoute(nameof(GetById), new { id = created.Id }, created);
    }

    // PUT /api/tasks/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateTaskRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Title boş olamaz.");

        var updated = await taskService.UpdateAsync(id, request, ct);
        return updated ? NoContent() : NotFound();
    }

    // DELETE /api/tasks/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await taskService.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
