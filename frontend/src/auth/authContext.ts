import { createContext, useContext } from 'react';
import type { LoginRequest, RegisterRequest, UserResponse } from '../types/auth';

// "Discriminated union": status alanına bakarak TypeScript hangi alanların var olduğunu bilir.
// status === 'authenticated' ise user KESİN var; diğer durumlarda user'a erişmeye çalışmak derleme hatası.
export type AuthState =
  | { status: 'loading' }                                  // sayfa açıldı, oturum kontrol ediliyor
  | { status: 'anonymous' }                                // giriş yapılmamış
  | { status: 'authenticated'; user: UserResponse };

export type AuthContextValue = AuthState & {
  login: (request: LoginRequest) => Promise<void>;
  register: (request: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
};

// CONTEXT: Bir değeri ağacın derinliklerindeki component'lere props ile tek tek taşımadan ulaştırır.
// Oturum bilgisi; header, görev sayfası, giriş formu... birçok yerde lazım → prop drilling yerine Context.
// Not: Context SIK DEĞİŞEN veriler için uygun değildir (her değişiklikte tüm tüketiciler yeniden çizilir).
// Oturum nadiren değişir → ideal kullanım.
export const AuthContext = createContext<AuthContextValue | null>(null);

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    // Provider'ı sarmalamayı unutursan sessizce null ile çalışmak yerine açık bir hata ver.
    throw new Error('useAuth, <AuthProvider> içinde kullanılmalı.');
  }
  return context;
}
