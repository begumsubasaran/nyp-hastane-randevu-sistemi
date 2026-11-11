using RandevuSistemi.Models.Kullanicilar;
using RandevuSistemi.Models.Kurumlar;

namespace RandevuSistemi;

/// <summary>
/// GEÇİCİ TESTLER: Yazdığımız sınıfların doğru çalıştığını ekranda görmek için.
/// Giriş sistemi ve gerçek menüler eklendiğinde bu sınıf kaldırılacak.
/// </summary>
public static class GeciciTestler
{
    public static void OrnekKullanicilariGoster()
    {
        // Farklı türdeki nesneler aynı "Kullanici" listesinde tutulabilir (KALITIM sayesinde).
        List<Kullanici> kullanicilar =
        [
            new Hasta("Ayşe", "Yılmaz", "12345678901", "ayse", "sifre123", new DateOnly(1995, 4, 12)),
            new Doktor("Mehmet", "Kaya", "23456789012", "drmehmet", "sifre123", "Kardiyoloji", DoktorUnvani.UzmDr),
            new SaglikPersoneli("Zeynep", "Demir", "34567890123", "zeynep", "sifre123", PersonelGorevi.Hemsire),
            new Yonetici("Ali", "Çelik", "45678901234", "admin", "sifre123", YoneticiYetkisi.SistemYoneticisi)
        ];

        Console.WriteLine("\n--- Örnek kullanıcılar ---");
        foreach (Kullanici kullanici in kullanicilar)
        {
            // ÇOK BİÇİMLİLİK: Aynı metot çağrısı, nesnenin gerçek türüne göre
            // farklı sonuç üretir (Hasta yaşını, Doktor unvanını yazar).
            Console.WriteLine(kullanici.BilgiOzeti());
        }

        Console.WriteLine("\n--- Kapsülleme testleri ---");
        Kullanici ayse = kullanicilar[0];
        Console.WriteLine($"Doğru şifre kabul edildi mi? {ayse.SifreDogruMu("sifre123")}");
        Console.WriteLine($"Yanlış şifre kabul edildi mi? {ayse.SifreDogruMu("yanlis")}");

        try
        {
            // Geçersiz TC Kimlik No ile nesne oluşturmaya çalışıyoruz.
            _ = new Hasta("Test", "Kişi", "0123", "test", "sifre123", new DateOnly(2000, 1, 1));
        }
        catch (ArgumentException hata)
        {
            Console.WriteLine($"Geçersiz veri engellendi: {hata.Message}");
        }
    }

    public static void OrnekKurumYapisiniGoster()
    {
        // Çok şubeli bir kurum oluşturuyoruz.
        var kurum = new Kurum("Sakarya Sağlık Grubu", KurumTuru.Hastane);

        Sube merkez = kurum.SubeEkle("Merkez Şube", "Adapazarı / Sakarya");
        merkez.HaftaIciCalismaSaatiBelirle(new TimeOnly(8, 0), new TimeOnly(17, 0));
        merkez.CalismaSaatiBelirle(DayOfWeek.Saturday, new TimeOnly(9, 0), new TimeOnly(13, 0));
        merkez.BolumEkle("Kardiyoloji");
        merkez.BolumEkle("Dahiliye");
        merkez.BolumEkle("Radyoloji");
        merkez.BolumEkle("Laboratuvar");

        Sube serdivan = kurum.SubeEkle("Serdivan Şube", "Serdivan / Sakarya");
        serdivan.HaftaIciCalismaSaatiBelirle(new TimeOnly(9, 0), new TimeOnly(18, 0));
        serdivan.BolumEkle("Dahiliye");
        serdivan.BolumEkle("Göz Hastalıkları");

        Console.WriteLine("\n--- Örnek kurum ---");
        Console.WriteLine(kurum);
        Console.WriteLine($"Çok şubeli mi? {(kurum.CokSubeliMi ? "Evet" : "Hayır")}");

        foreach (Sube sube in kurum.Subeler)
        {
            Console.WriteLine($"\n  Şube: {sube}");
            Console.WriteLine("  Çalışma saatleri:");
            foreach (CalismaSaati saat in sube.CalismaSaatleri)
                Console.WriteLine($"    {saat}");
            Console.WriteLine($"  Bölümler: {string.Join(", ", sube.Bolumler)}");
        }

        Console.WriteLine("\n--- Çalışma saati kontrolleri (Merkez Şube) ---");
        DateTime[] zamanlar =
        [
            new DateTime(2026, 11, 16, 10, 0, 0), // Pazartesi 10:00 → açık
            new DateTime(2026, 11, 14, 14, 0, 0), // Cumartesi 14:00 → kapalı (13:00'te kapanıyor)
            new DateTime(2026, 11, 15, 10, 0, 0)  // Pazar 10:00 → kapalı
        ];
        foreach (DateTime zaman in zamanlar)
        {
            string gun = CalismaSaati.GunAdi(zaman.DayOfWeek);
            string durum = merkez.AcikMi(zaman) ? "AÇIK" : "KAPALI";
            Console.WriteLine($"  {zaman:dd.MM.yyyy} {gun} {zaman:HH:mm} → {durum}");
        }

        Console.WriteLine("\n--- Kural testleri ---");
        try
        {
            merkez.BolumEkle("kardiyoloji"); // aynı bölüm, küçük harfle
        }
        catch (InvalidOperationException hata)
        {
            Console.WriteLine($"Engellendi: {hata.Message}");
        }

        try
        {
            merkez.CalismaSaatiBelirle(DayOfWeek.Monday, new TimeOnly(17, 0), new TimeOnly(8, 0));
        }
        catch (ArgumentException hata)
        {
            Console.WriteLine($"Engellendi: {hata.Message}");
        }
    }
}
