using RandevuSistemi.Arayuz.Ekranlar;
using RandevuSistemi.Arayuz.Kontroller;
using RandevuSistemi.Models.Kullanicilar;
using RandevuSistemi.Veri;

namespace RandevuSistemi.Arayuz;

/// <summary>
/// Ana pencere: solda beyaz menü (simgeli), üstte sayfa başlığı ve tarih, ortada seçilen ekran.
///
/// Form sınıfı .NET'in hazır Form sınıfından TÜRER (kalıtım). Ekranlar ise
/// UserControl'den türeyen ayrı sınıflardır; menüden seçilen ekran orta alana yerleştirilir.
/// Bu form, tasarımcı (Designer) kullanılmadan tamamen kodla oluşturulmuştur.
///
/// Menüde hangi ekranların görüneceği giriş yapan kullanıcının rolüne göre belirlenir:
/// Hasta → Ana Sayfa, Randevularım, Kurumlar ve Hizmetler (salt okunur);
/// Doktor/Personel → Ana Sayfa, Randevularım, Çalışma Bilgilerim; Yönetici → tüm ekranlar.
///
/// Veriler otomatik kaydedilir: ekran değiştirilirken ve program kapatılırken.
/// Form, kaydetme işini sadece IVeriDeposu arayüzü üzerinden yapar; JSON kullanıldığını bilmez.
/// </summary>
public class AnaForm : Form
{
    private readonly UygulamaVerisi _veri;
    private readonly IVeriDeposu _depo;
    private readonly Kullanici _aktifKullanici;
    private readonly Label _kayitDurumu;

    private readonly Panel _icerik;
    private readonly Label _sayfaBasligi;
    private readonly FlowLayoutPanel _menu;
    private readonly Dictionary<string, MenuButonu> _menuButonlari = new();
    private MenuButonu? _aktifButon;

    public AnaForm(UygulamaVerisi veri, IVeriDeposu depo, Kullanici aktifKullanici)
    {
        _veri = veri;
        _depo = depo;
        _aktifKullanici = aktifKullanici;

        Text = $"Randevu Sistemi  —  {aktifKullanici.TamAd} ({aktifKullanici.Rol.GorunenAd()})";
        Size = new Size(1280, 800);
        MinimumSize = new Size(1060, 660);
        StartPosition = FormStartPosition.CenterScreen;
        Font = Tema.Normal;
        BackColor = Tema.Arkaplan;

        // ---- Ana yerleşim: 2 sütun (menü | içerik), 2 satır (başlık | ekran) ----
        var yerlesim = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        yerlesim.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        yerlesim.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        yerlesim.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        yerlesim.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // ================= Sol menü (beyaz) =================
        var yanPanel = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Kart, Margin = new Padding(0) };

        // Logo: turkuaz kalp simgesi + uygulama adı
        var logo = new Panel { Dock = DockStyle.Top, Height = 76 };
        var logoIkonu = new Label
        {
            Text = Tema.Ikonlar.Saglik,
            Font = new Font(Tema.Ikon.FontFamily, 20f),
            ForeColor = Tema.Vurgu,
            Dock = DockStyle.Left,
            Width = 60,
            TextAlign = ContentAlignment.MiddleCenter
        };
        var logoYazisi = new Label
        {
            Text = "Randevu\nSistemi",
            Font = new Font("Segoe UI Semibold", 12.5f),
            ForeColor = Tema.Lacivert,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        logo.Controls.Add(logoYazisi);
        logo.Controls.Add(logoIkonu);

        _menu = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };

