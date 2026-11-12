namespace RandevuSistemi.Models.Randevular;

/// <summary>
/// Randevunun yaşam döngüsündeki durumu.
///
///   Beklemede ──► Onaylandi ──► Tamamlandi
///       │             │
///       ├──► Reddedildi
///       └──► IptalEdildi ◄──┘
///
/// Hasta randevu oluşturduğunda "Beklemede" olur; doktor/personel veya yönetici onaylar ya da reddeder.
/// </summary>
public enum RandevuDurumu
{
    Beklemede,
    Onaylandi,
    Reddedildi,
    IptalEdildi,
    Tamamlandi
}

public static class RandevuDurumuUzantilari
{
    public static string GorunenAd(this RandevuDurumu durum) => durum switch
    {
        RandevuDurumu.Beklemede => "Onay bekliyor",
        RandevuDurumu.Onaylandi => "Onaylandı",
        RandevuDurumu.Reddedildi => "Reddedildi",
        RandevuDurumu.IptalEdildi => "İptal edildi",
        RandevuDurumu.Tamamlandi => "Tamamlandı",
        _ => durum.ToString()
    };

    /// <summary>
    /// Bu durumdaki randevu çalışanın takviminde yer kaplar mı?
    /// Reddedilen ve iptal edilen randevular saati boşaltır; çakışma kontrolünde sayılmaz.
    /// </summary>
    public static bool SaatiDoldururMu(this RandevuDurumu durum) =>
        durum is RandevuDurumu.Beklemede or RandevuDurumu.Onaylandi or RandevuDurumu.Tamamlandi;
}
