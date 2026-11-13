using RandevuSistemi.Models.Randevular;

namespace RandevuSistemi.Models.Kullanicilar;

/// <summary>
/// Yöneticinin yetki kapsamı.
/// - SistemYoneticisi: tüm kurumları (hastane/klinik) tanımlayabilir ve yönetebilir.
/// - KurumYoneticisi: sadece kendi kurumunu ve şubelerini yönetir.
/// </summary>
public enum YoneticiYetkisi
{
    SistemYoneticisi,
    KurumYoneticisi
}

/// <summary>
/// Hastane / Klinik yöneticisi. Kurum, şube, hizmet ve çalışan tanımlarını yapar,
/// randevuları onaylayabilir. Hangi kuruma bağlı olduğu sonraki adımlarda eklenecek.
/// </summary>
public class Yonetici : Kullanici
{
    public Yonetici(string ad, string soyad, string tcKimlikNo, string kullaniciAdi, string sifre,
                    YoneticiYetkisi yetki)
        : base(ad, soyad, tcKimlikNo, kullaniciAdi, sifre)
    {
        Yetki = yetki;
    }

    public override Rol Rol => Rol.Yonetici;

    public YoneticiYetkisi Yetki { get; set; }

    /// <summary>Yönetici tüm randevuları onaylayabilir / reddedebilir / tamamlayabilir.</summary>
    public override bool RandevuOnaylayabilirMi(Randevu randevu) => true;

    /// <summary>Yönetici tüm randevuları iptal edebilir.</summary>
    public override bool RandevuIptalEdebilirMi(Randevu randevu) => true;

    /// <summary>Yönetici tüm randevuları görür.</summary>
    public override bool RandevuyuGorebilirMi(Randevu randevu) => true;

    /// <summary>Yönetici herhangi bir hasta adına randevu oluşturabilir (ör. telefonla arayan hasta).</summary>
    public override bool RandevuOlusturabilirMi => true;

    public override string BilgiOzeti()
    {
        string yetkiAdi = Yetki == YoneticiYetkisi.SistemYoneticisi ? "Sistem Yöneticisi" : "Kurum Yöneticisi";
        return $"{base.BilgiOzeti()} - {yetkiAdi}";
    }
}
