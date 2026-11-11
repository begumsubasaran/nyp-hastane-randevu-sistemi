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
/// Örneğin laboratuvar veya radyoloji hizmetlerinde randevu bu kişilere verilebilir.
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
