using System.Text;

namespace RandevuSistemi;

/// <summary>
/// Uygulamanın giriş noktası.
/// Giriş ve kayıt seçenekleri ilerleyen adımlarda gerçek işlevlerle doldurulacak.
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
            Console.WriteLine("4) Örnek kurum yapısını göster (geçici test)");
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
                    GeciciTestler.OrnekKullanicilariGoster();
                    Bekle();
                    break;
                case "4":
                    GeciciTestler.OrnekKurumYapisiniGoster();
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
