// Uygulamanın giriş noktası (backend'deki Program.cs'in karşılığı).
// index.html'deki <div id="root"> elemanını bulur ve React'ı oraya "monte eder".
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import './index.css';
import App from './App.tsx';

createRoot(document.getElementById('root')!).render(
  // StrictMode: SADECE geliştirme ortamında ek kontroller yapar.
  // Component'leri bilerek İKİ KEZ çizer ve effect'leri iki kez çalıştırır; böylece yan etkili
  // (saf olmayan) kodlar erkenden ortaya çıkar. "console.log neden iki kez yazıyor?" sorusunun cevabı budur.
  // Production build'de hiçbir etkisi yoktur.
  <StrictMode>
    <App />
  </StrictMode>,
);
