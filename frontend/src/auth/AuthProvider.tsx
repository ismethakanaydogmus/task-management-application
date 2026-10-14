import { useQueryClient } from '@tanstack/react-query';
import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import * as authApi from '../api/auth';
import { refreshSession, setAccessToken, setSessionExpiredHandler } from '../api/client';
import type { AuthResponse, LoginRequest, RegisterRequest } from '../types/auth';
import { AuthContext, type AuthContextValue, type AuthState } from './authContext';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>({ status: 'loading' });
  const queryClient = useQueryClient();

  // Oturum bittiğinde (çıkış veya refresh başarısız) önbellekteki TÜM sunucu verisini sil.
  // Silmeseydik: aynı tarayıcıda sonra giriş yapan BAŞKA bir kullanıcı, bir önceki kullanıcının
  // görevlerini (önbellekten) bir anlığına görebilirdi. Bu bir veri sızıntısıdır.
  const endSession = useCallback(() => {
    setAccessToken(null);
    queryClient.clear();
    setState({ status: 'anonymous' });
  }, [queryClient]);

  // SAYFA AÇILINCA: Access token bellekte olduğu için sayfa yenilenince kaybolur.
  // httpOnly cookie'deki refresh token ile sessizce yeni bir access token istiyoruz.
  // Cookie geçerliyse kullanıcı giriş ekranını hiç görmez ("beni hatırla" etkisi).
  useEffect(() => {
    let cancelled = false;

    refreshSession()
      .then((session) => {
        if (cancelled) return;
        setState(session ? { status: 'authenticated', user: session.user } : { status: 'anonymous' });
      })
      .catch(() => {
        // Ağ hatası (backend kapalı): giriş ekranını göster; giriş denemesi anlamlı bir hata verecek.
        if (!cancelled) setState({ status: 'anonymous' });
      });

    // StrictMode bu effect'i İKİ KEZ çalıştırır (mount → unmount → mount).
    // "cancelled" bayrağı ilk çalıştırmanın sonucunun state'i ezmesini önler;
    // refreshSession'daki single-flight ise iki çalıştırmanın İKİ refresh isteği atmasını önler.
    return () => {
      cancelled = true;
    };
  }, []);

  // api/client.ts'nin "oturum bitti" haberini dinle (bir istek 401 aldı ve refresh de başarısız oldu).
  useEffect(() => {
    setSessionExpiredHandler(endSession);
    return () => setSessionExpiredHandler(null);
  }, [endSession]);

  const startSession = useCallback(
    (session: AuthResponse) => {
      setAccessToken(session.accessToken);
      queryClient.clear(); // önceki kullanıcıdan kalmış olabilecek veriyi temizle
      setState({ status: 'authenticated', user: session.user });
    },
    [queryClient],
  );

  const login = useCallback(
    async (request: LoginRequest) => startSession(await authApi.login(request)),
    [startSession],
  );

  const register = useCallback(
    async (request: RegisterRequest) => startSession(await authApi.register(request)),
    [startSession],
  );

  const logout = useCallback(async () => {
    try {
      await authApi.logout(); // sunucuda refresh token'ı iptal et + cookie'yi sil
    } finally {
      endSession(); // sunucuya ulaşılamasa bile istemci tarafında oturumu kapat
    }
  }, [endSession]);

  // useMemo: value nesnesi her çizimde YENİDEN oluşturulsaydı, Context'i kullanan TÜM component'ler
  // (değer aslında değişmemiş olsa bile) gereksiz yere yeniden çizilirdi.
  const value = useMemo<AuthContextValue>(
    () => ({ ...state, login, register, logout }),
    [state, login, register, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
