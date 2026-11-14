using RandevuSistemi.Models.Kurumlar;

namespace RandevuSistemi.Models.Randevular;

/// <summary>
/// Bir çalışanın haftalık olarak randevu verebildiği zaman aralığı.
/// Örnek: "Pazartesi 09:00 - 12:00". Her hafta tekrar eder.
/// Hastalar sadece bu aralıkların içine randevu alabilir.
///
/// DEĞİŞMEZ (immutable) sınıftır: oluşturulduktan sonra değerleri değişmez.
/// </summary>
public class UygunlukDilimi
{
    public const int EnKisaDakika = 15;

    public UygunlukDilimi(DayOfWeek gun, TimeOnly baslangic, TimeOnly bitis)
    {
        if (bitis <= baslangic)
            throw new ArgumentException("Bitiş saati başlangıç saatinden sonra olmalı.");
        if ((bitis - baslangic).TotalMinutes < EnKisaDakika)
            throw new ArgumentException($"Uygunluk dilimi en az {EnKisaDakika} dakika olmalı.");

        Gun = gun;
        Baslangic = baslangic;
        Bitis = bitis;
    }

    public DayOfWeek Gun { get; }
    public TimeOnly Baslangic { get; }
    public TimeOnly Bitis { get; }

    /// <summary>Verilen saat aralığı bu dilimin tamamen içinde mi?</summary>
    public bool KapsarMi(TimeOnly baslangic, TimeOnly bitis) =>
        baslangic < bitis && baslangic >= Baslangic && bitis <= Bitis;

    /// <summary>
    /// İki dilim çakışıyor mu? Aynı gündelerse ve zaman aralıkları kesişiyorsa çakışır.
    /// Uç uca eklenen dilimler (09:00-12:00 ve 12:00-15:00) çakışmaz.
    /// </summary>
    public bool CakisiyorMu(UygunlukDilimi diger) =>
        Gun == diger.Gun && Baslangic < diger.Bitis && diger.Baslangic < Bitis;

    /// <summary>
    /// Dilimi verilen saat aralığına KIRPAR (daraltır).
    /// Örnek: 09:00-16:00 dilimi, 09:20-17:00 aralığına kırpılınca 09:20-16:00 olur.
    /// Dilim zaten aralığın içindeyse kendisini, kırpılınca hiç (veya 15 dakikadan az) süre kalmıyorsa null döndürür.
    /// </summary>
    public UygunlukDilimi? AraligaKirp(TimeOnly acilis, TimeOnly kapanis)
    {
        TimeOnly yeniBaslangic = Baslangic > acilis ? Baslangic : acilis;   // ikisinden geç olanı
        TimeOnly yeniBitis = Bitis < kapanis ? Bitis : kapanis;             // ikisinden erken olanı

        if (yeniBaslangic == Baslangic && yeniBitis == Bitis)
            return this; // değişiklik yok

        if (yeniBitis <= yeniBaslangic || (yeniBitis - yeniBaslangic).TotalMinutes < EnKisaDakika)
            return null; // kırpınca anlamlı bir süre kalmıyor

        return new UygunlukDilimi(Gun, yeniBaslangic, yeniBitis);
    }

    /// <summary>
    /// Dilimi şubenin o günkü çalışma saatlerine kırpar. Şube o gün kapalıysa null döndürür.
    /// </summary>
    public UygunlukDilimi? SubeSaatlerineKirp(Sube sube)
    {
        CalismaSaati? saat = sube.CalismaSaatiGetir(Gun);
        return saat is null ? null : AraligaKirp(saat.Acilis, saat.Kapanis);
    }

    public override string ToString() =>
        $"{CalismaSaati.GunAdi(Gun)}  {Baslangic:HH:mm} - {Bitis:HH:mm}";
}
