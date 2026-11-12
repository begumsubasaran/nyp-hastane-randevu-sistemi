using RandevuSistemi.Models.Hizmetler;

namespace RandevuSistemi.Models.Kurumlar;

/// <summary>Sağlık kuruluşunun türü.</summary>
public enum KurumTuru
{
    Hastane,
    Klinik,
    TipMerkezi
}

/// <summary>
/// Hastane / klinik gibi sağlık kuruluşu.
/// - Tek şubeli bir klinik: 1 şubesi olan kurum.
/// - Çok şubeli bir hastane grubu: birden fazla şubesi olan kurum.
/// Böylece iki senaryo da aynı sınıf yapısıyla desteklenir.
///
/// Kurum ile Sube arasında "sahiplik" (composition) ilişkisi vardır:
/// şubeler kurum üzerinden oluşturulur ve kurumun listesinde tutulur.
/// </summary>
public class Kurum
{
    private readonly List<Sube> _subeler = new();
    private string _ad = string.Empty;

    public Kurum(string ad, KurumTuru tur)
    {
        Ad = ad;
        Tur = tur;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Ad
    {
        get => _ad;
        set => _ad = Dogrulama.BosOlamaz(value, "Kurum adı");
    }

    public KurumTuru Tur { get; set; }

    public string TurAdi => Tur switch
    {
        KurumTuru.Hastane => "Hastane",
        KurumTuru.Klinik => "Klinik",
        KurumTuru.TipMerkezi => "Tıp Merkezi",
        _ => Tur.ToString()
    };

    public IReadOnlyList<Sube> Subeler => _subeler.AsReadOnly();

    public bool CokSubeliMi => _subeler.Count > 1;

    /// <summary>Kuruma yeni şube ekler. Aynı isimde şube varsa hata verir.</summary>
    public Sube SubeEkle(string ad, string adres)
    {
        var sube = new Sube(Id, ad, adres);
        bool ayniIsimVar = _subeler.Any(s =>
            string.Equals(s.Ad, sube.Ad, StringComparison.CurrentCultureIgnoreCase));
        if (ayniIsimVar)
            throw new InvalidOperationException($"'{sube.Ad}' adında bir şube zaten var.");

        _subeler.Add(sube);
        return sube;
    }

    public bool SubeKaldir(Guid subeId) => _subeler.RemoveAll(s => s.Id == subeId) > 0;

    public Sube? SubeBul(Guid subeId) => _subeler.FirstOrDefault(s => s.Id == subeId);

    /// <summary>
    /// Kurumun tüm şubelerindeki hizmetler. Hizmetler şube düzeyinde tanımlanır,
    /// çünkü her şube farklı hizmet sunabilir ve fiyatları farklı olabilir.
    /// SelectMany: her şubenin hizmet listesini tek bir listede birleştirir.
    /// </summary>
    public IEnumerable<SaglikHizmeti> TumHizmetler => _subeler.SelectMany(s => s.Hizmetler);

    public override string ToString() => $"{Ad} [{TurAdi}] - {_subeler.Count} şube";
}
