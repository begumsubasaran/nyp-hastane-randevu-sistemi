using RandevuSistemi.Models.Randevular;

namespace RandevuSistemi.Models.Kullanicilar;

/// <summary>
/// Randevu alan kullanıcı. Kullanici sınıfından türer (KALITIM).
/// </summary>
public class Hasta : Kullanici
{
    private DateOnly _dogumTarihi;

    public Hasta(string ad, string soyad, string tcKimlikNo, string kullaniciAdi, string sifre,
                 DateOnly dogumTarihi)
        : base(ad, soyad, tcKimlikNo, kullaniciAdi, sifre) // ortak alanlar üst sınıfa gönderilir
    {
        DogumTarihi = dogumTarihi;
    }

    public override Rol Rol => Rol.Hasta;

    public DateOnly DogumTarihi
    {
        get => _dogumTarihi;
        set
        {
            if (value > DateOnly.FromDateTime(DateTime.Today))
                throw new ArgumentException("Doğum tarihi gelecekte olamaz.");
            _dogumTarihi = value;
        }
    }

    /// <summary>Doğum tarihinden hesaplanan yaş (saklanmaz, her seferinde hesaplanır).</summary>
    public int Yas
    {
        get
        {
            DateOnly bugun = DateOnly.FromDateTime(DateTime.Today);
            int yas = bugun.Year - DogumTarihi.Year;
            if (DogumTarihi > bugun.AddYears(-yas))
                yas--; // bu yılki doğum günü henüz gelmediyse
            return yas;
        }
    }

    /// <summary>Hasta sadece KENDİ randevusunu iptal edebilir; onaylama yetkisi yoktur.</summary>
    public override bool RandevuIptalEdebilirMi(Randevu randevu) => randevu.HastaId == Id;

    /// <summary>Üst sınıftaki özet + hastaya özel bilgi (override + base çağrısı).</summary>
    public override string BilgiOzeti() => $"{base.BilgiOzeti()} - Yaş: {Yas}";
}
