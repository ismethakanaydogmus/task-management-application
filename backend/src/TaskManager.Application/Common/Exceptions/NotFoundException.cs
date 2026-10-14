namespace TaskManager.Application.Common.Exceptions;

// Service "aradığın kayıt yok" demek istediğinde fırlatır.
// Service HTTP'yi bilmez (404 diyemez); bu exception'ı Api katmanındaki GlobalExceptionHandler 404'e çevirir.
//
// Neden exception? (Alternatifi Result pattern — gerekçe docs/04'te)
//  - Controller'lar if/else'siz, tek satır kalır.
//  - Bulunamama durumu her katmandan (iç içe service çağrılarında bile) tek bir yerde HTTP'ye çevrilir.
public sealed class NotFoundException(string resource, object key)
    : Exception($"{resource} bulunamadı (id: {key}).")
{
    public string Resource { get; } = resource;
    public object Key { get; } = key;
}
