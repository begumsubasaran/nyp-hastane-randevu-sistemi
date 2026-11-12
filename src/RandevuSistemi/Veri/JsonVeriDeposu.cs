using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using RandevuSistemi.Veri.Kayitlar;

namespace RandevuSistemi.Veri;

/// <summary>
/// IVeriDeposu arayüzünün JSON dosyası kullanan uygulaması.
/// Tüm veriler tek bir okunabilir JSON dosyasında saklanır.
/// </summary>
public class JsonVeriDeposu : IVeriDeposu
{
    private readonly string _dosyaYolu;

    private static readonly JsonSerializerOptions Ayarlar = new()
    {
        WriteIndented = true,                                    // okunabilir, girintili JSON
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,   // Türkçe karakterler olduğu gibi yazılsın
        Converters = { new JsonStringEnumConverter() }           // enum'lar sayı yerine isim olarak yazılsın
    };

    public JsonVeriDeposu(string dosyaYolu)
    {
        _dosyaYolu = dosyaYolu;
    }

    public bool VeriVarMi => File.Exists(_dosyaYolu);

    public string Konum => _dosyaYolu;

    public UygulamaVerisi Yukle()
    {
        string json = File.ReadAllText(_dosyaYolu);
        VeriKaydi kayit = JsonSerializer.Deserialize<VeriKaydi>(json, Ayarlar)
                          ?? throw new InvalidDataException("Veri dosyası boş veya okunamadı.");
        return VeriDonusturucu.VeriyeDonustur(kayit);
    }

    public void Kaydet(UygulamaVerisi veri)
    {
        string? klasor = Path.GetDirectoryName(_dosyaYolu);
        if (!string.IsNullOrEmpty(klasor))
            Directory.CreateDirectory(klasor);

        VeriKaydi kayit = VeriDonusturucu.KaydaDonustur(veri);
        string json = JsonSerializer.Serialize(kayit, Ayarlar);

        // Önce geçici dosyaya yazıp sonra asıl dosyanın yerine koyuyoruz.
        // Böylece yazma sırasında bir hata olursa eski dosya bozulmaz.
        string geciciDosya = _dosyaYolu + ".tmp";
        File.WriteAllText(geciciDosya, json);
        File.Move(geciciDosya, _dosyaYolu, overwrite: true);
    }
}