        // Alt kısım: kullanıcı kartı (baş harf rozeti + ad + rol)
        var kullaniciKarti = new Panel { Dock = DockStyle.Bottom, Height = 64, Padding = new Padding(16, 12, 8, 12) };
        var rozet = new BasHarfRozeti(aktifKullanici.Ad, aktifKullanici.Soyad) { Dock = DockStyle.Left };
        var kullaniciYazisi = new Label
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10, 0, 0, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Tema.Yazi,
            Text = $"{aktifKullanici.TamAd}\n{aktifKullanici.Rol.GorunenAd()}"
        };
        kullaniciKarti.Controls.Add(kullaniciYazisi);
        kullaniciKarti.Controls.Add(rozet);

        var cikisButonu = new MenuButonu(Tema.Ikonlar.Cikis, "Çıkış Yap") { Dock = DockStyle.Bottom };
        cikisButonu.Click += (_, _) =>
        {
            OturumKapatildi = true; // Program.cs giriş ekranını tekrar açacak
            Close();
        };

        _kayitDurumu = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 40,
            ForeColor = Tema.SolukYazi,
            Font = new Font("Segoe UI", 8f),
            TextAlign = ContentAlignment.MiddleCenter,
            Text = "Veriler otomatik kaydedilir"
        };
        new ToolTip().SetToolTip(_kayitDurumu, $"Veri dosyası:\n{_depo.Konum}");

        var ayiriciCizgi = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Tema.Cizgi };
        var sagCizgi = new Panel { Dock = DockStyle.Right, Width = 1, BackColor = Tema.Cizgi };

        // Not: Dock sırasında SON eklenen kontrol ÖNCE yerleşir.
        // Bu yüzden "Fill" olan menü en başta, en dıştaki kenar çizgisi en sonda eklenir.
        yanPanel.Controls.Add(_menu);
        yanPanel.Controls.Add(logo);
        yanPanel.Controls.Add(cikisButonu);
        yanPanel.Controls.Add(ayiriciCizgi);
        yanPanel.Controls.Add(kullaniciKarti);
        yanPanel.Controls.Add(_kayitDurumu);
        yanPanel.Controls.Add(sagCizgi);
        yerlesim.Controls.Add(yanPanel, 0, 0);
        yerlesim.SetRowSpan(yanPanel, 2);

        // ================= Üst başlık (sayfa adı + tarih) =================
        var ustPanel = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Arkaplan, Margin = new Padding(0), Padding = new Padding(28, 14, 28, 0) };
        _sayfaBasligi = new Label
        {
            Dock = DockStyle.Fill,
            Font = Tema.Baslik,
            ForeColor = Tema.Lacivert,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var tarihEtiketi = new Label
        {
            Dock = DockStyle.Right,
            Width = 300,
            ForeColor = Tema.SolukYazi,
            TextAlign = ContentAlignment.MiddleRight,
            Text = DateTime.Today.ToString("d MMMM yyyy, dddd", new System.Globalization.CultureInfo("tr-TR"))
        };
        ustPanel.Controls.Add(_sayfaBasligi);
        ustPanel.Controls.Add(tarihEtiketi);
        yerlesim.Controls.Add(ustPanel, 1, 0);

        // ================= Ekranların gösterileceği alan =================
        _icerik = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28, 8, 28, 24), Margin = new Padding(0) };
        yerlesim.Controls.Add(_icerik, 1, 1);

        Controls.Add(yerlesim);

        // ================= Menü öğeleri: role göre =================
        // Nesnenin gerçek türüne göre (pattern matching) farklı menü kuruluyor.
        MenuButonu anaSayfaButonu = MenuEkle(Tema.Ikonlar.AnaSayfa, "Ana Sayfa", AnaSayfaOlustur);

        string randevuBasligi = _aktifKullanici is Yonetici ? "Randevular" : "Randevularım";
        MenuEkle(Tema.Ikonlar.Takvim, randevuBasligi, () => new RandevularEkrani(_veri, _aktifKullanici));

        switch (_aktifKullanici)
        {
            case Yonetici:
                MenuEkle(Tema.Ikonlar.Kurum, "Kurumlar", () => new KurumlarEkrani(_veri, _aktifKullanici));
                MenuEkle(Tema.Ikonlar.Saglik, "Hizmetler", () => new HizmetlerEkrani(_veri));
                MenuEkle(Tema.Ikonlar.Kisiler, "Çalışanlar", () => new CalisanlarEkrani(_veri, _aktifKullanici));
                MenuEkle(Tema.Ikonlar.Kisi, "Kullanıcılar", () => new KullanicilarEkrani(_veri.Kullanicilar));
                break;

            case Calisan calisan:
                // Doktor/personel sadece kendi bilgilerini (uygunluk saatleri vb.) görür.
                MenuEkle(Tema.Ikonlar.Saat, "Çalışma Bilgilerim", () => new CalisanlarEkrani(_veri, _aktifKullanici, calisan));
                break;

            case Hasta:
                // Hasta kurumları sadece görüntüler: çalışma saatleri, bölümler, hizmetler (süre, ücret).
                MenuEkle(Tema.Ikonlar.Kurum, "Kurumlar ve Hizmetler", () => new KurumlarEkrani(_veri, _aktifKullanici));
                break;
        }

        // Açılışta ana sayfayı göster
        EkranGoster(anaSayfaButonu, "Ana Sayfa", AnaSayfaOlustur());

        // Program kapanırken son değişiklikler kaydedilir.
        FormClosing += (_, _) => VeriyiKaydet();
    }

    /// <summary>"Çıkış Yap" ile kapatıldıysa true; X ile kapatıldıysa false (program biter).</summary>
    public bool OturumKapatildi { get; private set; }

    /// <summary>Ana sayfa ekranını oluşturur; kısayol kartları menüdeki ekranlara geçiş yapabilir.</summary>
    private Control AnaSayfaOlustur() => new AnaSayfaEkrani(_veri, _aktifKullanici, MenuyeGit);

    /// <summary>Başlığı verilen menü öğesine tıklanmış gibi o ekrana geçer.</summary>
    private void MenuyeGit(string baslik)
    {
        // BeginInvoke: geçiş, tıklanan kısayol kartının Click olayı bittikten sonra yapılır.
        // (Ana sayfa ekranı kendi Click olayının ortasında silinmesin diye.)
        if (_menuButonlari.TryGetValue(baslik, out MenuButonu? buton))
            BeginInvoke(new Action(buton.PerformClick));
    }

    /// <summary>Tüm verileri depoya kaydeder; hata olursa kullanıcıya bildirir.</summary>
    private void VeriyiKaydet()
    {
        try
        {
            _depo.Kaydet(_veri);
            _kayitDurumu.Text = $"Veriler otomatik kaydedilir\nSon kayıt: {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception hata) when (hata is IOException or UnauthorizedAccessException)
        {
            Tema.HataGoster($"Veriler kaydedilemedi:\n{hata.Message}");
        }
    }

    /// <summary>
    /// Menüye simgeli bir buton ekler. Butona tıklanınca verilen fonksiyonla ekran oluşturulup gösterilir.
    /// Func&lt;Control&gt;: "Control döndüren bir fonksiyon" demektir.
    /// </summary>
    private MenuButonu MenuEkle(string ikon, string baslik, Func<Control> ekranOlustur)
    {
        var buton = new MenuButonu(ikon, baslik);
        buton.Click += (_, _) => EkranGoster(buton, baslik, ekranOlustur());
        _menu.Controls.Add(buton);
        _menuButonlari[baslik] = buton;
        return buton;
    }

    private void EkranGoster(MenuButonu buton, string baslik, Control ekran)
    {
        // Önceki ekranda yapılan değişiklikleri kaydet.
        VeriyiKaydet();

        if (_aktifButon is not null)
            _aktifButon.Secili = false;
        buton.Secili = true;
        _aktifButon = buton;

        _sayfaBasligi.Text = baslik;

        // Eski ekranı kaldırıp bellekten temizle, yenisini yerleştir.
        List<Control> eskiEkranlar = _icerik.Controls.Cast<Control>().ToList();
        _icerik.Controls.Clear();
        foreach (Control eski in eskiEkranlar)
            eski.Dispose();

        ekran.Dock = DockStyle.Fill;
        _icerik.Controls.Add(ekran);
    }
}
