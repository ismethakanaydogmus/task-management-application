using FluentValidation;

namespace TaskManager.Application.Auth;

internal sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta boş olamaz.")
            .MaximumLength(256).WithMessage("E-posta en fazla 256 karakter olabilir.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.");

        // Uzunluk, karmaşıklık kurallarından ("en az 1 büyük harf, 1 sembol") daha etkilidir
        // (NIST SP 800-63B önerisi). Karmaşıklık kuralları kullanıcıları "Sifre123!" gibi tahmin edilebilir kalıplara iter.
        //
        // ÜST SINIR neden var? Hash algoritması bilerek yavaştır; 1 MB'lık bir "şifre" göndererek
        // sunucuyu meşgul etmek (DoS) mümkün olmasın.
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre boş olamaz.")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalı.")
            .MaximumLength(128).WithMessage("Şifre en fazla 128 karakter olabilir.");
    }
}

internal sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        // Girişte format kurallarını (min uzunluk vb.) KONTROL ETMİYORUZ: kurallar ileride değişirse
        // eski kurallarla kayıt olmuş kullanıcılar giriş yapamaz hale gelmesin. Sadece boşluk ve üst sınır.
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta boş olamaz.")
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre boş olamaz.")
            .MaximumLength(128);
    }
}
