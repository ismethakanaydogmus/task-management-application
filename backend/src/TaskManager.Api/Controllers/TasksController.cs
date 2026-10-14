using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Tasks;

namespace TaskManager.Api.Controllers;

// Controller'ın TEK görevi HTTP'yi konuşmak: isteği al → service'i çağır → başarılı sonucu HTTP'ye çevir.
//
// Aşama 3'e göre fark: if/else yok.
//  - Validasyon hatası → service ValidationException fırlatır → GlobalExceptionHandler → 400
//  - Kayıt yok         → service NotFoundException fırlatır  → GlobalExceptionHandler → 404
// Controller sadece "mutlu yolu" (happy path) bilir.
//
// [ProducesResponseType]: OpenAPI dokümanında hangi durum kodlarının dönebileceğini belgeler.
// Frontend geliştiricisi (veya otomatik client üreten araçlar) neyle karşılaşacağını bilir.
[ApiController]
[Route("api/tasks")]
[Produces("application/json")]
public class TasksController(ITaskService taskService) : ControllerBase
{
    // CancellationToken: ASP.NET Core bunu otomatik olarak HttpContext.RequestAborted'a bağlar.

    // GET /api/tasks
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TaskResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> GetAll(CancellationToken ct)
        => Ok(await taskService.GetAllAsync(ct));

    // GET /api/tasks/5
    [HttpGet("{id:int}", Name = nameof(GetById))]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> GetById(int id, CancellationToken ct)
        => Ok(await taskService.GetByIdAsync(id, ct));

    // POST /api/tasks
    [HttpPost]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskResponse>> Create(CreateTaskRequest request, CancellationToken ct)
    {
        var created = await taskService.CreateAsync(request, ct);
        return CreatedAtRoute(nameof(GetById), new { id = created.Id }, created);
    }

    // PUT /api/tasks/5
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateTaskRequest request, CancellationToken ct)
    {
        await taskService.UpdateAsync(id, request, ct);
        return NoContent();
    }

    // DELETE /api/tasks/5
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await taskService.DeleteAsync(id, ct);
        return NoContent();
    }
}
