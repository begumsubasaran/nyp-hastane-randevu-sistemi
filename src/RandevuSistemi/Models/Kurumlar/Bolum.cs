namespace RandevuSistemi.Models.Kurumlar;

/// <summary>
/// Şube içindeki bölüm (Kardiyoloji, Radyoloji, Laboratuvar vb.).
/// Bölümler Sube.BolumEkle(...) ile eklenir.
/// </summary>
public class Bolum
{
    private string _ad = string.Empty;

    public Bolum(string ad, string? aciklama = null)
    {
        Ad = ad;
        Aciklama = aciklama;
    }

    /// <summary>
    /// Benzersiz kimlik. "internal set": sadece bu proje içinden (JSON'dan yüklerken aynı kimliği
    /// geri vermek için) atanabilir; dışarıdan değiştirilemez.
    /// </summary>
    public Guid Id { get; internal set; } = Guid.NewGuid();

    public string Ad
    {
        get => _ad;
        set => _ad = Dogrulama.BosOlamaz(value, "Bölüm adı");
    }

    public string? Aciklama { get; set; }

    public override string ToString() => Ad;
}
