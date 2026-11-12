using RandevuSistemi.Models.Hizmetler;
using RandevuSistemi.Models.Kurumlar;

namespace RandevuSistemi.Models.Kullanicilar;

/// <summary>
/// Kurumda çalışan kişilerin (Doktor, Sağlık Personeli) ortak SOYUT sınıfı.
/// Çok seviyeli kalıtım örneği: Kullanici → Calisan → Doktor
///
/// Randevu "çalışan + hizmet" seçilerek alınacağı için çalıştığı şube ve
/// sunduğu hizmetler burada (ortak üst sınıfta) tutulur.
/// </summary>
public abstract class Calisan : Kullanici
{
    // Hizmetlerin kendisi değil, kimlikleri (Id) tutulur: hizmetin asıl sahibi Kurum'dur.
    // HashSet aynı Id'nin iki kez eklenmesine izin vermez.
    private readonly HashSet<Guid> _hizmetIdleri = new();

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

    public void SubeyeAta(Sube sube) => SubeId = sube.Id;

    // ---------------- Sunduğu hizmetler ----------------

    public IReadOnlyCollection<Guid> HizmetIdleri => _hizmetIdleri;

    /// <summary>Hizmeti çalışanın sunduğu hizmetlere ekler. Zaten varsa false döner.</summary>
    public bool HizmetEkle(SaglikHizmeti hizmet) => _hizmetIdleri.Add(hizmet.Id);

    public bool HizmetCikar(Guid hizmetId) => _hizmetIdleri.Remove(hizmetId);

    public bool HizmetSunuyorMu(Guid hizmetId) => _hizmetIdleri.Contains(hizmetId);

    public override string BilgiOzeti() =>
        $"{base.BilgiOzeti()} - {Unvan}{(AktifMi ? "" : " (pasif)")}";
}
