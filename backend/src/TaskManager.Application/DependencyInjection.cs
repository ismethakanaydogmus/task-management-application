using Microsoft.Extensions.DependencyInjection;
using TaskManager.Application.Tasks;

namespace TaskManager.Application;

// Her katman kendi servislerini kendisi kaydeder; Program.cs sadece builder.Services.AddApplication() der.
// Faydası: TaskService "internal" kalabiliyor (dışarıdan new'lenemez, sadece interface üzerinden kullanılır)
// ve Program.cs her yeni servis için şişmiyor.
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ITaskService, TaskService>();

        // Sistem saati. Singleton: durumsuz ve thread-safe. Testte FakeTimeProvider ile değiştirilir.
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
