using RandevuSistemi.Models.Hizmetler;
using RandevuSistemi.Models.Kullanicilar;
using RandevuSistemi.Models.Kurumlar;
using RandevuSistemi.Models.Randevular;
using RandevuSistemi.Veri;

namespace RandevuSistemi.Servisler;

/// <summary>
/// Randevu oluşturma kurallarını tek bir yerde toplayan servis sınıfı.
///
/// Randevu sınıfı sadece kendi bilgilerini kontrol edebilir. "Bu saatte başka randevu var mı?"
/// sorusunu cevaplamak için TÜM randevulara bakmak gerekir; bu yüzden bu kontroller
/// verilere erişebilen ayrı bir sınıfta yapılır (Tek Sorumluluk İlkesi - SOLID "S").
///
/// Kontrol edilen kurallar:
///  1) Geçmiş bir zamana randevu alınamaz
///  2) Çalışan aktif olmalı, bir şubeye atanmış olmalı ve bu hizmeti sunmalı
///  3) Randevu şubenin çalışma saatleri içinde olmalı
///  4) Randevu çalışanın UYGUNLUK ZAMAN DİLİMLERİNDEN birinin içinde olmalı
///  5) ÇAKIŞMA: çalışanın aynı saatte başka randevusu olmamalı
///  6) ÇAKIŞMA: hastanın aynı saatte başka randevusu olmamalı
/// </summary>
public class RandevuServisi
{
    private readonly UygulamaVerisi _veri;

    public RandevuServisi(UygulamaVerisi veri)
    {
        _veri = veri;
    }

    /// <summary>
    /// Tüm kuralları kontrol eder; uygunsa randevuyu oluşturup listeye ekler.
    /// Kurallardan biri sağlanmazsa RandevuKuraliException fırlatır.
    /// </summary>
    public Randevu RandevuOlustur(Hasta hasta, Calisan calisan, SaglikHizmeti hizmet, DateTime baslangic)
    {
        string? ihlal = KuralIhlali(hasta, calisan, hizmet, baslangic);
        if (ihlal is not null)
            throw new RandevuKuraliException(ihlal);

        var randevu = new Randevu(hasta, calisan, hizmet, baslangic);
        _veri.Randevular.Add(randevu);
        return randevu;
    }

    /// <summary>
    /// Verilen randevu bilgileri kurallara uyuyor mu? Uyuyorsa null,
    /// uymuyorsa kullanıcıya gösterilecek açıklamayı döndürür.
    /// </summary>
    public string? KuralIhlali(Hasta? hasta, Calisan calisan, SaglikHizmeti hizmet, DateTime baslangic)
    {
        DateTime bitis = baslangic.AddMinutes(hizmet.SureDakika);

        // 1) Geçmiş zaman
        if (baslangic <= DateTime.Now)
            return "Geçmiş bir zamana randevu alınamaz.";

        // 2) Çalışan ve hizmet
        if (!calisan.AktifMi)
            return $"{calisan.TamAd} şu anda aktif değil.";
        if (!calisan.HizmetSunuyorMu(hizmet.Id))
            return $"{calisan.TamAd}, \"{hizmet.Ad}\" hizmetini sunmuyor.";

        Sube? sube = _veri.SubeBul(calisan.SubeId);
        if (sube is null)
            return $"{calisan.TamAd} herhangi bir şubeye atanmamış.";
        if (sube.HizmetBul(hizmet.Id) is null)
            return $"\"{hizmet.Ad}\" hizmeti {sube.Ad} şubesinde verilmiyor.";

        // 3) Şube çalışma saatleri
        if (!sube.CalismaSaatleriIcindeMi(baslangic, bitis))
            return $"{sube.Ad} bu saatte açık değil.";

        // 4) Çalışanın uygunluk zaman dilimleri
        if (!calisan.UygunMu(baslangic, bitis))
            return $"{calisan.TamAd} bu saatte randevu vermiyor (uygunluk saatleri dışında).";

        // 5) Çalışan için çakışma kontrolü
        Randevu? calisanCakismasi = CakisanRandevu(r => r.CalisanId == calisan.Id, baslangic, bitis);
        if (calisanCakismasi is not null)
            return $"Çakışma: {calisan.TamAd} bu saatte dolu " +
                   $"({calisanCakismasi.Baslangic:HH:mm} - {calisanCakismasi.Bitis:HH:mm} arası başka bir randevusu var).";

        // 6) Hasta için çakışma kontrolü
        if (hasta is not null)
        {
            Randevu? hastaCakismasi = CakisanRandevu(r => r.HastaId == hasta.Id, baslangic, bitis);
            if (hastaCakismasi is not null)
                return $"Çakışma: {hasta.TamAd} için {hastaCakismasi.Baslangic:HH:mm} - {hastaCakismasi.Bitis:HH:mm} " +
                       $"arasında zaten bir randevu var ({hastaCakismasi.HizmetAdi}).";
        }

        return null; // tüm kurallar sağlandı
    }

