using RandevuSistemi.Models.Hizmetler;
using RandevuSistemi.Models.Kullanicilar;
using RandevuSistemi.Models.Kurumlar;
using RandevuSistemi.Models.Randevular;

namespace RandevuSistemi.Veri;

/// <summary>
/// Uygulama ilk açıldığında ekranların boş görünmemesi için örnek veriler.
/// JSON ile kalıcı kayıt eklendiğinde, veri dosyası yoksa bu veriler kullanılacak.
/// </summary>
public static class OrnekVeriler
{
    public static UygulamaVerisi Olustur()
    {
        var veri = new UygulamaVerisi();

        // ================= Çok şubeli hastane =================
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

        // Her şubenin hizmetleri ve fiyatları ayrı tanımlanır.
        merkez.HizmetEkle("Genel Muayene", HizmetKategorisi.Muayene, 15, 750m);
        SaglikHizmeti kardiyolojiMuayene = merkez.HizmetEkle("Kardiyoloji Muayenesi", HizmetKategorisi.Muayene, 20, 1200m);
        SaglikHizmeti ekg = merkez.HizmetEkle("EKG", HizmetKategorisi.Tetkik, 15, 400m, "Elektrokardiyografi");
        SaglikHizmeti rontgen = merkez.HizmetEkle("Akciğer Röntgeni", HizmetKategorisi.Goruntuleme, 10, 600m);
        SaglikHizmeti mr = merkez.HizmetEkle("MR", HizmetKategorisi.Goruntuleme, 30, 3500m, "Manyetik rezonans görüntüleme");
        SaglikHizmeti kanTahlili = merkez.HizmetEkle("Kan Tahlili", HizmetKategorisi.Laboratuvar, 10, 350m);

        SaglikHizmeti serdivanMuayene = serdivan.HizmetEkle("Genel Muayene", HizmetKategorisi.Muayene, 15, 700m);
        serdivan.HizmetEkle("Göz Muayenesi", HizmetKategorisi.Muayene, 20, 900m);
        serdivan.HizmetEkle("Kan Tahlili", HizmetKategorisi.Laboratuvar, 10, 350m);

        // ================= Tek şubeli klinik =================
        var klinik = new Kurum("Erenler Aile Kliniği", KurumTuru.Klinik);
        Sube klinikSube = klinik.SubeEkle("Erenler", "Erenler / Sakarya");
        klinikSube.HaftaIciCalismaSaatiBelirle(new TimeOnly(9, 0), new TimeOnly(17, 30));
        klinikSube.BolumEkle("Genel Muayene");

        SaglikHizmeti klinikMuayene = klinikSube.HizmetEkle("Genel Muayene", HizmetKategorisi.Muayene, 20, 600m);
        SaglikHizmeti asi = klinikSube.HizmetEkle("Aşı Uygulaması", HizmetKategorisi.Tedavi, 10, 200m);

        veri.Kurumlar.Add(hastane);
        veri.Kurumlar.Add(klinik);

        // ================= Kullanıcılar =================
        var ayse = new Hasta("Ayşe", "Yılmaz", "12345678901", "ayse", "sifre123", new DateOnly(1995, 4, 12))
        {
            Telefon = "0532 111 22 33",
            Eposta = "ayse@ornek.com"
        };
        var can = new Hasta("Can", "Öztürk", "56789012345", "can", "sifre123", new DateOnly(1988, 9, 3));

        var mehmet = new Doktor("Mehmet", "Kaya", "23456789012", "drmehmet", "sifre123", "Kardiyoloji", DoktorUnvani.UzmDr);
        mehmet.SubeyeAta(merkez);
        mehmet.HizmetEkle(kardiyolojiMuayene);
        mehmet.HizmetEkle(ekg);
        mehmet.UygunlukEkle(merkez, DayOfWeek.Monday, Saat(9), Saat(12));
        mehmet.UygunlukEkle(merkez, DayOfWeek.Monday, Saat(13), Saat(16));
        mehmet.UygunlukEkle(merkez, DayOfWeek.Wednesday, Saat(9), Saat(12));
        mehmet.UygunlukEkle(merkez, DayOfWeek.Friday, Saat(13), Saat(17));

        var elif = new Doktor("Elif", "Arslan", "67890123456", "drelif", "sifre123", "Dahiliye", DoktorUnvani.DocDr);
        elif.SubeyeAta(serdivan);
        elif.HizmetEkle(serdivanMuayene);
        elif.UygunlukEkle(serdivan, DayOfWeek.Tuesday, Saat(9), Saat(12));
        elif.UygunlukEkle(serdivan, DayOfWeek.Thursday, Saat(13), Saat(17, 30));

        var hakan = new Doktor("Hakan", "Aydın", "89012345678", "drhakan", "sifre123", "Aile Hekimliği", DoktorUnvani.Dr);
        hakan.SubeyeAta(klinikSube);
        hakan.HizmetEkle(klinikMuayene);
        hakan.HizmetEkle(asi);
        foreach (DayOfWeek gun in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
        {
            hakan.UygunlukEkle(klinikSube, gun, Saat(9), Saat(12));
            hakan.UygunlukEkle(klinikSube, gun, Saat(13), Saat(17));
        }

        var zeynep = new SaglikPersoneli("Zeynep", "Demir", "34567890123", "zeynep", "sifre123", PersonelGorevi.Hemsire);
        zeynep.SubeyeAta(merkez);
        zeynep.HizmetEkle(kanTahlili);
        foreach (DayOfWeek gun in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
            zeynep.UygunlukEkle(merkez, gun, Saat(8), Saat(12));

        var burak = new SaglikPersoneli("Burak", "Şahin", "78901234567", "burak", "sifre123", PersonelGorevi.RadyolojiTeknisyeni);
        burak.SubeyeAta(merkez);
        burak.HizmetEkle(rontgen);
        burak.HizmetEkle(mr);
        burak.UygunlukEkle(merkez, DayOfWeek.Tuesday, Saat(9), Saat(16));
        burak.UygunlukEkle(merkez, DayOfWeek.Thursday, Saat(9), Saat(16));
        burak.UygunlukEkle(merkez, DayOfWeek.Saturday, Saat(9), Saat(13));

        var admin = new Yonetici("Ali", "Çelik", "45678901234", "admin", "sifre123", YoneticiYetkisi.SistemYoneticisi);

        veri.Kullanicilar.AddRange(new Kullanici[] { ayse, can, mehmet, elif, hakan, zeynep, burak, admin });

        // ================= Örnek randevular =================
        // Tarihler bugüne göre hesaplanır: "bir sonraki Pazartesi" gibi.
        // Böylece program hangi gün açılırsa açılsın randevular ileri tarihli ve uygunluk saatleri içinde olur.
        veri.Randevular.Add(new Randevu(ayse, mehmet, kardiyolojiMuayene, SonrakiGun(DayOfWeek.Monday, 9, 0)));
        veri.Randevular.Add(new Randevu(can, mehmet, ekg, SonrakiGun(DayOfWeek.Monday, 9, 20)));
        veri.Randevular.Add(new Randevu(ayse, elif, serdivanMuayene, SonrakiGun(DayOfWeek.Tuesday, 10, 0)));
        veri.Randevular.Add(new Randevu(can, burak, mr, SonrakiGun(DayOfWeek.Tuesday, 11, 0)));
        veri.Randevular.Add(new Randevu(ayse, zeynep, kanTahlili, SonrakiGun(DayOfWeek.Wednesday, 8, 30)));
        veri.Randevular.Add(new Randevu(can, hakan, asi, SonrakiGun(DayOfWeek.Thursday, 14, 0)));

        return veri;
    }

    /// <summary>
    /// Yarından itibaren verilen günün ilk tarihini, belirtilen saatle döndürür.
    /// Örnek: bugün Perşembe ise SonrakiGun(Monday, 9, 0) → gelecek Pazartesi 09:00
    /// </summary>
    private static DateTime SonrakiGun(DayOfWeek gun, int saat, int dakika)
    {
        DateTime tarih = DateTime.Today.AddDays(1);
        while (tarih.DayOfWeek != gun)
            tarih = tarih.AddDays(1);
        return tarih.AddHours(saat).AddMinutes(dakika);
    }

    /// <summary>Kısaltma: Saat(9) → 09:00, Saat(17, 30) → 17:30</summary>
    private static TimeOnly Saat(int saat, int dakika = 0) => new(saat, dakika);
}
