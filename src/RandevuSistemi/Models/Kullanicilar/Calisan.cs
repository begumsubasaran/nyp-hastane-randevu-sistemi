using RandevuSistemi.Models.Hizmetler;
using RandevuSistemi.Models.Kurumlar;
using RandevuSistemi.Models.Randevular;

namespace RandevuSistemi.Models.Kullanicilar;

/// <summary>
/// Kurumda çalışan kişilerin (Doktor, Sağlık Personeli) ortak SOYUT sınıfı.
/// Çok seviyeli kalıtım örneği: Kullanici → Calisan → Doktor
///
/// Randevu "çalışan + hizmet" seçilerek alınacağı için çalıştığı şube, sunduğu hizmetler
/// ve uygunluk zaman dilimleri burada (ortak üst sınıfta) tutulur.
/// </summary>
public abstract class Calisan : Kullanici
{
    // Hizmetlerin kendisi değil, kimlikleri (Id) tutulur: hizmetin asıl sahibi Sube'dir.
    // HashSet aynı Id'nin iki kez eklenmesine izin vermez.
    private readonly HashSet<Guid> _hizmetIdleri = new();
    private readonly List<UygunlukDilimi> _uygunlukDilimleri = new();

    protected Calisan(string ad, string soyad, string tcKimlikNo, string kullaniciAdi, string sifre)
        : base(ad, soyad, tcKimlikNo, kullaniciAdi, sifre)
    {
    }

    /// <summary>İşten ayrılan çalışan silinmez, pasife alınır (geçmiş randevular korunur).</summary>
    public bool AktifMi { get; private set; } = true;

    public void PasifeAl() => AktifMi = false;
    public void AktifEt() => AktifMi = true;

    /// <summary>
    /// Her çalışan türü unvanını kendisi belirler.
    /// Doktor: "Uzm. Dr. (Kardiyoloji)", Sağlık Personeli: "Hemşire" gibi.
    /// </summary>
    public abstract string Unvan { get; }

    // ---------------- Şube ----------------

    /// <summary>Çalışanın görev yaptığı şube. Henüz atanmadıysa null.</summary>
    public Guid? SubeId { get; private set; }

    /// <summary>
    /// Çalışanı bir şubeye atar. Yeni şubede geçerli olmayan bilgiler temizlenir:
    /// - o şubede bulunmayan hizmetler çıkarılır,
    /// - şubenin çalışma saatleri dışında kalan uygunluk dilimleri silinir.
    /// </summary>
    public void SubeyeAta(Sube sube)
    {
        SubeId = sube.Id;
        _hizmetIdleri.RemoveWhere(hizmetId => sube.HizmetBul(hizmetId) is null);
        _uygunlukDilimleri.RemoveAll(d => !sube.CalismaSaatleriIcindeMi(d.Gun, d.Baslangic, d.Bitis));
    }

    // ---------------- Sunduğu hizmetler ----------------

    public IReadOnlyCollection<Guid> HizmetIdleri => _hizmetIdleri;

    /// <summary>Hizmeti çalışanın sunduğu hizmetlere ekler. Zaten varsa false döner.</summary>
    public bool HizmetEkle(SaglikHizmeti hizmet) => _hizmetIdleri.Add(hizmet.Id);

    public bool HizmetCikar(Guid hizmetId) => _hizmetIdleri.Remove(hizmetId);

    public bool HizmetSunuyorMu(Guid hizmetId) => _hizmetIdleri.Contains(hizmetId);

    // ---------------- Uygunluk zaman dilimleri ----------------

    /// <summary>Haftalık uygunluk dilimleri (Pazartesiden Pazara, saate göre sıralı).</summary>
    public IReadOnlyList<UygunlukDilimi> UygunlukDilimleri => _uygunlukDilimleri.AsReadOnly();

    /// <summary>
    /// Yeni uygunluk dilimi ekler. Kurallar:
    /// 1) Dilim çalışanın kendi şubesi için tanımlanır,
    /// 2) Şubenin o günkü çalışma saatleri içinde olmalı,
    /// 3) Aynı gündeki başka bir dilimle çakışmamalı.
    /// </summary>
    public UygunlukDilimi UygunlukEkle(Sube sube, DayOfWeek gun, TimeOnly baslangic, TimeOnly bitis)
    {
        UygunlukDilimi dilim = YeniDilimiDogrula(sube, gun, baslangic, bitis, haric: null);
        _uygunlukDilimleri.Add(dilim);
        DilimleriSirala();
        return dilim;
    }

