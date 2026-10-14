namespace TaskManager.Domain.Entities;

// DOMAIN katmanı: uygulamanın "ne olduğu". Hiçbir projeye ve pakete bağımlı değil
// (EF Core, ASP.NET, JSON... hiçbirini bilmez). En iç halka budur.
//
// Neden "Task" değil de "TaskItem"?
// .NET'te System.Threading.Tasks.Task zaten var (async metotların dönüş tipi); isim çakışmasını önlüyoruz.
public class TaskItem
{
    public int Id { get; set; }

    // "required": nesne oluşturulurken Title verilmek ZORUNDA (derleme zamanında kontrol edilir).
    public required string Title { get; set; }

    public string? Description { get; set; }

    public bool IsCompleted { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public DateTime? DueDate { get; set; }

    // Tüm zamanlar UTC. Kullanıcının yerel saatine çevirmek frontend'in işi.
    // Değerleri artık entity değil, TaskService atıyor (TimeProvider ile → test edilebilir).
    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Görev tamamlandığında TaskService tarafından doldurulur (iş kuralı).
    public DateTime? CompletedAt { get; set; }
}
