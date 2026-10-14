// Backend ile konuşan TEK nokta. Component'ler fetch'i doğrudan çağırmaz.
// Aşama 7: Access token burada (bellekte) tutulur ve her isteğe eklenir; 401 gelirse sessizce yenilenir.
import type { AuthResponse } from '../types/auth';

const API_URL = import.meta.env.VITE_API_URL;

// Backend'in döndürdüğü ProblemDetails (RFC 9457) şekli — Aşama 4'te biz tasarladık.
type ProblemDetails = {
  title?: string;
  status?: number;
  detail?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
};

export class ApiError extends Error {
  readonly status: number;
  readonly fieldErrors: Record<string, string[]>;
  readonly traceId?: string;

  constructor(status: number, message: string, fieldErrors: Record<string, string[]> = {}, traceId?: string) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.fieldErrors = fieldErrors;
    this.traceId = traceId;
  }

  // 0 = sunucuya hiç ulaşılamadı (backend kapalı, internet yok, CORS reddi)
  get isNetworkError() {
    return this.status === 0;
  }
}

// ---------------------------------------------------------------------------
// ACCESS TOKEN NEREDE? → Bu modülün içindeki bir DEĞİŞKENDE (JavaScript belleği).
//
//  localStorage'da OLSAYDI: Sayfaya sızan herhangi bir script (XSS: kötü niyetli bir npm paketi,
//  render edilen kullanıcı girdisi...) localStorage.getItem('token') ile token'ı okuyup saldırgana gönderebilirdi.
//  Token sayfa kapansa bile diskte kalırdı.
//
//  Bellekte: Sayfa yenilenince kaybolur → httpOnly cookie'deki refresh token ile sessizce yenisi alınır.
//  XSS yine tehlikelidir (saldırgan sayfa açıkken istek atabilir) ama token'ı alıp götüremez ve ömrü 15 dk.
// ---------------------------------------------------------------------------
let accessToken: string | null = null;

export function setAccessToken(token: string | null) {
  accessToken = token;
}

// Refresh başarısız olunca (oturum gerçekten bitti) AuthProvider'a haber vermek için.
let onSessionExpired: (() => void) | null = null;

export function setSessionExpiredHandler(handler: (() => void) | null) {
  onSessionExpired = handler;
}

type FetchOptions = {
  // false: auth endpoint'leri (login/register/refresh/logout). Bearer token eklenmez, 401'de yenileme denenmez,
  // cookie gönderilir/alınır (credentials: 'include').
  auth?: boolean;
};

export async function apiFetch<T>(path: string, init: RequestInit = {}, { auth = true }: FetchOptions = {}): Promise<T> {
  let response = await send(path, init, auth);

  // Access token'ın süresi dolmuş olabilir → bir kez yenilemeyi dene, başarılıysa isteği TEKRARLA.
  // Kullanıcı hiçbir şey fark etmez.
  if (response.status === 401 && auth) {
    const session = await refreshSession();
    if (session) {
      response = await send(path, init, auth);
    } else {
      onSessionExpired?.();
    }
  }

  // DİKKAT: fetch 4xx/5xx cevaplarda exception FIRLATMAZ; response.ok kontrolünü biz yapıyoruz.
  if (!response.ok) {
    throw await toApiError(response);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

// ---------------------------------------------------------------------------
// SINGLE-FLIGHT REFRESH: Aynı anda birden fazla istek 401 alırsa (ör. sayfa açılırken 3 sorgu),
// HER BİRİ ayrı refresh isteği atmamalı. İlk çağrı Promise'i oluşturur, diğerleri AYNI Promise'i bekler.
//
// Bu neden ŞART? Refresh token ROTATION kullanıyoruz: her refresh eski token'ı iptal eder.
// İki paralel refresh olsaydı ikincisi az önce iptal edilmiş token'ı gönderirdi → backend bunu
// "çalınmış token" sanar (reuse detection) ve kullanıcının TÜM oturumlarını kapatırdı.
// React StrictMode'un effect'leri iki kez çalıştırması da tam olarak bu durumu tetikler.
// ---------------------------------------------------------------------------
let refreshPromise: Promise<AuthResponse | null> | null = null;

export function refreshSession(): Promise<AuthResponse | null> {
  refreshPromise ??= (async () => {
    try {
      const session = await apiFetch<AuthResponse>('/api/auth/refresh', { method: 'POST' }, { auth: false });
      accessToken = session.accessToken;
      return session;
    } catch (error) {
      // 401 = geçerli oturum yok (cookie yok/süresi dolmuş/iptal). Bu bir "hata" değil, "giriş yapılmamış" demek.
      if (error instanceof ApiError && error.status === 401) {
        accessToken = null;
        return null;
      }
      throw error; // ağ hatası vb. → çağıran karar versin
    } finally {
      refreshPromise = null;
    }
  })();
  return refreshPromise;
}

async function send(path: string, init: RequestInit, auth: boolean): Promise<Response> {
  try {
    return await fetch(`${API_URL}${path}`, {
      ...init,
      // 'include': cross-origin istekte cookie'yi gönder ve Set-Cookie'yi kabul et.
      // Backend'de AllowCredentials() ile birlikte gerekir. Sadece auth endpoint'lerinde açıyoruz;
      // görev istekleri kimliğini Authorization header'ı ile kanıtlar, cookie'ye ihtiyacı yok.
      credentials: auth ? 'same-origin' : 'include',
      headers: {
        Accept: 'application/json',
        ...(init.body ? { 'Content-Type': 'application/json' } : {}),
        ...(auth && accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
        ...init.headers,
      },
    });
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') throw error;
    // fetch SADECE ağ seviyesinde hata olunca fırlatır (sunucu kapalı, DNS, CORS reddi).
    throw new ApiError(0, 'Sunucuya ulaşılamadı. Backend çalışıyor mu?');
  }
}

async function toApiError(response: Response): Promise<ApiError> {
  if ((response.headers.get('content-type') ?? '').includes('json')) {
    try {
      const problem = (await response.json()) as ProblemDetails;
      return new ApiError(
        response.status,
        problem.detail ?? problem.title ?? `İstek başarısız (${response.status})`,
        problem.errors ?? {},
        problem.traceId,
      );
    } catch {
      // gövde bozuksa aşağıdaki genel mesaja düş
    }
  }

  const message =
    response.status === 429 ? 'Çok fazla deneme yaptın. Biraz bekleyip tekrar dene.' : `İstek başarısız (${response.status})`;
  return new ApiError(response.status, message);
}
