import { useState, type FormEvent } from 'react';
import { ApiError } from '../api/client';
import { useAuth } from '../auth/authContext';

type Mode = 'login' | 'register';

export function AuthForm() {
  const { login, register } = useAuth();
  const [mode, setMode] = useState<Mode>('login');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const isLogin = mode === 'login';

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFieldErrors({});
    setFormError(null);
    setSubmitting(true);

    try {
      await (isLogin ? login : register)({ email, password });
      // Başarılıysa AuthProvider state'i 'authenticated' olur ve bu component ekrandan kalkar.
    } catch (error) {
      if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
        setFieldErrors(error.fieldErrors);
      } else {
        setFormError(error instanceof Error ? error.message : 'Beklenmeyen bir hata oluştu.');
      }
      setSubmitting(false);
    }
  }

  function switchMode() {
    setMode(isLogin ? 'register' : 'login');
    setFieldErrors({});
    setFormError(null);
  }

  return (
    <main className="container auth-container">
      <header className="app-header">
        <h1>Görev Yöneticisi</h1>
        <p className="subtitle">{isLogin ? 'Devam etmek için giriş yap.' : 'Yeni bir hesap oluştur.'}</p>
      </header>

      <section className="card">
        <form className="task-form" onSubmit={handleSubmit} noValidate>
          <div className="field">
            <label htmlFor="email">E-posta</label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              // autoComplete: tarayıcının ve şifre yöneticilerinin alanı doğru tanıması için
              autoComplete="email"
              aria-invalid={!!fieldErrors.email}
              aria-describedby={fieldErrors.email ? 'email-error' : undefined}
              required
            />
            {fieldErrors.email && (
              <span id="email-error" className="field-error" role="alert">
                {fieldErrors.email.join(' ')}
              </span>
            )}
          </div>

          <div className="field">
            <label htmlFor="password">Şifre</label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              // "new-password": şifre yöneticisi güçlü şifre önerir; "current-password": kayıtlı şifreyi doldurur
              autoComplete={isLogin ? 'current-password' : 'new-password'}
              aria-invalid={!!fieldErrors.password}
              aria-describedby={fieldErrors.password ? 'password-error' : isLogin ? undefined : 'password-hint'}
              required
            />
            {fieldErrors.password ? (
              <span id="password-error" className="field-error" role="alert">
                {fieldErrors.password.join(' ')}
              </span>
            ) : (
              !isLogin && (
                <span id="password-hint" className="field-hint">
                  En az 8 karakter. Uzun bir cümle, karmaşık kısa bir şifreden daha güvenlidir.
                </span>
              )
            )}
          </div>

          {formError && (
            <p className="form-error" role="alert">
              {formError}
            </p>
          )}

          <button type="submit" className="btn-primary" disabled={submitting}>
            {submitting ? 'Lütfen bekle…' : isLogin ? 'Giriş yap' : 'Kayıt ol'}
          </button>
        </form>

        <p className="auth-switch">
          {isLogin ? 'Hesabın yok mu?' : 'Zaten hesabın var mı?'}{' '}
          <button type="button" className="link-button" onClick={switchMode}>
            {isLogin ? 'Kayıt ol' : 'Giriş yap'}
          </button>
        </p>
      </section>
    </main>
  );
}
