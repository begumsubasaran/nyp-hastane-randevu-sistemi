using RandevuSistemi.Arayuz.Ekranlar;
using RandevuSistemi.Models.Kullanicilar;
using RandevuSistemi.Veri;

namespace RandevuSistemi.Arayuz;

/// <summary>
/// Ana pencere: solda menü, üstte sayfa başlığı, ortada seçilen ekran.
///
/// Form sınıfı .NET'in hazır Form sınıfından TÜRER (kalıtım). Ekranlar ise
/// UserControl'den türeyen ayrı sınıflardır; menüden seçilen ekran orta alana yerleştirilir.
/// Bu form, tasarımcı (Designer) kullanılmadan tamamen kodla oluşturulmuştur.
///
/// Menüde hangi ekranların görüneceği giriş yapan kullanıcının rolüne göre belirlenir:
/// Hasta → Randevularım; Doktor/Personel → Randevularım + Çalışma Bilgilerim; Yönetici → tüm ekranlar.
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
    private Button? _aktifButon;

    public AnaForm(UygulamaVerisi veri, IVeriDeposu depo, Kullanici aktifKullanici)
    {
        _veri = veri;
        _depo = depo;
        _aktifKullanici = aktifKullanici;

        Text = $"Hastane / Klinik Randevu Sistemi  —  {aktifKullanici.TamAd} ({aktifKullanici.Rol.GorunenAd()})";
        Size = new Size(1200, 760);
        MinimumSize = new Size(1000, 620);
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
        yerlesim.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        yerlesim.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // ---- Sol menü ----
        var yanPanel = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Birincil, Margin = new Padding(0) };
        var logo = new Label
        {
            Text = "RANDEVU SİSTEMİ",
            Dock = DockStyle.Top,
            Height = 64,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 13f),
            TextAlign = ContentAlignment.MiddleCenter
        };
        _menu = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 12, 0, 0)
        };
        _kayitDurumu = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            ForeColor = Color.FromArgb(190, 205, 225),
            Font = new Font("Segoe UI", 8.5f),
            TextAlign = ContentAlignment.MiddleCenter,
            Text = "Veriler otomatik kaydedilir"
        };
        new ToolTip().SetToolTip(_kayitDurumu, $"Veri dosyası:\n{_depo.Konum}");

        // Giriş yapan kullanıcının adı ve rolü
        var kullaniciKarti = new Label
        {
            Dock = DockStyle.Top,
            Height = 58,
            ForeColor = Color.White,
            BackColor = Tema.BirincilKoyu,
            Font = new Font("Segoe UI", 10f),
            TextAlign = ContentAlignment.MiddleCenter,
            Text = $"{aktifKullanici.TamAd}\n{aktifKullanici.Rol.GorunenAd()}"
        };

        Button cikisButonu = Tema.MenuButonu("Çıkış Yap");
        cikisButonu.Dock = DockStyle.Bottom;
        cikisButonu.Click += (_, _) =>
        {
            OturumKapatildi = true; // Program.cs giriş ekranını tekrar açacak
            Close();
        };

        // Not: Dock sırasında SON eklenen kontrol ÖNCE yerleşir.
        // Bu yüzden "Fill" olan menü en başta eklenir; en alttaki kayıt bilgisi en sonda.
        yanPanel.Controls.Add(_menu);
        yanPanel.Controls.Add(kullaniciKarti);
        yanPanel.Controls.Add(logo);
        yanPanel.Controls.Add(cikisButonu);
        yanPanel.Controls.Add(_kayitDurumu);
        yerlesim.Controls.Add(yanPanel, 0, 0);
        yerlesim.SetRowSpan(yanPanel, 2);

        // ---- Üst başlık ----
        var ustPanel = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Kart, Margin = new Padding(0) };
        _sayfaBasligi = new Label
        {
            Dock = DockStyle.Fill,
            Font = Tema.Baslik,
            ForeColor = Tema.Yazi,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(24, 0, 0, 0)
        };
        ustPanel.Controls.Add(_sayfaBasligi);
        yerlesim.Controls.Add(ustPanel, 1, 0);

        // ---- Ekranların gösterileceği alan ----
        _icerik = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), Margin = new Padding(0) };
        yerlesim.Controls.Add(_icerik, 1, 1);

        Controls.Add(yerlesim);

        // ---- Menü öğeleri: role göre ----
        // Nesnenin gerçek türüne göre (pattern matching) farklı menü kuruluyor.
        string randevuBasligi = _aktifKullanici is Yonetici ? "Randevular" : "Randevularım";
        Button randevularButonu = MenuEkle(randevuBasligi, () => new RandevularEkrani(_veri, _aktifKullanici));

        switch (_aktifKullanici)
        {
            case Yonetici:
                MenuEkle("Kurumlar", () => new KurumlarEkrani(_veri, _aktifKullanici));
                MenuEkle("Hizmetler", () => new HizmetlerEkrani(_veri));
                MenuEkle("Çalışanlar", () => new CalisanlarEkrani(_veri));
                MenuEkle("Kullanıcılar", () => new KullanicilarEkrani(_veri.Kullanicilar));
                break;

            case Calisan calisan:
                // Doktor/personel sadece kendi bilgilerini (uygunluk saatleri vb.) görür.
                MenuEkle("Çalışma Bilgilerim", () => new CalisanlarEkrani(_veri, calisan));
                break;

            // Hasta: sadece "Randevularım"
        }

        // Açılışta ilk ekranı göster
        EkranGoster(randevularButonu, randevuBasligi, new RandevularEkrani(_veri, _aktifKullanici));

        // Program kapanırken son değişiklikler kaydedilir.
        FormClosing += (_, _) => VeriyiKaydet();
    }

    /// <summary>"Çıkış Yap" ile kapatıldıysa true; X ile kapatıldıysa false (program biter).</summary>
    public bool OturumKapatildi { get; private set; }

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
    /// Menüye bir buton ekler. Butona tıklanınca verilen fonksiyonla ekran oluşturulup gösterilir.
    /// Func&lt;Control&gt;: "Control döndüren bir fonksiyon" demektir.
    /// </summary>
    private Button MenuEkle(string baslik, Func<Control> ekranOlustur)
    {
        Button buton = Tema.MenuButonu(baslik);
        buton.Click += (_, _) => EkranGoster(buton, baslik, ekranOlustur());
        _menu.Controls.Add(buton);
        return buton;
    }

    private void EkranGoster(Button buton, string baslik, Control ekran)
    {
        // Önceki ekranda yapılan değişiklikleri kaydet.
        VeriyiKaydet();

        if (_aktifButon is not null)
            _aktifButon.BackColor = Tema.Birincil;
        buton.BackColor = Tema.BirincilKoyu;
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
