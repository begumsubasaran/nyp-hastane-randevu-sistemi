namespace RandevuSistemi.Models.Kurumlar;

/// <summary>
/// Bir şubenin haftanın belirli bir günündeki çalışma saatleri.
/// DEĞİŞMEZ (immutable) bir sınıftır: oluşturulduktan sonra değerleri değişmez.
/// Saat değiştirmek için Sube.CalismaSaatiBelirle(...) ile yenisi verilir.
/// </summary>
public class CalismaSaati
{
    public CalismaSaati(DayOfWeek gun, TimeOnly acilis, TimeOnly kapanis)
    {
        if (kapanis <= acilis)
            throw new ArgumentException("Kapanış saati açılış saatinden sonra olmalı.");

        Gun = gun;
        Acilis = acilis;
        Kapanis = kapanis;
    }

    // Sadece "get" var, "set" yok → değerler yalnızca constructor'da atanabilir.
    public DayOfWeek Gun { get; }
    public TimeOnly Acilis { get; }
    public TimeOnly Kapanis { get; }

    /// <summary>
    /// Verilen saat aralığı tamamen çalışma saatleri içinde mi?
    /// İleride randevu oluştururken bu kontrol kullanılacak.
    /// </summary>
    public bool KapsarMi(TimeOnly baslangic, TimeOnly bitis) =>
        baslangic < bitis && baslangic >= Acilis && bitis <= Kapanis;

    public override string ToString() => $"{GunAdi(Gun)}: {Acilis:HH:mm} - {Kapanis:HH:mm}";

    /// <summary>DayOfWeek değerini Türkçe gün adına çevirir.</summary>
    public static string GunAdi(DayOfWeek gun) => gun switch
    {
        DayOfWeek.Monday => "Pazartesi",
        DayOfWeek.Tuesday => "Salı",
        DayOfWeek.Wednesday => "Çarşamba",
        DayOfWeek.Thursday => "Perşembe",
        DayOfWeek.Friday => "Cuma",
        DayOfWeek.Saturday => "Cumartesi",
        DayOfWeek.Sunday => "Pazar",
        _ => gun.ToString()
    };
}
