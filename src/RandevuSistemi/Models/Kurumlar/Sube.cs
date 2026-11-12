using RandevuSistemi.Models.Hizmetler;

namespace RandevuSistemi.Models.Kurumlar;

/// <summary>
/// Bir kurumun şubesi. Çalışma saatleri, bölümler ve sağlık hizmetleri şube bazında tanımlanır,
/// çünkü aynı kurumun farklı şubeleri farklı saatlerde çalışıp farklı hizmetler sunabilir.
///
/// KAPSÜLLEME: Listeler private tutulur. Dışarıya sadece okunabilir (IReadOnlyList)
/// halleri verilir; ekleme/çıkarma yalnızca kuralları kontrol eden metotlarla yapılabilir.
/// Örneğin dışarıdan sube.Bolumler.Add(...) yazılamaz.
/// </summary>
public class Sube
{
    private readonly List<CalismaSaati> _calismaSaatleri = new();
    private readonly List<Bolum> _bolumler = new();
    private readonly List<SaglikHizmeti> _hizmetler = new();
    private string _ad = string.Empty;
    private string _adres = string.Empty;

    /// <summary>Şubeler Kurum.SubeEkle(...) üzerinden oluşturulmalıdır.</summary>
    public Sube(Guid kurumId, string ad, string adres)
    {
        KurumId = kurumId;
        Ad = ad;
        Adres = adres;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>Şubenin bağlı olduğu kurumun kimliği.</summary>
    public Guid KurumId { get; private set; }

    public string Ad
    {
        get => _ad;
        set => _ad = Dogrulama.BosOlamaz(value, "Şube adı");
    }

    public string Adres
    {
        get => _adres;
        set => _adres = Dogrulama.BosOlamaz(value, "Adres");
    }

    public IReadOnlyList<CalismaSaati> CalismaSaatleri => _calismaSaatleri.AsReadOnly();
    public IReadOnlyList<Bolum> Bolumler => _bolumler.AsReadOnly();
    public IReadOnlyList<SaglikHizmeti> Hizmetler => _hizmetler.AsReadOnly();

    // ---------------- Çalışma saatleri ----------------

    /// <summary>Bir günün çalışma saatini belirler. O gün için önceden saat varsa üzerine yazar.</summary>
    public void CalismaSaatiBelirle(DayOfWeek gun, TimeOnly acilis, TimeOnly kapanis)
    {
        var yeniSaat = new CalismaSaati(gun, acilis, kapanis); // geçersizse burada hata fırlar
        _calismaSaatleri.RemoveAll(c => c.Gun == gun);
        _calismaSaatleri.Add(yeniSaat);
        _calismaSaatleri.Sort((a, b) => GunSirasi(a.Gun).CompareTo(GunSirasi(b.Gun)));
    }

    /// <summary>Pazartesi–Cuma için aynı çalışma saatini tek seferde belirler.</summary>
    public void HaftaIciCalismaSaatiBelirle(TimeOnly acilis, TimeOnly kapanis)
    {
        DayOfWeek[] haftaIci =
        [
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
        ];
        foreach (DayOfWeek gun in haftaIci)
            CalismaSaatiBelirle(gun, acilis, kapanis);
    }

    /// <summary>Şubeyi o gün kapalı yapar. Kaldırılacak bir kayıt varsa true döner.</summary>
    public bool CalismaGunuKaldir(DayOfWeek gun) => _calismaSaatleri.RemoveAll(c => c.Gun == gun) > 0;

    /// <summary>O günün çalışma saatini döndürür; şube o gün kapalıysa null döner.</summary>
    public CalismaSaati? CalismaSaatiGetir(DayOfWeek gun) => _calismaSaatleri.FirstOrDefault(c => c.Gun == gun);

    /// <summary>Şube verilen tarih ve saatte açık mı?</summary>
    public bool AcikMi(DateTime zaman)
    {
        CalismaSaati? saat = CalismaSaatiGetir(zaman.DayOfWeek);
        if (saat is null)
            return false;

        TimeOnly an = TimeOnly.FromDateTime(zaman);
        return an >= saat.Acilis && an < saat.Kapanis;
    }

    /// <summary>
    /// Bir zaman aralığı (ör. randevu) tamamen şubenin çalışma saatleri içinde mi?
    /// Randevu sistemi bu metodu kullanacak.
    /// </summary>
    public bool CalismaSaatleriIcindeMi(DateTime baslangic, DateTime bitis)
    {
        if (baslangic.Date != bitis.Date)
            return false; // gün aşan randevu kabul edilmez

        CalismaSaati? saat = CalismaSaatiGetir(baslangic.DayOfWeek);
        return saat is not null
               && saat.KapsarMi(TimeOnly.FromDateTime(baslangic), TimeOnly.FromDateTime(bitis));
    }

    /// <summary>Haftanın bir gününde, verilen saat aralığı şubenin çalışma saatleri içinde mi?</summary>
    public bool CalismaSaatleriIcindeMi(DayOfWeek gun, TimeOnly baslangic, TimeOnly bitis)
    {
        CalismaSaati? saat = CalismaSaatiGetir(gun);
        return saat is not null && saat.KapsarMi(baslangic, bitis);
    }

    private static int GunSirasi(DayOfWeek gun) => CalismaSaati.GunSirasi(gun);

    // ---------------- Bölümler ----------------

    /// <summary>Şubeye yeni bölüm ekler. Aynı isimde bölüm varsa hata verir.</summary>
    public Bolum BolumEkle(string ad, string? aciklama = null)
    {
        var bolum = new Bolum(ad, aciklama); // ad boşsa burada hata fırlar
        if (BolumBul(bolum.Ad) is not null)
            throw new InvalidOperationException($"'{bolum.Ad}' bölümü bu şubede zaten var.");

        _bolumler.Add(bolum);
        return bolum;
    }

    public bool BolumKaldir(Guid bolumId) => _bolumler.RemoveAll(b => b.Id == bolumId) > 0;

    /// <summary>İsme göre bölüm arar (büyük/küçük harf duyarsız).</summary>
    public Bolum? BolumBul(string ad) =>
        _bolumler.FirstOrDefault(b => string.Equals(b.Ad, ad.Trim(), StringComparison.CurrentCultureIgnoreCase));

    // ---------------- Sağlık hizmetleri ----------------

    /// <summary>Şubeye yeni hizmet ekler. Bu şubede aynı isimde hizmet varsa hata verir.</summary>
    public SaglikHizmeti HizmetEkle(string ad, HizmetKategorisi kategori, int sureDakika, decimal ucret,
                                    string? aciklama = null)
    {
        var hizmet = new SaglikHizmeti(ad, kategori, sureDakika, ucret, aciklama); // geçersizse hata fırlar
        bool ayniIsimVar = _hizmetler.Any(h =>
            string.Equals(h.Ad, hizmet.Ad, StringComparison.CurrentCultureIgnoreCase));
        if (ayniIsimVar)
            throw new InvalidOperationException($"'{hizmet.Ad}' hizmeti bu şubede zaten var.");

        _hizmetler.Add(hizmet);
        return hizmet;
    }

    public bool HizmetKaldir(Guid hizmetId) => _hizmetler.RemoveAll(h => h.Id == hizmetId) > 0;

    public SaglikHizmeti? HizmetBul(Guid hizmetId) => _hizmetler.FirstOrDefault(h => h.Id == hizmetId);

    public override string ToString() => $"{Ad} ({Adres})";
}
