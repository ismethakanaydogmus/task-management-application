namespace TaskManager.Application.Abstractions;

// "Şu anki isteği yapan kullanıcı kim?"
// Application, HTTP'yi ve JWT'yi BİLMEZ; sadece bu sözleşmeyi bilir.
// Gerçekleştirimi Api katmanında (HttpCurrentUser): token'daki "sub" claim'ini okur.
//
// Neden kullanıcı id'sini service metotlarına PARAMETRE olarak geçirmiyoruz? (GetAllAsync(userId, ...))
// Her controller action'ında id'yi okuyup geçirmek gerekirdi; BİR yerde unutulsa veya yanlış değer
// (ör. route'tan gelen id) geçirilse IDOR açığı oluşur. Service kimliği kendisi, tek ve güvenilir kaynaktan alır.
public interface ICurrentUser
{
    // Giriş yapılmamışsa exception fırlatır (fallback policy sayesinde normalde buraya hiç gelinmez).
    int UserId { get; }
}
