using RandevuSistemi.Arayuz.Ekranlar;
using RandevuSistemi.Veri;

namespace RandevuSistemi.Arayuz;

/// <summary>
/// Ana pencere: solda menü, üstte sayfa başlığı, ortada seçilen ekran.
///
/// Form sınıfı .NET'in hazır Form sınıfından TÜRER (kalıtım). Ekranlar ise
/// UserControl'den türeyen ayrı sınıflardır; menüden seçilen ekran orta alana yerleştirilir.
/// Bu form, tasarımcı (Designer) kullanılmadan tamamen kodla oluşturulmuştur.
/// </summary>
public class AnaForm : Form
{
    private readonly UygulamaVerisi _veri;

    private readonly Panel _icerik;
    private readonly Label _sayfaBasligi;
    private readonly FlowLayoutPanel _menu;
    private Button? _aktifButon;

    public AnaForm()
    {
        // Şimdilik örnek veriler; ileride JSON dosyasından okunacak.
        _veri = OrnekVeriler.Olustur();

        Text = "Hastane / Klinik Randevu Sistemi";
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
        // Not: Dock sırasında son eklenen kontrol önce yerleşir.
        // Önce "Fill" olan menüyü, sonra "Top" olan logoyu ekliyoruz.
        yanPanel.Controls.Add(_menu);
        yanPanel.Controls.Add(logo);
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

        // ---- Menü öğeleri ----
        Button kurumlarButonu = MenuEkle("Kurumlar", () => new KurumlarEkrani(_veri.Kurumlar));
        MenuEkle("Hizmetler", () => new HizmetlerEkrani(_veri));
        MenuEkle("Çalışanlar", () => new CalisanlarEkrani(_veri));
        MenuEkle("Kullanıcılar", () => new KullanicilarEkrani(_veri.Kullanicilar));

        // Açılışta ilk ekranı göster
        EkranGoster(kurumlarButonu, "Kurumlar", new KurumlarEkrani(_veri.Kurumlar));
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
