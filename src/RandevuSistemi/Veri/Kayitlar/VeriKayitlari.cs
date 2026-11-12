using RandevuSistemi.Models.Hizmetler;
using RandevuSistemi.Models.Kullanicilar;
using RandevuSistemi.Models.Kurumlar;
using RandevuSistemi.Models.Randevular;

namespace RandevuSistemi.Veri.Kayitlar;

// =============================================================================
// KAYIT SINIFLARI (DTO - Data Transfer Object)
//
// Model sınıflarımız kurallarını korumak için private alanlar, salt okunur listeler ve
// doğrulamalı constructor'lar kullanıyor. JSON kütüphanesi ise basit, herkese açık
// get/set property'leri olan sınıflarla rahat çalışır.
//
// Bu yüzden diske yazarken modelleri bu sade "kayıt" sınıflarına çeviriyoruz;
// okurken de kayıtlardan modelleri, modelin kendi metotlarıyla (kurallarıyla) yeniden kuruyoruz.
// Böylece model sınıflarının kapsüllemesi bozulmuyor.
// =============================================================================

/// <summary>Veri dosyasının kök nesnesi.</summary>
public class VeriKaydi
{
    /// <summary>Dosya biçimi değişirse eski dosyaları tanıyabilmek için sürüm numarası.</summary>
    public int Surum { get; set; } = 1;
    public DateTime KayitZamani { get; set; }
    public List<KurumKaydi> Kurumlar { get; set; } = new();
    public List<KullaniciKaydi> Kullanicilar { get; set; } = new();
    public List<RandevuKaydi> Randevular { get; set; } = new();
}

public class KurumKaydi
{
    public Guid Id { get; set; }
    public string Ad { get; set; } = "";
    public KurumTuru Tur { get; set; }
    public List<SubeKaydi> Subeler { get; set; } = new();
}

public class SubeKaydi
{
    public Guid Id { get; set; }
    public string Ad { get; set; } = "";
    public string Adres { get; set; } = "";
    public List<CalismaSaatiKaydi> CalismaSaatleri { get; set; } = new();
    public List<BolumKaydi> Bolumler { get; set; } = new();
    public List<HizmetKaydi> Hizmetler { get; set; } = new();
}

public class CalismaSaatiKaydi
{
    public DayOfWeek Gun { get; set; }
    public TimeOnly Acilis { get; set; }
    public TimeOnly Kapanis { get; set; }
}

public class BolumKaydi
{
    public Guid Id { get; set; }
    public string Ad { get; set; } = "";
    public string? Aciklama { get; set; }
}

public class HizmetKaydi
{
    public Guid Id { get; set; }
    public string Ad { get; set; } = "";
    public HizmetKategorisi Kategori { get; set; }
    public int SureDakika { get; set; }
    public decimal Ucret { get; set; }
    public string? Aciklama { get; set; }
}

/// <summary>
/// Tüm kullanıcı türleri için tek kayıt sınıfı. Hangi türden olduğu "Rol" alanında yazar;
/// türe özel alanlar (ör. Hasta için DogumTarihi) diğer türlerde boş (null) kalır.
/// </summary>
public class KullaniciKaydi
{
    public Rol Rol { get; set; }
    public Guid Id { get; set; }
    public string Ad { get; set; } = "";
    public string Soyad { get; set; } = "";
    public string TcKimlikNo { get; set; } = "";
    public string KullaniciAdi { get; set; } = "";
    public string SifreOzeti { get; set; } = "";
    public string? Telefon { get; set; }
    public string? Eposta { get; set; }

    // Hasta
    public DateOnly? DogumTarihi { get; set; }

    // Çalışan (Doktor + Sağlık Personeli)
    public bool AktifMi { get; set; } = true;
    public Guid? SubeId { get; set; }
    public List<Guid> HizmetIdleri { get; set; } = new();
    public List<UygunlukKaydi> UygunlukDilimleri { get; set; } = new();

    // Doktor
    public string? UzmanlikAlani { get; set; }
    public DoktorUnvani? AkademikUnvan { get; set; }

    // Sağlık Personeli
    public PersonelGorevi? Gorev { get; set; }

    // Yönetici
    public YoneticiYetkisi? Yetki { get; set; }
}

public class UygunlukKaydi
{
    public DayOfWeek Gun { get; set; }
    public TimeOnly Baslangic { get; set; }
    public TimeOnly Bitis { get; set; }
}

public class RandevuKaydi
{
    public Guid Id { get; set; }
    public Guid HastaId { get; set; }
    public Guid CalisanId { get; set; }
    public Guid SubeId { get; set; }
    public Guid HizmetId { get; set; }
    public string HastaAdi { get; set; } = "";
    public string CalisanAdi { get; set; } = "";
    public string HizmetAdi { get; set; } = "";
    public int SureDakika { get; set; }
    public decimal Ucret { get; set; }
    public DateTime Baslangic { get; set; }
    public DateTime OlusturmaZamani { get; set; }
    public RandevuDurumu Durum { get; set; }
    public string? Not { get; set; }
}
