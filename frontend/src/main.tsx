// Uygulamanın giriş noktası (backend'deki Program.cs'in karşılığı).
// index.html'deki <div id="root"> elemanını bulur ve React'ı oraya "monte eder".
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { ApiError } from './api/client';
import App from './App.tsx';
import './index.css';

// QueryClient: TÜM sunucu verisinin önbelleği. Uygulamada TEK bir tane olur (backend'deki Singleton gibi).
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // 30 sn boyunca veri "taze" sayılır; bu sürede aynı veri için tekrar istek atılmaz.
      // Varsayılan 0'dır: her mount'ta, her sekmeye dönüşte arka planda yeniden çeker.
      staleTime: 30_000,
      // Varsayılan: hata olunca 3 kez tekrar dener. Ama 4xx (400, 404) istemci hatasıdır;
      // aynı isteği tekrar göndermek sonucu değiştirmez. Sadece ağ hatası ve 5xx'te tekrar dene.
      retry: (failureCount, error) => {
        if (error instanceof ApiError && error.status >= 400 && error.status < 500) return false;
        return failureCount < 2;
      },
    },
  },
});

createRoot(document.getElementById('root')!).render(
  // StrictMode: SADECE geliştirme ortamında component'leri bilerek İKİ KEZ çizer; saf olmayan kodu yakalar.
  <StrictMode>
    {/* Provider: alt ağaçtaki her component useQuery/useMutation ile bu önbelleğe erişebilir (React Context). */}
    <QueryClientProvider client={queryClient}>
      <App />
      {/* Sağ alttaki çiçek ikonu: önbellekteki sorguları, durumlarını, verilerini canlı gösterir.
          Production build'de otomatik olarak çıkarılır. */}
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  </StrictMode>,
);
