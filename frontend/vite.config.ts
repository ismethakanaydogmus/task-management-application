/// <reference types="vitest/config" />
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  test: {
    // jsdom: Node içinde tarayıcı DOM'unu taklit eder (document, window...). Component testleri için gerekli.
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    // SAAT DİLİMİ SABİT: Tarih testleri makinenin yerel saat dilimine bağlı olsaydı İstanbul'da geçer,
    // UTC'de çalışan bir CI sunucusunda KIRILIRDI ("bende çalışıyordu"nun klasik sebeplerinden biri).
    // VITE_API_URL: testte .env.development yüklenmez (mod "test"); sabit, sahte bir adres veriyoruz.
    env: { TZ: 'Europe/Istanbul', VITE_API_URL: 'http://api.test' },
  },
})
