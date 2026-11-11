using System.Text;
using RandevuSistemi.Models.Kullanicilar;

namespace RandevuSistemi;

/// <summary>
/// Uygulamanın giriş noktası.
/// Giriş ve kayıt seçenekleri ilerleyen günlerde gerçek işlevlerle doldurulacak.
/// </summary>
public static class Program
{
    public static void Main()
    {
        // Windows konsolunda Türkçe karakterlerin (ç, ğ, ı, ö, ş, ü) düzgün görünmesi için
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        bool calisiyor = true;
        while (calisiyor)
        {
            BaslikYazdir();
            Console.WriteLine("1) Giriş yap");
            Console.WriteLine("2) Hasta olarak kayıt ol");
            Console.WriteLine("3) Örnek kullanıcıları göster (geçici test)");
            Console.WriteLine("0) Çıkış");
            Console.Write("\nSeçiminiz: ");

            string? secim = Console.ReadLine();
            switch (secim)
            {
                case "1":
                case "2":
                    Console.WriteLine("\nBu özellik ilerleyen adımlarda eklenecek.");
                    Bekle();
                    break;
                case "3":
                    OrnekKullanicilariGoster();
                    Bekle();
                    break;
                case "0":
                    calisiyor = false;
                    break;
                default:
                    Console.WriteLine("\nGeçersiz seçim, tekrar deneyin.");
                    Bekle();
                    break;
            }
        }

        Console.WriteLine("\nGüle güle!");
    }

    /// <summary>
    /// GEÇİCİ TEST: Kullanıcı sınıflarının doğru çalıştığını görmek için.
    /// Giriş sistemi eklendiğinde (10. gün) bu menü seçeneği kaldırılacak.
    /// </summary>
    private static void OrnekKullanicilariGoster()
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

    private static void BaslikYazdir()
    {
        Console.Clear();
        Console.WriteLine("========================================");
        Console.WriteLine("   HASTANE / KLİNİK RANDEVU SİSTEMİ");
        Console.WriteLine("========================================\n");
    }

    private static void Bekle()
    {
        Console.WriteLine("\nDevam etmek için bir tuşa basın...");
        Console.ReadKey(true);
    }
}
