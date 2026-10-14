import { useEffect, useState } from 'react';

// DEBOUNCE: Değer değiştikçe beklemeye başla; `delay` ms boyunca YENİ bir değişiklik olmazsa değeri yayınla.
//
// Arama kutusuna "market" yazarken:
//   debounce YOK → m, ma, mar, mark, marke, market = 6 istek (ve cevaplar karışık sırayla gelebilir)
//   debounce VAR → kullanıcı yazmayı bırakınca 1 istek
//
// Benzer kavram: THROTTLE → "en fazla X ms'de bir" (ör. scroll olayları). Debounce ise "sakinleşince bir kez".
export function useDebouncedValue<T>(value: T, delay = 300): T {
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delay);
    // Cleanup: değer `delay` dolmadan tekrar değişirse ÖNCEKİ zamanlayıcıyı iptal et.
    // Bu satır olmasaydı her harf için ayrı ayrı zamanlayıcı çalışır, debounce olmazdı.
    return () => clearTimeout(timer);
  }, [value, delay]);

  return debounced;
}
