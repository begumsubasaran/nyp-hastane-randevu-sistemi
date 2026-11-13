using RandevuSistemi.Models.Kullanicilar;
using RandevuSistemi.Veri;

namespace RandevuSistemi.Servisler;

/// <summary>
/// Kullanıcı girişini (oturum açmayı) yöneten servis.
/// Kullanıcı adı ve şifreyi kontrol eder; doğruysa giriş yapan kullanıcıyı döndürür.
/// </summary>
public class OturumServisi
{
    private readonly UygulamaVerisi _veri;

    public OturumServisi(UygulamaVerisi veri)
    {
        _veri = veri;
    }

    /// <summary>
    /// Giriş yapmayı dener. Başarısız olursa nedenini açıklayan bir hata fırlatır.
    /// Güvenlik için "kullanıcı yok" ile "şifre yanlış" ayrımı kullanıcıya söylenmez.
    /// </summary>
    public Kullanici GirisYap(string kullaniciAdi, string sifre)
    {
        if (string.IsNullOrWhiteSpace(kullaniciAdi) || string.IsNullOrEmpty(sifre))
            throw new ArgumentException("Kullanıcı adı ve şifre boş bırakılamaz.");

        // Kullanıcı adları küçük harfle saklanıyor (Kullanici sınıfı böyle kaydediyor).
        string arananAd = kullaniciAdi.Trim().ToLowerInvariant();

        Kullanici? kullanici = _veri.Kullanicilar.FirstOrDefault(k => k.KullaniciAdi == arananAd);

        // Şifre kontrolü Kullanici sınıfının içinde yapılır; şifre özeti dışarı hiç çıkmaz.
        if (kullanici is null || !kullanici.SifreDogruMu(sifre))
            throw new UnauthorizedAccessException("Kullanıcı adı veya şifre hatalı.");

        if (kullanici is Calisan { AktifMi: false })
            throw new UnauthorizedAccessException("Bu hesap pasif durumda. Lütfen yöneticinize başvurun.");

        return kullanici;
    }
}
