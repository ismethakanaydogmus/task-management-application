import { afterEach, describe, expect, it, vi } from 'vitest';
import { ApiError, apiFetch, setAccessToken, setSessionExpiredHandler } from './client';

// api/client.ts'nin en kritik mantığı: 401 → refresh → isteği tekrarla, ve SINGLE-FLIGHT refresh.
// Gerçek ağ yerine global fetch'i bir STUB ile değiştiriyoruz: hangi URL'ye ne cevap döneceğine biz karar veriyoruz.

const json = (status: number, body: unknown) =>
  new Response(JSON.stringify(body), { status, headers: { 'content-type': 'application/problem+json' } });

afterEach(() => {
  // client.ts modül seviyesinde durum (token, handler) tutuyor → testler arasında sıfırla.
  setAccessToken(null);
  setSessionExpiredHandler(null);
  vi.unstubAllGlobals();
});

describe('apiFetch', () => {
  it('Authorization başlığına bellekteki access tokenı ekler', async () => {
    const fetchMock = vi.fn().mockResolvedValue(json(200, []));
    vi.stubGlobal('fetch', fetchMock);
    setAccessToken('abc');

    await apiFetch('/api/tasks');

    const [url, init] = fetchMock.mock.calls[0];
    expect(url).toBe('http://api.test/api/tasks');
    expect(init.headers.Authorization).toBe('Bearer abc');
  });

  it('ProblemDetails hatasını alan hatalarıyla birlikte ApiErrora çevirir', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(
      json(400, { title: 'Geçersiz', errors: { title: ['Başlık boş olamaz.'] }, traceId: '00-abc' }),
    ));

    const error = await apiFetch('/api/tasks').catch((e: unknown) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({ status: 400, fieldErrors: { title: ['Başlık boş olamaz.'] }, traceId: '00-abc' });
  });

  it('204 No Content cevabında gövde okumaya çalışmaz', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 204 })));

    await expect(apiFetch('/api/tasks/1', { method: 'DELETE' })).resolves.toBeUndefined();
  });

  it('ağ hatasını status=0 ApiErrora çevirir', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')));

    const error = await apiFetch('/api/tasks').catch((e: unknown) => e);

    expect(error).toMatchObject({ status: 0, isNetworkError: true });
  });

  it('aynı anda 401 alan iki istek için TEK refresh yapar ve ikisini de yeni tokenla tekrarlar', async () => {
    // Senaryo: access token'ın süresi dolmuş; sayfa açılırken iki sorgu aynı anda gidiyor.
    // Single-flight OLMASAYDI iki refresh gider; rotation yüzünden ikincisi az önce iptal edilmiş token'ı
    // gönderir, backend bunu "çalınmış token" sanıp kullanıcının TÜM oturumlarını kapatırdı (Aşama 7).
    setAccessToken('expired');
    const fetchMock = vi.fn(async (url: string, init: RequestInit) => {
      if (url.endsWith('/api/auth/refresh')) {
        await new Promise((r) => setTimeout(r, 10)); // refresh biraz sürsün → iki istek de onu beklesin
        return json(200, { accessToken: 'fresh', accessTokenExpiresAt: '', user: { id: 1, email: 'a@b.c' } });
      }
      const auth = (init.headers as Record<string, string>).Authorization;
      return auth === 'Bearer fresh' ? json(200, { ok: url }) : json(401, { title: 'Unauthorized' });
    });
    vi.stubGlobal('fetch', fetchMock);

    const [a, b] = await Promise.all([apiFetch('/api/tasks'), apiFetch('/api/tasks/summary')]);

    const refreshCalls = fetchMock.mock.calls.filter(([url]) => url.endsWith('/api/auth/refresh'));
    expect(refreshCalls).toHaveLength(1);
    expect(refreshCalls[0][1].credentials).toBe('include'); // cookie gönderilmeli
    expect(a).toEqual({ ok: 'http://api.test/api/tasks' });
    expect(b).toEqual({ ok: 'http://api.test/api/tasks/summary' });
  });

  it('refresh de başarısızsa oturumun bittiğini bildirir', async () => {
    setAccessToken('expired');
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(json(401, { title: 'Unauthorized' })));
    const onExpired = vi.fn();
    setSessionExpiredHandler(onExpired);

    const error = await apiFetch('/api/tasks').catch((e: unknown) => e);

    expect(error).toMatchObject({ status: 401 });
    expect(onExpired).toHaveBeenCalledOnce();
  });
});
