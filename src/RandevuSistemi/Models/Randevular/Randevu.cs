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
        DateTime baslangic, DateTime olusturmaZamani, RandevuDurumu durum, string? not,
        string? sonIslemYapan, DateTime? sonIslemZamani)
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
            Not = not,
            SonIslemYapan = sonIslemYapan,
            SonIslemZamani = sonIslemZamani
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
    /// Sadece aşağıdaki kurallı metotlarla değişir: Onayla, Reddet, IptalEt, Tamamla.
    /// </summary>
    public RandevuDurumu Durum { get; private set; }

    /// <summary>Reddetme veya iptal sebebi gibi açıklamalar.</summary>
    public string? Not { get; private set; }

    /// <summary>Durumu en son değiştiren kişi (ör. "Mehmet Kaya") ve zamanı.</summary>
    public string? SonIslemYapan { get; private set; }
    public DateTime? SonIslemZamani { get; private set; }

    public string UcretMetni => $"{Ucret:N2} ₺";

    /// <summary>
    /// Bu randevu verilen zaman aralığıyla çakışıyor mu?
    /// Uç uca randevular (09:00-09:20 ve 09:20-09:40) çakışmaz.
    /// </summary>
    public bool CakisiyorMu(DateTime baslangic, DateTime bitis) =>
        Baslangic < bitis && baslangic < Bitis;

    // =====================================================================
    // ONAY MEKANİZMASI
    //
    //   Beklemede ──Onayla──► Onaylandi ──Tamamla──► Tamamlandi
    //       │                     │
    //       ├──Reddet──► Reddedildi
    //       └──IptalEt──► IptalEdildi ◄──IptalEt──┘
    //
    // Her metot iki şeyi kontrol eder:
    //  1) YETKİ: işlemi yapan kişi bunu yapabilir mi? (Kullanici sınıfındaki virtual metotlar;
    //     her rol kendi kuralını override eder → ÇOK BİÇİMLİLİK)
    //  2) DURUM: randevunun şu anki durumundan istenen duruma geçilebilir mi?
    // =====================================================================

    /// <summary>Şu anki durumdan verilen duruma geçiş kurallara uygun mu?</summary>
    public bool GecisMumkunMu(RandevuDurumu yeniDurum) => (Durum, yeniDurum) switch
    {
        (RandevuDurumu.Beklemede, RandevuDurumu.Onaylandi) => true,
        (RandevuDurumu.Beklemede, RandevuDurumu.Reddedildi) => true,
        (RandevuDurumu.Beklemede or RandevuDurumu.Onaylandi, RandevuDurumu.IptalEdildi) => true,
        (RandevuDurumu.Onaylandi, RandevuDurumu.Tamamlandi) => true,
        _ => false
    };

    /// <summary>Doktor/personel (kendi randevusu) veya yönetici randevuyu onaylar.</summary>
    public void Onayla(Kullanici yapan)
    {
        YetkiKontrolu(yapan.RandevuOnaylayabilirMi(this), "onaylama");
        GecisKontrolu(RandevuDurumu.Onaylandi);
        DurumDegistir(RandevuDurumu.Onaylandi, yapan, not: null);
    }

    /// <summary>Doktor/personel veya yönetici randevuyu reddeder. Sebep yazmak zorunludur.</summary>
    public void Reddet(Kullanici yapan, string sebep)
    {
        YetkiKontrolu(yapan.RandevuOnaylayabilirMi(this), "reddetme");
        GecisKontrolu(RandevuDurumu.Reddedildi);
        string temizSebep = Dogrulama.BosOlamaz(sebep, "Reddetme sebebi");
        DurumDegistir(RandevuDurumu.Reddedildi, yapan, temizSebep);
    }

    /// <summary>Hasta (kendi randevusu), ilgili çalışan veya yönetici randevuyu iptal eder.</summary>
    public void IptalEt(Kullanici yapan, string? sebep)
    {
        YetkiKontrolu(yapan.RandevuIptalEdebilirMi(this), "iptal etme");
        GecisKontrolu(RandevuDurumu.IptalEdildi);
        if (Baslangic <= DateTime.Now)
            throw new InvalidOperationException("Başlamış veya geçmiş bir randevu iptal edilemez.");

        DurumDegistir(RandevuDurumu.IptalEdildi, yapan, string.IsNullOrWhiteSpace(sebep) ? null : sebep.Trim());
    }

    /// <summary>Randevu gerçekleştikten sonra doktor/personel veya yönetici "tamamlandı" olarak işaretler.</summary>
    public void Tamamla(Kullanici yapan)
    {
        YetkiKontrolu(yapan.RandevuOnaylayabilirMi(this), "tamamlama");
        GecisKontrolu(RandevuDurumu.Tamamlandi);
        if (Baslangic.Date > DateTime.Today)
            throw new InvalidOperationException("Randevu günü gelmeden randevu tamamlandı olarak işaretlenemez.");

        DurumDegistir(RandevuDurumu.Tamamlandi, yapan, Not);
    }

    private static void YetkiKontrolu(bool yetkiliMi, string islem)
    {
        if (!yetkiliMi)
            throw new UnauthorizedAccessException($"Bu randevu için {islem} yetkiniz yok.");
    }

    private void GecisKontrolu(RandevuDurumu yeniDurum)
    {
        if (!GecisMumkunMu(yeniDurum))
            throw new InvalidOperationException(
                $"\"{Durum.GorunenAd()}\" durumundaki bir randevu \"{yeniDurum.GorunenAd()}\" durumuna geçemez.");
    }

    private void DurumDegistir(RandevuDurumu yeniDurum, Kullanici yapan, string? not)
    {
        Durum = yeniDurum;
        Not = not;
        SonIslemYapan = $"{yapan.TamAd} ({yapan.Rol.GorunenAd()})";
        SonIslemZamani = DateTime.Now;
    }

    public override string ToString() =>
        $"{Baslangic:dd.MM.yyyy HH:mm} - {HizmetAdi} ({CalisanAdi}) [{Durum.GorunenAd()}]";
}
