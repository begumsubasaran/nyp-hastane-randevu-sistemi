using RandevuSistemi.Models.Kurumlar;
using RandevuSistemi.Veri;

namespace RandevuSistemi.Arayuz;

/// <summary>
/// Açılır kutularda (ComboBox) şubeyi "Kurum / Şube" şeklinde göstermek için küçük yardımcı kayıt.
/// "record": sadece veri taşıyan, kısa yazılan bir sınıf türüdür.
/// </summary>
public sealed record SubeSecenegi(Kurum Kurum, Sube Sube)
{
    public override string ToString() => $"{Kurum.Ad} / {Sube.Ad}";

    /// <summary>Tüm kurumların tüm şubelerini seçenek listesi olarak verir.</summary>
    public static List<SubeSecenegi> Tumu(UygulamaVerisi veri) =>
        veri.Kurumlar
            .SelectMany(kurum => kurum.Subeler.Select(sube => new SubeSecenegi(kurum, sube)))
            .ToList();
}
