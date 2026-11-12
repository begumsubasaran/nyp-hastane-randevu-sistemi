using RandevuSistemi.Models.Kullanicilar;
using RandevuSistemi.Models.Kurumlar;

namespace RandevuSistemi.Veri;

/// <summary>
/// Uygulamadaki tüm verileri bir arada tutan sınıf.
/// Ekranlar verilere buradan ulaşır. İleride JSON dosyasına bu nesne kaydedilecek.
/// </summary>
public class UygulamaVerisi
{
    public List<Kurum> Kurumlar { get; } = new();
    public List<Kullanici> Kullanicilar { get; } = new();

    /// <summary>
    /// Kullanıcılar arasından sadece çalışanları (Doktor + Sağlık Personeli) verir.
    /// OfType&lt;Calisan&gt;(): listedeki nesnelerden türü Calisan (veya alt türü) olanları seçer.
    /// </summary>
    public IEnumerable<Calisan> Calisanlar => Kullanicilar.OfType<Calisan>();

    /// <summary>Şube kimliğinden şubeyi bulur (hangi kurumda olduğu fark etmez).</summary>
    public Sube? SubeBul(Guid? subeId)
    {
        if (subeId is null)
            return null;
        return Kurumlar.Select(k => k.SubeBul(subeId.Value)).FirstOrDefault(s => s is not null);
    }

    /// <summary>Bir şubenin bağlı olduğu kurumu bulur.</summary>
    public Kurum? SubeninKurumu(Guid? subeId)
    {
        if (subeId is null)
            return null;
        return Kurumlar.FirstOrDefault(k => k.SubeBul(subeId.Value) is not null);
    }
}
