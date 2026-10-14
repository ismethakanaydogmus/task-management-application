import { describe, expect, it } from 'vitest';
import { formatDateTime, isOverdue, localInputToUtcIso } from './date';

// Saf fonksiyonlar (aynı girdi → aynı çıktı, yan etki yok) test için en kolay ve en hızlı hedeftir.
// Saat dilimi vite.config.ts'te Europe/Istanbul (UTC+3) olarak sabitlendi.
describe('localInputToUtcIso', () => {
  it('datetime-local değerini (saat dilimsiz, yerel) UTC ISO metnine çevirir', () => {
    // İstanbul 18:00 = UTC 15:00 → backend validator'ı "Z" ile biten bu değeri kabul eder (Aşama 4)
    expect(localInputToUtcIso('2026-10-20T18:00')).toBe('2026-10-20T15:00:00.000Z');
  });

  it('boş değer için null döner', () => {
    expect(localInputToUtcIso('')).toBeNull();
  });
});

describe('formatDateTime', () => {
  it('backendden gelen UTC zamanı yerel saatte gösterir', () => {
    expect(formatDateTime('2026-10-20T15:00:00Z')).toContain('18:00');
  });
});

describe('isOverdue', () => {
  const now = new Date('2026-10-14T12:00:00Z');

  it.each([
    ['2026-10-14T11:59:00Z', true],  // geçmiş
    ['2026-10-14T12:01:00Z', false], // gelecek
    [null, false],                   // son tarih yok
  ])('%s → %s', (dueDate, expected) => {
    expect(isOverdue(dueDate, now)).toBe(expected);
  });
});
