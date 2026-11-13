using RandevuSistemi.Models.Kullanicilar;
using RandevuSistemi.Veri;

namespace RandevuSistemi.Arayuz.Formlar;

/// <summary>
/// Yöneticinin yeni doktor veya sağlık personeli kaydettiği pencere.
/// Ortak alanları KullaniciFormuTemeli'nden alır; türe özel alanları (uzmanlık, unvan, görev, şube) ekler.
/// Hizmetler ve uygunluk saatleri kayıttan sonra Çalışanlar ekranından tanımlanır.
/// </summary>
public class CalisanFormu : KullaniciFormuTemeli
{
    // Sıraları enum'larla aynı: DoktorUnvani (Dr, UzmDr, DocDr, ProfDr), PersonelGorevi (Hemsire, Ebe, ...)
    private static readonly string[] Unvanlar = ["Dr.", "Uzm. Dr.", "Doç. Dr.", "Prof. Dr."];
    private static readonly string[] Gorevler = ["Hemşire", "Ebe", "Laborant", "Radyoloji Teknisyeni", "Fizyoterapist"];

    private readonly ComboBox _turKutusu = SecimKutusu(["Doktor", "Sağlık Personeli"]);
    private readonly TextBox _uzmanlikKutusu = new();
    private readonly ComboBox _unvanKutusu = SecimKutusu(Unvanlar);
    private readonly ComboBox _gorevKutusu = SecimKutusu(Gorevler);
    private readonly ComboBox _subeKutusu;

    public CalisanFormu(UygulamaVerisi veri) : base("Yeni Çalışan", veri)
    {
        _subeKutusu = SecimKutusu(SubeSecenegi.Tumu(veri));

        SatirEkle("Çalışan türü", _turKutusu);
        SatirEkle("Uzmanlık alanı", _uzmanlikKutusu);
        SatirEkle("Akademik unvan", _unvanKutusu);
        SatirEkle("Görev", _gorevKutusu);
        SatirEkle("Şube", _subeKutusu);

        _turKutusu.SelectedIndexChanged += (_, _) => AlanlariAyarla();
        AlanlariAyarla();
    }

    private bool DoktorMu => _turKutusu.SelectedIndex == 0;

    /// <summary>Doktor seçiliyse uzmanlık/unvan, personel seçiliyse görev alanı açık olur.</summary>
    private void AlanlariAyarla()
    {
        _uzmanlikKutusu.Enabled = DoktorMu;
        _unvanKutusu.Enabled = DoktorMu;
        _gorevKutusu.Enabled = !DoktorMu;
    }

    protected override Kullanici KullaniciOlustur()
    {
        if (_subeKutusu.SelectedItem is not SubeSecenegi secenek)
            throw new ArgumentException("Lütfen çalışanın şubesini seçin.");

        // Seçilen türe göre farklı alt sınıftan nesne oluşturuluyor; ikisi de bir Calisan'dır.
        Calisan calisan = DoktorMu
            ? new Doktor(AdKutusu.Text, SoyadKutusu.Text, TcKutusu.Text, KullaniciAdiKutusu.Text, SifreKutusu.Text,
                         _uzmanlikKutusu.Text, (DoktorUnvani)_unvanKutusu.SelectedIndex)
            : new SaglikPersoneli(AdKutusu.Text, SoyadKutusu.Text, TcKutusu.Text, KullaniciAdiKutusu.Text, SifreKutusu.Text,
                                  (PersonelGorevi)_gorevKutusu.SelectedIndex);

        calisan.SubeyeAta(secenek.Sube);
        return calisan;
    }
}
