using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Common;
using TaskManager.Application.Tasks;

namespace TaskManager.Api.Controllers;

// Controller'ın TEK görevi HTTP'yi konuşmak: isteği al → service'i çağır → başarılı sonucu HTTP'ye çevir.
//
// Kimlik: Bu controller'da [Authorize] YOK ama korumalı → Program.cs'teki fallback policy (Aşama 7).
// Sahiplik: Hiçbir action userId almıyor/geçirmiyor → TaskService, ICurrentUser'dan alıyor (Aşama 8).
//   Başkasının görevi → service NotFoundException → 404.
[ApiController]
[Route("api/tasks")]
[Produces("application/json")]
public class TasksController(ITaskService taskService) : ControllerBase
{
    // GET /api/tasks?status=active&priority=High&search=market&sortBy=dueDate&sortDirection=asc&page=1&pageSize=20
    // [FromQuery]: TaskQuery'nin alanları query string'den doldurulur. Enum'lar büyük/küçük harf duyarsız ("active" = "Active").
    [HttpGet]
    [ProducesResponseType<PagedResult<TaskResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<TaskResponse>>> GetPaged([FromQuery] TaskQuery query, CancellationToken ct)
        => Ok(await taskService.GetPagedAsync(query, ct));

    // GET /api/tasks/summary → { all, active, completed } (filtre rozetleri için; sayfalamadan bağımsız)
    // Dikkat: "summary" metni "{id:int}" route'uyla çakışmaz çünkü int constraint'i var.
    [HttpGet("summary")]
    [ProducesResponseType<TaskSummary>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TaskSummary>> GetSummary(CancellationToken ct)
        => Ok(await taskService.GetSummaryAsync(ct));

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
