namespace TaskManager.Api.Models;

// Neden "Task" değil de "TaskItem"?
// .NET'te System.Threading.Tasks.Task zaten var (async metotların dönüş tipi).
// "Task" adını kullansaydık her dosyada isim çakışması yaşar, "Task" mı "Models.Task" mı diye uğraşırdık.
public class TaskItem
{
    public int Id { get; set; }

    // "required": nesne oluşturulurken Title verilmek ZORUNDA (C# 11).
    // Derleme zamanında kontrol edilir: new TaskItem() yazıp Title'ı unutursan kod derlenmez.
    public required string Title { get; set; }

    // "string?" = null olabilir (açıklama opsiyonel).
    // Projede <Nullable>enable</Nullable> açık olduğu için null olabilecek yerleri böyle açıkça işaretliyoruz.
    public string? Description { get; set; }

    public bool IsCompleted { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public DateTime? DueDate { get; set; }

    // Neden DateTime.Now değil de UtcNow?
    // Sunucu hangi saat diliminde çalışırsa çalışsın aynı anı temsil etsin diye.
    // Kullanıcının yerel saatine çevirmek frontend'in işi.
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}

// Öncelik için string ("high") yerine enum kullanıyoruz:
// geçersiz değerler ("hihg") derleme/model binding aşamasında yakalanır.
public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2
}
