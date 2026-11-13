using RandevuSistemi.Models.Hizmetler;
using RandevuSistemi.Models.Kullanicilar;
using RandevuSistemi.Models.Kurumlar;
using RandevuSistemi.Models.Randevular;

namespace RandevuSistemi.Veri;

/// <summary>
/// Uygulamadaki tüm verileri bir arada tutan sınıf.
/// Ekranlar verilere buradan ulaşır. İleride JSON dosyasına bu nesne kaydedilecek.
/// </summary>
public class UygulamaVerisi
{
    public List<Kurum> Kurumlar { get; } = new();
    public List<Kullanici> Kullanicilar { get; } = new();
    public List<Randevu> Randevular { get; } = new();

    /// <summary>
    /// Kullanıcılar arasından sadece çalışanları (Doktor + Sağlık Personeli) verir.
    /// OfType&lt;Calisan&gt;(): listedeki nesnelerden türü Calisan (veya alt türü) olanları seçer.
    /// </summary>
    public IEnumerable<Calisan> Calisanlar => Kullanicilar.OfType<Calisan>();

    public IEnumerable<Hasta> Hastalar => Kullanicilar.OfType<Hasta>();

    /// <summary>
    /// Yeni kullanıcı ekler. Kullanıcı adı ve TC Kimlik No sistemde benzersiz olmalıdır.
    /// </summary>
    public void KullaniciEkle(Kullanici kullanici)
    {
        if (Kullanicilar.Any(k => k.KullaniciAdi == kullanici.KullaniciAdi))
            throw new InvalidOperationException($"\"{kullanici.KullaniciAdi}\" kullanıcı adı zaten kullanılıyor.");
        if (Kullanicilar.Any(k => k.TcKimlikNo == kullanici.TcKimlikNo))
            throw new InvalidOperationException("Bu TC Kimlik No ile kayıtlı bir kullanıcı zaten var.");

        Kullanicilar.Add(kullanici);
    }

    /// <summary>Yeni kurum ekler. Aynı isimde kurum olamaz.</summary>
    public void KurumEkle(Kurum kurum)
    {
        if (Kurumlar.Any(k => string.Equals(k.Ad, kurum.Ad, StringComparison.CurrentCultureIgnoreCase)))
            throw new InvalidOperationException($"\"{kurum.Ad}\" adında bir kurum zaten var.");

        Kurumlar.Add(kurum);
    }

    /// <summary>
    /// Kimliğe göre kullanıcı bulur. Generic (T) sayesinde istenen türde döner:
    /// KullaniciBul&lt;Hasta&gt;(id) → Hasta?, KullaniciBul&lt;Calisan&gt;(id) → Calisan?
    /// </summary>
    public T? KullaniciBul<T>(Guid id) where T : Kullanici =>
        Kullanicilar.OfType<T>().FirstOrDefault(k => k.Id == id);

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

    /// <summary>Hizmet kimliğinden hizmeti bulur (tüm kurum ve şubelerde arar).</summary>
    public SaglikHizmeti? HizmetBul(Guid hizmetId) =>
        Kurumlar.SelectMany(k => k.TumHizmetler).FirstOrDefault(h => h.Id == hizmetId);
}
