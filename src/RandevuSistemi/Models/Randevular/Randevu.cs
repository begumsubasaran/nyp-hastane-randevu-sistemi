using RandevuSistemi.Models.Hizmetler;
using RandevuSistemi.Models.Kullanicilar;

namespace RandevuSistemi.Models.Randevular;

/// <summary>
/// Bir hastanın, bir çalışandan, belirli bir hizmet için aldığı randevu.
///
/// Randevu detayları (işlem, süre, ücret, çalışan, tarih/saat) randevu oluşturulurken
/// KOPYALANARAK saklanır. Böylece hizmetin fiyatı sonradan değişse bile
/// eski randevuların ücreti değişmez (gerçek sistemlerde de böyledir).
///
/// Bu sınıf sadece randevunun KENDİ kurallarını kontrol eder.
/// "Çalışan o saatte uygun mu, başka randevuyla çakışıyor mu" gibi diğer randevulara
/// bakmayı gerektiren kontroller RandevuServisi'nde yapılacak.
/// </summary>
public class Randevu
{
    public Randevu(Hasta hasta, Calisan calisan, SaglikHizmeti hizmet, DateTime baslangic)
    {
        if (!calisan.AktifMi)
            throw new InvalidOperationException($"{calisan.TamAd} aktif bir çalışan değil.");
        if (calisan.SubeId is null)
            throw new InvalidOperationException($"{calisan.TamAd} herhangi bir şubeye atanmamış.");
        if (!calisan.HizmetSunuyorMu(hizmet.Id))
            throw new InvalidOperationException($"{calisan.TamAd}, \"{hizmet.Ad}\" hizmetini sunmuyor.");

        HastaId = hasta.Id;
        CalisanId = calisan.Id;
        SubeId = calisan.SubeId.Value;
        HizmetId = hizmet.Id;

        // Randevu anındaki bilgiler kopyalanır (snapshot)
        HastaAdi = hasta.TamAd;
        CalisanAdi = $"{calisan.TamAd} ({calisan.Unvan})";
        HizmetAdi = hizmet.Ad;
        SureDakika = hizmet.SureDakika;
        Ucret = hizmet.Ucret;

        // Saniye ve salise kısmı atılır (ör. 09:30:00)
        Baslangic = new DateTime(baslangic.Year, baslangic.Month, baslangic.Day,
                                 baslangic.Hour, baslangic.Minute, 0);
        OlusturmaZamani = DateTime.Now;
        Durum = RandevuDurumu.Beklemede;
    }

    /// <summary>
    /// Sadece veri dosyasından yükleme için kullanılan boş constructor (private).
    /// Dışarıdan çağrılamaz; KayittanYukle metodu kullanır.
    /// </summary>
    private Randevu()
    {
        HastaAdi = CalisanAdi = HizmetAdi = string.Empty;
    }

    /// <summary>
    /// Kaydedilmiş bir randevuyu olduğu gibi geri oluşturur.
    /// Normal constructor'daki kontroller (çalışan bu hizmeti sunuyor mu vb.) burada YAPILMAZ:
    /// geçmişte alınmış bir randevu, çalışan o hizmeti artık sunmasa bile korunmalıdır.
    /// </summary>
    internal static Randevu KayittanYukle(
        Guid id, Guid hastaId, Guid calisanId, Guid subeId, Guid hizmetId,
        string hastaAdi, string calisanAdi, string hizmetAdi, int sureDakika, decimal ucret,
        DateTime baslangic, DateTime olusturmaZamani, RandevuDurumu durum, string? not)
    {
        return new Randevu
        {
            Id = id,
            HastaId = hastaId,
            CalisanId = calisanId,
            SubeId = subeId,
            HizmetId = hizmetId,
            HastaAdi = hastaAdi,
            CalisanAdi = calisanAdi,
            HizmetAdi = hizmetAdi,
            SureDakika = sureDakika,
            Ucret = ucret,
            Baslangic = baslangic,
            OlusturmaZamani = olusturmaZamani,
            Durum = durum,
            Not = not
        };
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    // ---- Kimlik bilgileri (ilişkili nesnelere ulaşmak için) ----
    public Guid HastaId { get; private set; }
    public Guid CalisanId { get; private set; }
    public Guid SubeId { get; private set; }
    public Guid HizmetId { get; private set; }

    // ---- Randevu anında kopyalanan detaylar ----
    public string HastaAdi { get; private set; }
    public string CalisanAdi { get; private set; }
    public string HizmetAdi { get; private set; }
    public int SureDakika { get; private set; }
    public decimal Ucret { get; private set; }

    // ---- Zaman ----
    public DateTime Baslangic { get; private set; }

    /// <summary>Bitiş saati saklanmaz; başlangıç + hizmet süresinden hesaplanır.</summary>
    public DateTime Bitis => Baslangic.AddMinutes(SureDakika);

    public DateTime OlusturmaZamani { get; private set; }

    // ---- Durum ----

    /// <summary>
    /// Durum dışarıdan doğrudan değiştirilemez (private set).
    /// Onaylama, reddetme, iptal gibi geçişler kurallı metotlarla yapılacak (onay mekanizması adımı).
    /// </summary>
    public RandevuDurumu Durum { get; private set; }

    /// <summary>Reddetme veya iptal sebebi gibi açıklamalar.</summary>
    public string? Not { get; private set; }

    public string UcretMetni => $"{Ucret:N2} ₺";

    /// <summary>
    /// Bu randevu verilen zaman aralığıyla çakışıyor mu?
    /// Uç uca randevular (09:00-09:20 ve 09:20-09:40) çakışmaz.
    /// </summary>
    public bool CakisiyorMu(DateTime baslangic, DateTime bitis) =>
        Baslangic < bitis && baslangic < Bitis;

    public override string ToString() =>
        $"{Baslangic:dd.MM.yyyy HH:mm} - {HizmetAdi} ({CalisanAdi}) [{Durum.GorunenAd()}]";
}
