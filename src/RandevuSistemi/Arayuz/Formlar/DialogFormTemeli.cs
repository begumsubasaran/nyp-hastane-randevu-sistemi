namespace RandevuSistemi.Arayuz.Formlar;

/// <summary>
/// Ekleme pencerelerinin (yeni kurum, şube, hasta, çalışan) ORTAK SOYUT temel sınıfı.
///
/// Ortak işleri bir kez burada yapar: pencere ayarları, "etiket + kutu" satırları,
/// Kaydet / İptal butonları ve hata gösterimi. Her alt sınıf sadece kendi alanlarını ekler
/// ve "Kaydet" tıklanınca ne yapılacağını (abstract Kaydet metodu) yazar.
/// Bu yapıya "Template Method" (şablon metot) tasarım deseni denir.
///
/// Kalıtım ağacı:
///   Form (.NET)
///   └── DialogFormTemeli (abstract)
///       ├── KurumFormu
///       ├── SubeFormu
///       └── KullaniciFormuTemeli (abstract)
///           ├── HastaKayitFormu
///           └── CalisanFormu
/// </summary>
public abstract class DialogFormTemeli : Form
{
    private readonly TableLayoutPanel _tablo;
    private bool _butonlarEklendi;

    protected DialogFormTemeli(string baslik, int kutuGenisligi = 280)
    {
        Text = baslik;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = Tema.Normal;
        BackColor = Tema.Kart;

        // Pencere, eklenen satır sayısına göre kendi boyunu ayarlar.
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        _tablo = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 0,
            Padding = new Padding(20, 16, 20, 12)
        };
        _tablo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        _tablo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, kutuGenisligi));
        Controls.Add(_tablo);
    }

    /// <summary>Pencereye "Etiket: [kutu]" şeklinde bir satır ekler.</summary>
    protected void SatirEkle(string etiket, Control kontrol)
    {
        int satir = _tablo.RowCount;
        _tablo.RowCount++;
        _tablo.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var etiketKontrolu = new Label
        {
            Text = etiket,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Tema.SolukYazi,
            Margin = new Padding(0, 4, 8, 4)
        };
        kontrol.Dock = DockStyle.Fill;
        kontrol.Margin = new Padding(0, 4, 0, 4);

        _tablo.Controls.Add(etiketKontrolu, 0, satir);
        _tablo.Controls.Add(kontrol, 1, satir);
    }

    /// <summary>
    /// Pencere ilk gösterilirken (tüm constructor'lar bittikten sonra) Kaydet / İptal butonlarını
    /// en alta ekler. Böylece alt sınıfların eklediği satırlar her zaman butonların üstünde kalır.
    /// </summary>
    protected override void OnLoad(EventArgs e)
    {
        if (!_butonlarEklendi)
        {
            ButonlariEkle();
            _butonlarEklendi = true;
        }
        base.OnLoad(e);
    }

    private void ButonlariEkle()
    {
        int satir = _tablo.RowCount;
        _tablo.RowCount++;
        _tablo.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var butonlar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Margin = new Padding(0, 14, 0, 0)
        };
        Button kaydet = Tema.Buton("Kaydet");
        Button iptal = Tema.Buton("İptal", birincil: false);
        iptal.DialogResult = DialogResult.Cancel;
        iptal.CausesValidation = false;
        kaydet.Click += (_, _) => KaydetTiklandi();
        butonlar.Controls.Add(kaydet);
        butonlar.Controls.Add(iptal);

        _tablo.Controls.Add(butonlar, 0, satir);
        _tablo.SetColumnSpan(butonlar, 2);

        AcceptButton = kaydet;
        CancelButton = iptal;
    }

    /// <summary>
    /// Alt sınıfın kaydetme işlemini çağırır. Model sınıflarının fırlattığı doğrulama hataları
    /// burada tek bir yerde yakalanıp kullanıcıya gösterilir.
    /// </summary>
    private void KaydetTiklandi()
    {
        try
        {
            if (Kaydet())
                DialogResult = DialogResult.OK;
        }
        catch (Exception hata) when (hata is ArgumentException or InvalidOperationException)
        {
            Tema.HataGoster(hata.Message);
        }
    }

    /// <summary>
    /// Her alt sınıf bu metodu kendine göre yazar (ABSTRACT → zorunlu OVERRIDE).
    /// Başarılıysa true döndürür; hata varsa exception fırlatabilir.
    /// </summary>
    protected abstract bool Kaydet();

    /// <summary>Alt sınıfların kullandığı yardımcı: liste kutusu oluşturur.</summary>
    protected static ComboBox SecimKutusu(IEnumerable<object> secenekler)
    {
        var kutu = new KaydirmasizComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        foreach (object secenek in secenekler)
            kutu.Items.Add(secenek);
        if (kutu.Items.Count > 0)
            kutu.SelectedIndex = 0;
        return kutu;
    }
}
