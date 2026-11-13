using RandevuSistemi.Models.Kullanicilar;
using RandevuSistemi.Veri;

namespace RandevuSistemi.Arayuz.Formlar;

/// <summary>
/// Hastanın kendisini sisteme kaydettiği pencere (giriş ekranından açılır).
/// Ortak alanları KullaniciFormuTemeli'nden alır; sadece doğum tarihini ekler.
/// </summary>
public class HastaKayitFormu : KullaniciFormuTemeli
{
    private readonly DateTimePicker _dogumTarihi = new()
    {
        Format = DateTimePickerFormat.Short,
        Value = DateTime.Today.AddYears(-30), // önce değer, sonra üst sınır
        MaxDate = DateTime.Today
    };

    public HastaKayitFormu(UygulamaVerisi veri) : base("Hasta Kaydı", veri)
    {
        SatirEkle("Doğum tarihi", _dogumTarihi);
    }

    protected override Kullanici KullaniciOlustur() =>
        new Hasta(AdKutusu.Text, SoyadKutusu.Text, TcKutusu.Text, KullaniciAdiKutusu.Text, SifreKutusu.Text,
                  DateOnly.FromDateTime(_dogumTarihi.Value));
}
