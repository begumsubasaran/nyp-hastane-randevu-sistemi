namespace RandevuSistemi.Veri;

/// <summary>
/// Verilerin nereye ve nasıl kaydedileceğini soyutlayan ARAYÜZ (interface).
///
/// Uygulamanın geri kalanı sadece bu arayüzü bilir; verinin JSON dosyasında mı,
/// bir veritabanında mı tutulduğunu bilmez. Yarın SQLite'a geçmek istersek
/// sadece bu arayüzü uygulayan yeni bir sınıf yazmamız yeterli olur
/// (SOLID'deki "Dependency Inversion" ilkesi).
/// </summary>
public interface IVeriDeposu
{
    /// <summary>Daha önce kaydedilmiş veri var mı?</summary>
    bool VeriVarMi { get; }

    /// <summary>Kaydedilmiş verileri okuyup uygulama nesnelerine dönüştürür.</summary>
    UygulamaVerisi Yukle();

    /// <summary>Uygulamadaki tüm verileri kalıcı olarak kaydeder.</summary>
    void Kaydet(UygulamaVerisi veri);

    /// <summary>Kullanıcıya gösterilebilecek kayıt yeri (ör. dosya yolu).</summary>
    string Konum { get; }
}
