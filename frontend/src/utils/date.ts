// Tarih/saat dilimi kuralı (backend Aşama 4 ile aynı):
//   Sunucu ve veritabanı → her şey UTC
//   Ekran               → kullanıcının yerel saati
// Çeviri SADECE burada, sınırda yapılır.

// <input type="datetime-local"> değeri: "2026-10-20T18:00" → saat dilimi YOK!
// Bunu olduğu gibi backend'e gönderseydik Aşama 4'teki validator 400 dönerdi
// ("Tarih saat dilimi içermeli").
// new Date("2026-10-20T18:00") tarayıcının yerel saat dilimiyle yorumlar (İstanbul → +03:00),
// toISOString() ise UTC'ye çevirip sonuna "Z" koyar: "2026-10-20T15:00:00.000Z"
export function localInputToUtcIso(value: string): string | null {
  if (!value) return null;
  return new Date(value).toISOString();
}

// Backend'den gelen UTC metni ("2026-10-20T15:00:00Z") → kullanıcının yerel saatinde okunur metin.
// Intl.DateTimeFormat: tarayıcının yerleşik biçimlendiricisi; kütüphane gerektirmez.
const formatter = new Intl.DateTimeFormat('tr-TR', {
  dateStyle: 'medium',
  timeStyle: 'short',
});

export function formatDateTime(iso: string): string {
  return formatter.format(new Date(iso));
}

// Son tarihi geçmiş mi? (tamamlanmamış görevlerde kırmızı göstermek için)
export function isOverdue(iso: string | null, now: Date = new Date()): boolean {
  return iso !== null && new Date(iso) < now;
}
