using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using TaskManager.Application.Abstractions;

namespace TaskManager.Api.Auth;

// ICurrentUser'ın HTTP gerçekleştirimi: JwtBearer middleware'inin doğruladığı token'daki "sub" claim'i.
//
// IHttpContextAccessor: Controller dışındaki sınıfların (burada: service'lerin dolaylı bağımlılığı) o anki
// HTTP isteğine erişmesini sağlar. Scoped kaydedildiği için her istek kendi kullanıcısını görür.
internal sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public int UserId
    {
        get
        {
            var sub = accessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            // Token doğrulandıysa "sub" her zaman vardır. Yoksa bu bir programlama hatasıdır
            // (ör. bir endpoint'e yanlışlıkla [AllowAnonymous] konmuş). Sessizce 0 ile devam etmek yerine
            // durdur: "fail closed". 0 ile devam etseydik sorgu boş sonuç döner, hata gizlenirdi.
            if (!int.TryParse(sub, out var userId))
                throw new InvalidOperationException("Kimliği doğrulanmış kullanıcı bulunamadı.");

            return userId;
        }
    }
}
