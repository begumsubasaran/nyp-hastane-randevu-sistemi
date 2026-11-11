using RandevuSistemi.Models.Kullanicilar;
using RandevuSistemi.Models.Kurumlar;

namespace RandevuSistemi.Veri;

/// <summary>
/// Uygulama ilk açıldığında ekranların boş görünmemesi için örnek veriler.
/// JSON ile kalıcı kayıt eklendiğinde, veri dosyası yoksa bu veriler kullanılacak.
/// </summary>
public static class OrnekVeriler
{
    public static List<Kurum> Kurumlar()
    {
        // Çok şubeli hastane
        var hastane = new Kurum("Sakarya Sağlık Grubu", KurumTuru.Hastane);

        Sube merkez = hastane.SubeEkle("Merkez Şube", "Adapazarı / Sakarya");
        merkez.HaftaIciCalismaSaatiBelirle(new TimeOnly(8, 0), new TimeOnly(17, 0));
        merkez.CalismaSaatiBelirle(DayOfWeek.Saturday, new TimeOnly(9, 0), new TimeOnly(13, 0));
        merkez.BolumEkle("Kardiyoloji");
        merkez.BolumEkle("Dahiliye");
        merkez.BolumEkle("Radyoloji");
        merkez.BolumEkle("Laboratuvar");

        Sube serdivan = hastane.SubeEkle("Serdivan Şube", "Serdivan / Sakarya");
        serdivan.HaftaIciCalismaSaatiBelirle(new TimeOnly(9, 0), new TimeOnly(18, 0));
        serdivan.BolumEkle("Dahiliye");
        serdivan.BolumEkle("Göz Hastalıkları");

        // Tek şubeli klinik
        var klinik = new Kurum("Erenler Aile Kliniği", KurumTuru.Klinik);
        Sube klinikSube = klinik.SubeEkle("Erenler", "Erenler / Sakarya");
        klinikSube.HaftaIciCalismaSaatiBelirle(new TimeOnly(9, 0), new TimeOnly(17, 30));
        klinikSube.BolumEkle("Genel Muayene");

        return [hastane, klinik];
    }

    public static List<Kullanici> Kullanicilar()
    {
        var ayse = new Hasta("Ayşe", "Yılmaz", "12345678901", "ayse", "sifre123", new DateOnly(1995, 4, 12))
        {
            Telefon = "0532 111 22 33",
            Eposta = "ayse@ornek.com"
        };
        var can = new Hasta("Can", "Öztürk", "56789012345", "can", "sifre123", new DateOnly(1988, 9, 3));

        return
        [
            ayse,
            can,
            new Doktor("Mehmet", "Kaya", "23456789012", "drmehmet", "sifre123", "Kardiyoloji", DoktorUnvani.UzmDr),
            new Doktor("Elif", "Arslan", "67890123456", "drelif", "sifre123", "Dahiliye", DoktorUnvani.DocDr),
            new SaglikPersoneli("Zeynep", "Demir", "34567890123", "zeynep", "sifre123", PersonelGorevi.Hemsire),
            new SaglikPersoneli("Burak", "Şahin", "78901234567", "burak", "sifre123", PersonelGorevi.RadyolojiTeknisyeni),
            new Yonetici("Ali", "Çelik", "45678901234", "admin", "sifre123", YoneticiYetkisi.SistemYoneticisi)
        ];
    }
}
