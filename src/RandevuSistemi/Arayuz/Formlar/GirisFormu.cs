using RandevuSistemi.Models.Kullanicilar;
using RandevuSistemi.Servisler;
using RandevuSistemi.Veri;

namespace RandevuSistemi.Arayuz.Formlar;

/// <summary>
/// Program açılınca gösterilen giriş penceresi.
/// Doğru kullanıcı adı ve şifre girilince "GirisYapan" dolar ve pencere OK ile kapanır.
/// </summary>
public class GirisFormu : Form
{
    private readonly OturumServisi _oturum;
    private readonly TextBox _kullaniciAdiKutusu;
    private readonly TextBox _sifreKutusu;

    public GirisFormu(UygulamaVerisi veri)
    {
        _oturum = new OturumServisi(veri);

        Text = "Giriş - Hastane / Klinik Randevu Sistemi";
        ClientSize = new Size(440, 470);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = Tema.Normal;
        BackColor = Tema.Kart;

        var tablo = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 9, Padding = new Padding(40, 24, 40, 20) };
        tablo.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));  // başlık
        tablo.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));  // alt başlık
        tablo.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));  // "Kullanıcı adı"
        tablo.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));  // kutu
        tablo.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));  // "Şifre"
        tablo.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));  // kutu
        tablo.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));  // giriş butonu
        tablo.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // örnek hesaplar
        tablo.RowStyles.Add(new RowStyle(SizeType.Absolute, 4));

        var baslik = new Label
        {
            Text = "RANDEVU SİSTEMİ",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 18f),
            ForeColor = Tema.Birincil,
            TextAlign = ContentAlignment.MiddleCenter
        };
        var altBaslik = new Label
        {
            Text = "Devam etmek için giriş yapın",
            Dock = DockStyle.Fill,
            ForeColor = Tema.SolukYazi,
            TextAlign = ContentAlignment.TopCenter
        };

        _kullaniciAdiKutusu = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11f) };
        _sifreKutusu = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11f), UseSystemPasswordChar = true };

        Button girisButonu = Tema.Buton("Giriş Yap");
        girisButonu.AutoSize = false;
        girisButonu.Dock = DockStyle.Fill;
        girisButonu.Margin = new Padding(0, 14, 0, 4);
        girisButonu.Click += (_, _) => GirisYap();

        // Sunum/deneme kolaylığı için örnek hesaplar (gerçek bir sistemde gösterilmez).
        var ornekHesaplar = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Tema.SolukYazi,
            Font = new Font("Segoe UI", 9f),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "Örnek hesaplar (şifre: sifre123)\n" +
                   "•  ayse  →  Hasta\n" +
                   "•  drmehmet  →  Doktor\n" +
                   "•  zeynep  →  Sağlık Personeli\n" +
                   "•  admin  →  Yönetici"
        };

        tablo.Controls.Add(baslik, 0, 0);
        tablo.Controls.Add(altBaslik, 0, 1);
        tablo.Controls.Add(KucukEtiket("Kullanıcı adı"), 0, 2);
        tablo.Controls.Add(_kullaniciAdiKutusu, 0, 3);
        tablo.Controls.Add(KucukEtiket("Şifre"), 0, 4);
        tablo.Controls.Add(_sifreKutusu, 0, 5);
        tablo.Controls.Add(girisButonu, 0, 6);
        tablo.Controls.Add(ornekHesaplar, 0, 7);
        Controls.Add(tablo);

        AcceptButton = girisButonu; // Enter ile giriş
        ActiveControl = _kullaniciAdiKutusu;
    }

    /// <summary>Başarılı girişten sonra giriş yapan kullanıcı.</summary>
    public Kullanici? GirisYapan { get; private set; }

    private void GirisYap()
    {
        try
        {
            GirisYapan = _oturum.GirisYap(_kullaniciAdiKutusu.Text, _sifreKutusu.Text);
            DialogResult = DialogResult.OK;
        }
        catch (Exception hata) when (hata is ArgumentException or UnauthorizedAccessException)
        {
            Tema.HataGoster(hata.Message);
            _sifreKutusu.Clear();
            _sifreKutusu.Focus();
        }
    }

    private static Label KucukEtiket(string metin) => new()
    {
        Text = metin,
        Dock = DockStyle.Fill,
        ForeColor = Tema.SolukYazi,
        TextAlign = ContentAlignment.BottomLeft
    };
}