    /// <summary>
    /// Mevcut bir dilimi yeni gün/saatlerle değiştirir (ör. Pazartesi 09:00-12:00 → 08:00-12:00).
    /// Ekleme ile aynı kurallar geçerlidir; çakışma kontrolünde dilimin eski hali dikkate alınmaz.
    /// Kurallara uymazsa hata fırlatılır ve eski dilim olduğu gibi kalır.
    /// </summary>
    public UygunlukDilimi UygunlukGuncelle(UygunlukDilimi eskiDilim, Sube sube, DayOfWeek gun,
                                           TimeOnly baslangic, TimeOnly bitis)
    {
        int sira = _uygunlukDilimleri.IndexOf(eskiDilim);
        if (sira < 0)
            throw new InvalidOperationException("Güncellenecek zaman dilimi bulunamadı.");

        bool degisiklikYok = eskiDilim.Gun == gun && eskiDilim.Baslangic == baslangic && eskiDilim.Bitis == bitis;
        if (degisiklikYok)
            throw new InvalidOperationException(
                "Gün ve saatler mevcut dilimle aynı. Güncellemek için en az birini değiştirin.");

        UygunlukDilimi yeniDilim = YeniDilimiDogrula(sube, gun, baslangic, bitis, haric: eskiDilim);
        _uygunlukDilimleri[sira] = yeniDilim; // UygunlukDilimi değişmez olduğu için yenisiyle değiştiriyoruz
        DilimleriSirala();
        return yeniDilim;
    }

    /// <summary>
    /// Ekleme ve güncellemede ortak kuralları kontrol eder, geçerliyse yeni dilim nesnesini döndürür.
    /// haric: çakışma kontrolünde yok sayılacak dilim (güncellenen dilimin eski hali).
    /// </summary>
    private UygunlukDilimi YeniDilimiDogrula(Sube sube, DayOfWeek gun, TimeOnly baslangic, TimeOnly bitis,
                                            UygunlukDilimi? haric)
    {
        if (SubeId != sube.Id)
            throw new InvalidOperationException("Uygunluk saati sadece çalışanın kendi şubesi için tanımlanabilir.");

        var dilim = new UygunlukDilimi(gun, baslangic, bitis); // saatler hatalıysa burada hata fırlar

        if (!sube.CalismaSaatleriIcindeMi(gun, baslangic, bitis))
        {
            CalismaSaati? saat = sube.CalismaSaatiGetir(gun);
            string mesaj = saat is null
                ? $"{sube.Ad}, {CalismaSaati.GunAdi(gun)} günleri kapalı."
                : $"Uygunluk saati şubenin çalışma saatleri ({saat.Acilis:HH:mm} - {saat.Kapanis:HH:mm}) içinde olmalı.";
            throw new InvalidOperationException(mesaj);
        }

        UygunlukDilimi? cakisan = _uygunlukDilimleri.FirstOrDefault(d =>
            !ReferenceEquals(d, haric) && d.CakisiyorMu(dilim));
        if (cakisan is not null)
            throw new InvalidOperationException($"Bu dilim, mevcut \"{cakisan}\" dilimiyle çakışıyor.");

        return dilim;
    }

    /// <summary>Dilimleri önce güne (Pazartesi → Pazar), sonra başlangıç saatine göre sıralar.</summary>
    private void DilimleriSirala() =>
        _uygunlukDilimleri.Sort((a, b) =>
        {
            int gunFarki = CalismaSaati.GunSirasi(a.Gun).CompareTo(CalismaSaati.GunSirasi(b.Gun));
            return gunFarki != 0 ? gunFarki : a.Baslangic.CompareTo(b.Baslangic);
        });

    public bool UygunlukKaldir(UygunlukDilimi dilim) => _uygunlukDilimleri.Remove(dilim);

    /// <summary>
    /// Çalışan verilen tarih/saat aralığında uygun mu?
    /// Aralık, o günün uygunluk dilimlerinden birinin TAMAMEN içinde olmalı.
    /// Randevu oluşturulurken bu kontrol kullanılacak.
    /// </summary>
    public bool UygunMu(DateTime baslangic, DateTime bitis)
    {
        if (baslangic.Date != bitis.Date)
            return false;

        TimeOnly bas = TimeOnly.FromDateTime(baslangic);
        TimeOnly bit = TimeOnly.FromDateTime(bitis);
        return _uygunlukDilimleri.Any(d => d.Gun == baslangic.DayOfWeek && d.KapsarMi(bas, bit));
    }

    // ---------------- Randevu yetkileri (Kullanici'daki virtual metotların override'ı) ----------------

    /// <summary>Doktor/personel sadece KENDİSİNE alınmış randevuları onaylayabilir, reddedebilir, tamamlayabilir.</summary>
    public override bool RandevuOnaylayabilirMi(Randevu randevu) => randevu.CalisanId == Id;

    /// <summary>Doktor/personel kendisine alınmış randevuyu iptal edebilir.</summary>
    public override bool RandevuIptalEdebilirMi(Randevu randevu) => randevu.CalisanId == Id;

    /// <summary>Doktor/personel sadece kendisine alınmış randevuları görür.</summary>
    public override bool RandevuyuGorebilirMi(Randevu randevu) => randevu.CalisanId == Id;

    public override string BilgiOzeti() =>
        $"{base.BilgiOzeti()} - {Unvan}{(AktifMi ? "" : " (pasif)")}";
}
