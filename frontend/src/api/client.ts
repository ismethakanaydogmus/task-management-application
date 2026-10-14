// Backend ile konuşan TEK nokta. Component'ler fetch'i doğrudan çağırmaz.
// Faydası (backend'deki repository fikrinin aynısı):
//  - API adresi, header'lar, hata ayrıştırma TEK yerde.
//  - Aşama 7'de JWT token'ı eklemek için sadece bu dosyaya dokunacağız.

const API_URL = import.meta.env.VITE_API_URL;

// Backend'in döndürdüğü ProblemDetails (RFC 9457) şekli — Aşama 4'te biz tasarladık.
type ProblemDetails = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  errors?: Record<string, string[]>; // { "title": ["Başlık boş olamaz."] }
};

// Tüm API hataları bu sınıfa çevrilir; UI tek bir hata tipiyle uğraşır.
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

export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  let response: Response;

  try {
    response = await fetch(`${API_URL}${path}`, {
      ...init,
      headers: {
        Accept: 'application/json',
        ...(init.body ? { 'Content-Type': 'application/json' } : {}),
        ...init.headers,
      },
    });
  } catch (error) {
    // İstek iptal edildiyse (TanStack Query component kapanınca signal ile iptal eder) olduğu gibi fırlat.
    if (error instanceof DOMException && error.name === 'AbortError') throw error;

    // fetch SADECE ağ seviyesinde hata olunca exception fırlatır (sunucu kapalı, DNS, CORS reddi).
    // Tarayıcı güvenlik gereği CORS hatasının detayını JavaScript'e VERMEZ; sadece "Failed to fetch" der.
    // Detayı görmek için tarayıcının Console/Network sekmesine bakmak gerekir.
    throw new ApiError(0, 'Sunucuya ulaşılamadı. Backend çalışıyor mu?');
  }

  // DİKKAT: fetch 4xx/5xx cevaplarda exception FIRLATMAZ! (axios'tan farklı olarak)
  // response.ok (200-299) kontrolünü kendimiz yapmak zorundayız; unutmak çok yaygın bir hatadır.
  if (!response.ok) {
    throw await toApiError(response);
  }

  // 204 No Content (PUT, DELETE): gövde yok, response.json() çağırırsak hata verir.
  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

async function toApiError(response: Response): Promise<ApiError> {
  const contentType = response.headers.get('content-type') ?? '';

  // Backend'imiz hataları "application/problem+json" olarak döner.
  if (contentType.includes('json')) {
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

  return new ApiError(response.status, `İstek başarısız (${response.status})`);
}
