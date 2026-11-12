namespace RandevuSistemi.Models.Hizmetler;

/// <summary>Sağlık hizmetinin türü.</summary>
public enum HizmetKategorisi
{
    Muayene,
    Tetkik,
    Goruntuleme,
    Laboratuvar,
    Tedavi
}

public static class HizmetKategorisiUzantilari
{
    public static string GorunenAd(this HizmetKategorisi kategori) => kategori switch
    {
        HizmetKategorisi.Muayene => "Muayene",
        HizmetKategorisi.Tetkik => "Tetkik",
        HizmetKategorisi.Goruntuleme => "Görüntüleme",
        HizmetKategorisi.Laboratuvar => "Laboratuvar",
        HizmetKategorisi.Tedavi => "Tedavi",
        _ => kategori.ToString()
    };
}

/// <summary>
/// Kurumun sunduğu bir sağlık hizmeti (Genel Muayene, MR, Kan Tahlili vb.).
/// Her hizmetin bir SÜRESİ (randevu ne kadar sürer) ve ÜCRETİ vardır.
/// Hizmetler şube düzeyinde, Sube.HizmetEkle(...) ile tanımlanır.
/// </summary>
public class SaglikHizmeti
{
    public const int EnKisaSureDakika = 5;
    public const int EnUzunSureDakika = 480; // 8 saat

    private string _ad = string.Empty;
    private int _sureDakika;
    private decimal _ucret;

    public SaglikHizmeti(string ad, HizmetKategorisi kategori, int sureDakika, decimal ucret, string? aciklama = null)
    {
        Ad = ad;
        Kategori = kategori;
        SureDakika = sureDakika;
        Ucret = ucret;
        Aciklama = aciklama;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Ad
    {
        get => _ad;
        set => _ad = Dogrulama.BosOlamaz(value, "Hizmet adı");
    }

    public HizmetKategorisi Kategori { get; set; }

    /// <summary>Hizmetin dakika cinsinden süresi (5 - 480 dakika arası).</summary>
    public int SureDakika
    {
        get => _sureDakika;
        set
        {
            if (value < EnKisaSureDakika || value > EnUzunSureDakika)
                throw new ArgumentException(
                    $"Hizmet süresi {EnKisaSureDakika} ile {EnUzunSureDakika} dakika arasında olmalı.");
            _sureDakika = value;
        }
    }

    /// <summary>Para birimi için double yerine decimal kullanılır (yuvarlama hatası olmaz).</summary>
    public decimal Ucret
    {
        get => _ucret;
        set
        {
            if (value < 0)
                throw new ArgumentException("Ücret için negatif değer giremezsiniz.");
            _ucret = value;
        }
    }

    public string? Aciklama { get; set; }

    /// <summary>Süreyi TimeSpan olarak verir; randevu bitiş saatini hesaplarken kullanılacak.</summary>
    public TimeSpan Sure => TimeSpan.FromMinutes(SureDakika);

    public string UcretMetni => $"{Ucret:N2} ₺";

    public override string ToString() => $"{Ad} ({SureDakika} dk, {UcretMetni})";
}
