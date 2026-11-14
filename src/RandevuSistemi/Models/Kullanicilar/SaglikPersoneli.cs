namespace RandevuSistemi.Models.Kullanicilar;

/// <summary>Doktor dışındaki sağlık personelinin görevleri.</summary>
public enum PersonelGorevi
{
    Hemsire,
    Ebe,
    Laborant,
    RadyolojiTeknisyeni,
    Fizyoterapist
}

/// <summary>
/// Doktor dışındaki sağlık çalışanları (hemşire, laborant vb.).
/// Bunlardan sadece FİZYOTERAPİSTLERE randevu alınabilir; diğer görevler hastaya randevu vermez.
/// Bu kural RandevuKabulEderMi özelliği override edilerek tanımlanır (çok biçimlilik).
/// </summary>
public class SaglikPersoneli : Calisan
{
    public SaglikPersoneli(string ad, string soyad, string tcKimlikNo, string kullaniciAdi, string sifre,
                           PersonelGorevi gorev)
        : base(ad, soyad, tcKimlikNo, kullaniciAdi, sifre)
    {
        Gorev = gorev;
    }

    public override Rol Rol => Rol.SaglikPersoneli;

    public PersonelGorevi Gorev { get; set; }

    /// <summary>
    /// Fizyoterapistlerin çalıştığı bölüm. Hasta bu bölümü seçince bölümün doktorlarıyla birlikte
    /// fizyoterapistler de listelenir (bölüm, şubenin bölümleri arasında tanımlı olmalıdır).
    /// </summary>
    public const string FizyoterapiBolumu = "Fiziksel Tıp ve Rehabilitasyon";

    /// <summary>Sağlık personelinden sadece fizyoterapist randevu kabul eder.</summary>
    public override bool RandevuKabulEderMi => Gorev == PersonelGorevi.Fizyoterapist;

    /// <summary>Fizyoterapist "Fiziksel Tıp ve Rehabilitasyon" bölümünde listelenir; diğer personel hiçbir bölümde değildir.</summary>
    public override string? BolumAdi => RandevuKabulEderMi ? FizyoterapiBolumu : null;

    public override string Unvan => Gorev switch
    {
        PersonelGorevi.Hemsire => "Hemşire",
        PersonelGorevi.Ebe => "Ebe",
        PersonelGorevi.Laborant => "Laborant",
        PersonelGorevi.RadyolojiTeknisyeni => "Radyoloji Teknisyeni",
        PersonelGorevi.Fizyoterapist => "Fizyoterapist",
        _ => Gorev.ToString()
    };
}
