namespace TaskManager.Domain.Entities;

// String ("high") yerine enum: geçersiz değerler ("hihg") model binding aşamasında yakalanır.
public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2
}
