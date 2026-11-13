using System.Text.Json;
using RandevuSistemi.Arayuz;
using RandevuSistemi.Arayuz.Formlar;
using RandevuSistemi.Models.Kullanicilar;
using RandevuSistemi.Veri;

namespace RandevuSistemi;

/// <summary>
/// Uygulamanın giriş noktası. Verileri yükler, giriş penceresini ve ardından ana pencereyi açar.
/// </summary>
internal static class Program
{
    [STAThread] // Windows Forms uygulamaları tek iş parçacıklı (STA) modda çalışmalıdır
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Veri dosyası programın çalıştığı klasörde durur:
        // ...\src\RandevuSistemi\bin\Debug\net9.0-windows\veri\randevu-sistemi.json
        string dosyaYolu = Path.Combine(AppContext.BaseDirectory, "veri", "randevu-sistemi.json");
        IVeriDeposu depo = new JsonVeriDeposu(dosyaYolu);

        UygulamaVerisi veri = VeriyiYukle(depo);

        // Giriş → Ana pencere → (Çıkış yapılırsa) tekrar Giriş ...
        // Ana pencere X ile kapatılırsa veya girişten vazgeçilirse program biter.
        while (true)
        {
            Kullanici? kullanici;
            using (var giris = new GirisFormu(veri))
            {
                if (giris.ShowDialog() != DialogResult.OK)
                    break;
                kullanici = giris.GirisYapan;
            }

            if (kullanici is null)
                break;

            var anaForm = new AnaForm(veri, depo, kullanici);
            Application.Run(anaForm);

            if (!anaForm.OturumKapatildi)
                break;
        }
    }

    /// <summary>
    /// Kayıtlı veri varsa onu, yoksa (ilk açılış) örnek verileri döndürür.
    /// Dosya bozuksa yedeğini alıp örnek verilerle devam eder.
    /// </summary>
    private static UygulamaVerisi VeriyiYukle(IVeriDeposu depo)
    {
        if (!depo.VeriVarMi)
            return OrnekVeriler.Olustur();

        try
        {
            return depo.Yukle();
        }
        catch (Exception hata) when (hata is JsonException or InvalidDataException or IOException
                                          or ArgumentException or InvalidOperationException)
        {
            string yedek = depo.Konum + ".bozuk";
            try
            {
                File.Copy(depo.Konum, yedek, overwrite: true);
            }
            catch (IOException)
            {
                // yedek alınamazsa da devam et
            }

            Tema.HataGoster(
                $"Veri dosyası okunamadı:\n{hata.Message}\n\n" +
                $"Dosyanın bir kopyası şuraya alındı:\n{yedek}\n\n" +
                "Program örnek verilerle açılacak.");
            return OrnekVeriler.Olustur();
        }
    }
}
