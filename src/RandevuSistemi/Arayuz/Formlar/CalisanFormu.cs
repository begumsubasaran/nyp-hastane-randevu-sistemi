using RandevuSistemi.Models.Kullanicilar;
using RandevuSistemi.Models.Kurumlar;
using RandevuSistemi.Veri;

namespace RandevuSistemi.Arayuz.Formlar;

/// <summary>
/// Yöneticinin yeni doktor veya sağlık personeli kaydettiği pencere.
/// Ortak alanları KullaniciFormuTemeli'nden alır; türe özel alanları (şube, uzmanlık, unvan, görev) ekler.
/// Doktorun uzmanlık alanı, seçilen şubenin BÖLÜMLERİ arasından seçilir.
/// Hizmetler ve uygunluk saatleri kayıttan sonra Çalışanlar ekranından tanımlanır.
/// </summary>
public class CalisanFormu : KullaniciFormuTemeli
{
    // Sıraları enum'larla aynı: DoktorUnvani (Dr, UzmDr, DocDr, ProfDr), PersonelGorevi (Hemsire, Ebe, ...)
    private static readonly string[] Unvanlar = ["Dr.", "Uzm. Dr.", "Doç. Dr.", "Prof. Dr."];
    private static readonly string[] Gorevler = ["Hemşire", "Ebe", "Laborant", "Radyoloji Teknisyeni", "Fizyoterapist"];

    private readonly ComboBox _turKutusu = SecimKutusu(["Doktor", "Sağlık Personeli"]);
    private readonly ComboBox _bolumKutusu = SecimKutusu([]);
    private readonly ComboBox _unvanKutusu = SecimKutusu(Unvanlar);
    private readonly ComboBox _gorevKutusu = SecimKutusu(Gorevler);
    private readonly ComboBox _subeKutusu;

    public CalisanFormu(UygulamaVerisi veri) : base("Yeni Çalışan", veri)
    {
        _subeKutusu = SecimKutusu(SubeSecenegi.Tumu(veri));

        // Şube önce seçilir; uzmanlık alanı listesi o şubenin bölümlerinden dolar.
        SatirEkle("Çalışan türü", _turKutusu);
        SatirEkle("Şube", _subeKutusu);
        SatirEkle("Bölüm", _bolumKutusu);
        SatirEkle("Akademik unvan", _unvanKutusu);
        SatirEkle("Görev", _gorevKutusu);

        _turKutusu.SelectedIndexChanged += (_, _) => AlanlariAyarla();
        _subeKutusu.SelectedIndexChanged += (_, _) => BolumleriDoldur();
        BolumleriDoldur();
        AlanlariAyarla();
    }

    private bool DoktorMu => _turKutusu.SelectedIndex == 0;

    /// <summary>Seçilen şubenin bölümlerini uzmanlık alanı listesine doldurur.</summary>
    private void BolumleriDoldur()
    {
        _bolumKutusu.Items.Clear();
        if (_subeKutusu.SelectedItem is SubeSecenegi secenek)
        {
            foreach (Bolum bolum in secenek.Sube.Bolumler)
                _bolumKutusu.Items.Add(bolum); // ekranda bolum.ToString() yani bölüm adı görünür
        }

        if (_bolumKutusu.Items.Count > 0)
            _bolumKutusu.SelectedIndex = 0;
    }

    /// <summary>Doktor seçiliyse uzmanlık/unvan, personel seçiliyse görev alanı açık olur.</summary>
    private void AlanlariAyarla()
    {
        _bolumKutusu.Enabled = DoktorMu;
        _unvanKutusu.Enabled = DoktorMu;
        _gorevKutusu.Enabled = !DoktorMu;
    }

    protected override Kullanici KullaniciOlustur()
    {
        if (_subeKutusu.SelectedItem is not SubeSecenegi secenek)
            throw new ArgumentException("Lütfen çalışanın şubesini seçin.");

        Calisan calisan;
        if (DoktorMu)
        {
            if (_bolumKutusu.SelectedItem is not Bolum bolum)
                throw new ArgumentException(
                    $"{secenek.Sube.Ad} şubesinde tanımlı bölüm yok. Önce Kurumlar ekranından bölüm ekleyin.");

            calisan = new Doktor(AdKutusu.Text, SoyadKutusu.Text, TcKutusu.Text, KullaniciAdiKutusu.Text, SifreKutusu.Text,
                                 bolum.Ad, (DoktorUnvani)_unvanKutusu.SelectedIndex);
        }
        else
        {
            calisan = new SaglikPersoneli(AdKutusu.Text, SoyadKutusu.Text, TcKutusu.Text, KullaniciAdiKutusu.Text, SifreKutusu.Text,
                                          (PersonelGorevi)_gorevKutusu.SelectedIndex);
        }

        calisan.SubeyeAta(secenek.Sube);
        return calisan;
    }
}
