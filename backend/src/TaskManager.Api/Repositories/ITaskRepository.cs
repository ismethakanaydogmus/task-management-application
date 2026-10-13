using TaskManager.Api.Models;

namespace TaskManager.Api.Repositories;

// Interface = "ne yapılabileceği"nin sözleşmesi; "nasıl yapıldığını" söylemez.
// Controller bu interface'i bilir, arkadaki gerçek sınıfı bilmez.
// Aşama 2'de InMemoryTaskRepository yerine PostgreSQL kullanan bir sınıf yazacağız
// ve controller'da TEK SATIR bile değiştirmeyeceğiz; sadece Program.cs'teki DI kaydı değişecek.
//
// Neden şu an bellekteyken bile Task<...> (async) dönüyoruz?
// Gerçek veritabanı erişimi I/O işlemidir ve async olmalı. Şimdiden async imza kullanırsak
// Aşama 2'de interface'i ve onu çağıran her yeri değiştirmek zorunda kalmayız.
public interface ITaskRepository
{
    Task<IReadOnlyList<TaskItem>> GetAllAsync();
    Task<TaskItem?> GetByIdAsync(int id);          // bulunamazsa null
    Task<TaskItem> AddAsync(TaskItem task);
    Task<bool> UpdateAsync(TaskItem task);         // kayıt yoksa false
    Task<bool> DeleteAsync(int id);                // kayıt yoksa false
}
