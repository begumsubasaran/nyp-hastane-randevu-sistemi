using System.ComponentModel;
using System.Globalization;
using RandevuSistemi.Models.Hizmetler;
using RandevuSistemi.Models.Kurumlar;

namespace RandevuSistemi.Arayuz.Formlar;

/// <summary>
/// Şubeye yeni sağlık hizmeti eklemek için açılan küçük pencere (dialog).
/// Kurallar (ad boş olamaz, süre aralığı, aynı isim vb.) model sınıflarında kontrol edilir;
/// bu form sadece hatayı kullanıcıya gösterir.
/// </summary>
public class HizmetFormu : Form
{
    private readonly Sube _sube;
    private readonly TextBox _adKutusu;
    private readonly ComboBox _kategoriKutusu;
    private readonly NumericUpDown _sureKutusu;
    private readonly NumericUpDown _ucretKutusu;
    private readonly TextBox _aciklamaKutusu;

    public HizmetFormu(SubeSecenegi secenek)
    {
        _sube = secenek.Sube;

        Text = $"Yeni Hizmet - {secenek}";
        ClientSize = new Size(460, 340);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = Tema.Normal;
        BackColor = Tema.Kart;

        var tablo = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(20)
        };
        tablo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        tablo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 4; i++)
            tablo.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        tablo.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // açıklama
        tablo.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));   // butonlar

        _adKutusu = new TextBox { Dock = DockStyle.Fill };

        _kategoriKutusu = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        foreach (HizmetKategorisi kategori in Enum.GetValues<HizmetKategorisi>())
            _kategoriKutusu.Items.Add(kategori.GorunenAd());
        _kategoriKutusu.SelectedIndex = 0;

        _sureKutusu = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            Minimum = SaglikHizmeti.EnKisaSureDakika,
            Maximum = SaglikHizmeti.EnUzunSureDakika,
            Increment = 5,
            Value = 15
        };

        _ucretKutusu = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            // Alt sınırı negatif bırakıyoruz; yoksa kutu negatif değeri sessizce 0'a çevirir.
            // Negatif değeri biz yakalayıp kullanıcıya uyarı gösteriyoruz (UcretDogrula).
            Minimum = -1_000_000,
            Maximum = 1_000_000,
            DecimalPlaces = 2,
            Increment = 50,
            ThousandsSeparator = true,
            Value = 500
        };

        _ucretKutusu.Validating += UcretDogrula;

        _aciklamaKutusu = new TextBox { Dock = DockStyle.Fill, Multiline = true };

        tablo.Controls.Add(Etiket("Hizmet adı"), 0, 0);
        tablo.Controls.Add(_adKutusu, 1, 0);
        tablo.Controls.Add(Etiket("Kategori"), 0, 1);
        tablo.Controls.Add(_kategoriKutusu, 1, 1);
        tablo.Controls.Add(Etiket("Süre (dakika)"), 0, 2);
        tablo.Controls.Add(_sureKutusu, 1, 2);
        tablo.Controls.Add(Etiket("Ücret (₺)"), 0, 3);
        tablo.Controls.Add(_ucretKutusu, 1, 3);
        tablo.Controls.Add(Etiket("Açıklama"), 0, 4);
        tablo.Controls.Add(_aciklamaKutusu, 1, 4);

        // ---- Butonlar (sağa yaslı) ----
        var butonlar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 12, 0, 0)
        };
        Button kaydet = Tema.Buton("Kaydet");
        Button iptal = Tema.Buton("İptal", birincil: false);
        iptal.DialogResult = DialogResult.Cancel; // tıklanınca form kapanır
        iptal.CausesValidation = false;           // hatalı değer varken de İptal çalışsın
        kaydet.Click += (_, _) => Kaydet();
        butonlar.Controls.Add(kaydet);
        butonlar.Controls.Add(iptal);
        tablo.Controls.Add(butonlar, 0, 5);
        tablo.SetColumnSpan(butonlar, 2);

        Controls.Add(tablo);
        AcceptButton = kaydet; // Enter tuşu
        CancelButton = iptal;  // Esc tuşu

        // Hatalı değer varken pencere X ile kapatılmak istenirse kapanmasına izin ver.
        FormClosing += (_, e) => e.Cancel = false;
    }

    /// <summary>Oluşturulan hizmet (form "Kaydet" ile kapandıysa dolu olur).</summary>
    public SaglikHizmeti? OlusturulanHizmet { get; private set; }

    private void Kaydet()
    {
        // Enter tuşuyla kaydedilirse odak kutudan çıkmamış olabilir;
        // bu yüzden tüm kutuların doğrulamasını burada da çalıştırıyoruz.
        if (!ValidateChildren())
            return;

        try
        {
            OlusturulanHizmet = _sube.HizmetEkle(
                _adKutusu.Text,
                (HizmetKategorisi)_kategoriKutusu.SelectedIndex,
                (int)_sureKutusu.Value,
                _ucretKutusu.Value,
                string.IsNullOrWhiteSpace(_aciklamaKutusu.Text) ? null : _aciklamaKutusu.Text.Trim());

            DialogResult = DialogResult.OK; // modal formu kapatır
        }
        catch (Exception hata) when (hata is ArgumentException or InvalidOperationException)
        {
            // Model sınıfının fırlattığı hata mesajını kullanıcıya gösteriyoruz.
            Tema.HataGoster(hata.Message);
        }
    }

    /// <summary>
    /// Ücret kutusundan çıkılırken (veya Kaydet'e basılınca) çalışır.
    /// e.Cancel = true yapılırsa odak kutuda kalır ve kayıt yapılmaz.
    /// </summary>
    private void UcretDogrula(object? sender, CancelEventArgs e)
    {
        if (UcretNegatifMi())
        {
            Tema.HataGoster("Ücret için negatif değer giremezsiniz.");
            e.Cancel = true;
        }
    }

    /// <summary>
    /// Kutunun değeri ya da henüz onaylanmamış yazılı metni negatif mi?
    /// (Klavyeyle yazıp hemen Enter'a basıldığında Value henüz güncellenmemiş olabilir.)
    /// </summary>
    private bool UcretNegatifMi()
    {
        if (_ucretKutusu.Value < 0)
            return true;

        return decimal.TryParse(_ucretKutusu.Text, NumberStyles.Number, CultureInfo.CurrentCulture,
                                out decimal yazilan) && yazilan < 0;
    }

    private static Label Etiket(string metin) => new()
    {
        Text = metin,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        ForeColor = Tema.SolukYazi
    };
}