    /// <summary>
    /// Verilen koşula uyan (ör. aynı çalışan) ve saati dolduran randevular arasında,
    /// verilen aralıkla çakışan ilk randevuyu döndürür. Reddedilen/iptal edilen randevular sayılmaz.
    /// Func&lt;Randevu, bool&gt;: "randevu alıp true/false döndüren fonksiyon" demektir.
    /// </summary>
    private Randevu? CakisanRandevu(Func<Randevu, bool> kosul, DateTime baslangic, DateTime bitis) =>
        _veri.Randevular.FirstOrDefault(r =>
            kosul(r) && r.Durum.SaatiDoldururMu() && r.CakisiyorMu(baslangic, bitis));

    /// <summary>
    /// Çalışanın ileri tarihli, hâlâ geçerli (onay bekleyen veya onaylanmış) randevuları.
    /// Pasife alma onayında kaç randevunun iptal edileceğini göstermek için de kullanılır.
    /// </summary>
    public List<Randevu> GelecekRandevulari(Calisan calisan) =>
        _veri.Randevular
            .Where(r => r.CalisanId == calisan.Id
                        && r.Baslangic > DateTime.Now
                        && r.GecisMumkunMu(RandevuDurumu.IptalEdildi))
            .OrderBy(r => r.Baslangic)
            .ToList();

    /// <summary>
    /// Çalışanı pasife alır ve pasife alındığı andan SONRAKİ (ileri tarihli) randevularını iptal eder.
    /// Geçmiş ve tamamlanmış randevulara dokunulmaz. İptal edilen randevuların listesini döndürür.
    /// İptal işlemi Randevu.IptalEt ile yapıldığı için yetki ve durum kuralları burada da geçerlidir.
    /// </summary>
    public List<Randevu> CalisaniPasifeAl(Calisan calisan, Kullanici islemYapan)
    {
        List<Randevu> iptalEdilecekler = GelecekRandevulari(calisan);
        string sebep = $"{calisan.TamAd} {DateTime.Now:dd.MM.yyyy} tarihinde pasife alındığı için iptal edildi.";

        // Önce yetki kontrolü: yetkisi yoksa hiçbir şey değişmeden hata fırlatılır.
        if (iptalEdilecekler.Any(r => !islemYapan.RandevuIptalEdebilirMi(r)))
            throw new UnauthorizedAccessException("Bu çalışanın randevularını iptal etme yetkiniz yok.");

        calisan.PasifeAl();
        foreach (Randevu randevu in iptalEdilecekler)
            randevu.IptalEt(islemYapan, sebep);

        return iptalEdilecekler;
    }

    /// <summary>
    /// Seçilen gün için, çalışanın uygunluk dilimlerini hizmet süresi kadar parçalara bölerek
    /// olası randevu saatlerini listeler. Her saatin müsait olup olmadığı da işaretlenir.
    /// Örnek: 09:00-12:00 dilimi, 20 dakikalık hizmet → 09:00, 09:20, 09:40 ... 11:40
    /// </summary>
    public List<SaatSecenegi> SaatSecenekleri(Hasta? hasta, Calisan calisan, SaglikHizmeti hizmet, DateOnly gun)
    {
        var secenekler = new List<SaatSecenegi>();
        DateTime gunBaslangici = gun.ToDateTime(TimeOnly.MinValue);

        foreach (UygunlukDilimi dilim in calisan.UygunlukDilimleri.Where(d => d.Gun == gun.DayOfWeek))
        {
            DateTime baslangic = gunBaslangici + dilim.Baslangic.ToTimeSpan();
            DateTime dilimSonu = gunBaslangici + dilim.Bitis.ToTimeSpan();

            while (baslangic.AddMinutes(hizmet.SureDakika) <= dilimSonu)
            {
                string? neden = KuralIhlali(hasta, calisan, hizmet, baslangic);
                secenekler.Add(new SaatSecenegi(baslangic, baslangic.AddMinutes(hizmet.SureDakika), neden));
                baslangic = baslangic.AddMinutes(hizmet.SureDakika);
            }
        }

        return secenekler;
    }
}
