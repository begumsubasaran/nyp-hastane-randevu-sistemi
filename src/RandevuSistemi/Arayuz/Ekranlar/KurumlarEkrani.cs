using RandevuSistemi.Arayuz.Formlar;
using RandevuSistemi.Models.Hizmetler;
using RandevuSistemi.Models.Kullanicilar;
using RandevuSistemi.Models.Kurumlar;
using RandevuSistemi.Veri;

namespace RandevuSistemi.Arayuz.Ekranlar;

/// <summary>
/// Kurumları ve şubelerini ağaç yapısında gösterir ve yönetir.
/// - Sistem Yöneticisi: yeni hastane/klinik ve şube ekleyebilir ("yetki çerçevesinde").
/// - Yöneticiler: seçilen şubenin çalışma saatlerini ve bölümlerini düzenleyebilir.
/// Hizmetler burada sadece listelenir; eklemek/silmek için Hizmetler ekranı kullanılır.
/// </summary>
public class KurumlarEkrani : UserControl
{
    private readonly UygulamaVerisi _veri;
    private readonly Kullanici _aktifKullanici;

    private readonly TreeView _agac;
    private readonly Label _secilenBaslik;
    private readonly Label _secilenBilgi;
    private readonly ListBox _saatListesi;
    private readonly ListBox _bolumListesi;
    private readonly ListBox _hizmetListesi;

    // Çalışma saati düzenleme
    private readonly ComboBox _gunKutusu;
    private readonly DateTimePicker _acilisSaati;
    private readonly DateTimePicker _kapanisSaati;
    private readonly FlowLayoutPanel _saatAraclari;

    // Bölüm düzenleme
    private readonly TextBox _bolumAdiKutusu;
    private readonly FlowLayoutPanel _bolumAraclari;

    private Sube? _seciliSube;
    private Kurum? _seciliKurum;

    public KurumlarEkrani(UygulamaVerisi veri, Kullanici aktifKullanici)
    {
        _veri = veri;
        _aktifKullanici = aktifKullanici;
        BackColor = Tema.Arkaplan;

        var yerlesim = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        yerlesim.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        yerlesim.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));

        // ================= Sol: butonlar + kurum / şube ağacı =================
        var solTaraf = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0, 0, 12, 0) };
        solTaraf.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        solTaraf.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var solButonlar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        if (_aktifKullanici.KurumTanimlayabilirMi)
        {
            Button yeniKurum = Tema.Buton("+ Yeni Kurum");
            Button yeniSube = Tema.Buton("+ Yeni Şube", birincil: false);
            yeniKurum.Click += (_, _) => YeniKurum();
            yeniSube.Click += (_, _) => YeniSube();
            solButonlar.Controls.Add(yeniKurum);
            solButonlar.Controls.Add(yeniSube);
        }
        else
        {
            solButonlar.Controls.Add(new Label
            {
                Text = "Yeni kurum/şube sadece Sistem Yöneticisi tarafından eklenebilir.",
                AutoSize = true,
                ForeColor = Tema.SolukYazi,
                Margin = new Padding(0, 10, 0, 0)
            });
        }
        solTaraf.Controls.Add(solButonlar, 0, 0);

        _agac = new TreeView
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 11f),
            ItemHeight = 30,
            HideSelection = false,
            FullRowSelect = true,
            ShowLines = false
        };
        _agac.AfterSelect += (_, e) => SecimiGoster(e.Node?.Tag);
        solTaraf.Controls.Add(KartIcineAl(_agac), 0, 1);
        yerlesim.Controls.Add(solTaraf, 0, 0);

        // ================= Sağ: seçilen öğenin ayrıntıları =================
        var detay = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 10, BackColor = Tema.Kart };
        detay.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));   // 0 başlık
        detay.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));   // 1 bilgi
        detay.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));   // 2 "Çalışma Saatleri"
        detay.RowStyles.Add(new RowStyle(SizeType.Percent, 36));    // 3 saat listesi
        detay.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));   // 4 saat araçları
        detay.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));   // 5 "Bölümler"
        detay.RowStyles.Add(new RowStyle(SizeType.Percent, 28));    // 6 bölüm listesi
        detay.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));   // 7 bölüm araçları
        detay.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));   // 8 "Hizmetler"
        detay.RowStyles.Add(new RowStyle(SizeType.Percent, 36));    // 9 hizmet listesi

        _secilenBaslik = new Label { Dock = DockStyle.Fill, Font = Tema.AltBaslik, ForeColor = Tema.Yazi };
        _secilenBilgi = new Label { Dock = DockStyle.Fill, ForeColor = Tema.SolukYazi };
        _saatListesi = YeniListe(secilebilir: true);
        _bolumListesi = YeniListe(secilebilir: true);
        _hizmetListesi = YeniListe(secilebilir: false);

        // ---- Çalışma saati araçları: [Gün] [açılış] – [kapanış] [Kaydet] [Günü Kapat] ----
        _gunKutusu = new KaydirmasizComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110, Margin = new Padding(0, 6, 6, 0) };
        foreach (DayOfWeek gun in CalismaSaati.HaftaninGunleri)
            _gunKutusu.Items.Add(CalismaSaati.GunAdi(gun));
        _acilisSaati = SaatSecici();
        _kapanisSaati = SaatSecici();
        Button saatKaydet = KucukButon("Kaydet", birincil: true);
        Button gunuKapat = KucukButon("Günü Kapat", birincil: false);
        saatKaydet.Click += (_, _) => CalismaSaatiKaydet();
        gunuKapat.Click += (_, _) => GunuKapat();

        _saatAraclari = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        _saatAraclari.Controls.Add(_gunKutusu);
        _saatAraclari.Controls.Add(_acilisSaati);
        _saatAraclari.Controls.Add(new Label { Text = "–", AutoSize = true, Margin = new Padding(0, 10, 6, 0) });
        _saatAraclari.Controls.Add(_kapanisSaati);
        _saatAraclari.Controls.Add(saatKaydet);
        _saatAraclari.Controls.Add(gunuKapat);

        _gunKutusu.SelectedIndexChanged += (_, _) => SeciliGununSaatleriniYukle();
        _saatListesi.SelectedIndexChanged += (_, _) =>
        {
            if (_saatListesi.SelectedItem is CalismaSaati saat)
                _gunKutusu.SelectedIndex = CalismaSaati.GunSirasi(saat.Gun);
        };

        // ---- Bölüm araçları: [bölüm adı] [Ekle] [Seçiliyi Sil] ----
        _bolumAdiKutusu = new TextBox { Width = 200, Margin = new Padding(0, 8, 6, 0), PlaceholderText = "Yeni bölüm adı" };
        Button bolumEkle = KucukButon("Ekle", birincil: true);
        Button bolumSil = KucukButon("Seçiliyi Sil", birincil: false);
        bolumEkle.Click += (_, _) => BolumEkle();
        bolumSil.Click += (_, _) => BolumSil();

        _bolumAraclari = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        _bolumAraclari.Controls.Add(_bolumAdiKutusu);
        _bolumAraclari.Controls.Add(bolumEkle);
        _bolumAraclari.Controls.Add(bolumSil);

        detay.Controls.Add(_secilenBaslik, 0, 0);
        detay.Controls.Add(_secilenBilgi, 0, 1);
        detay.Controls.Add(Tema.BolumBasligi("Çalışma Saatleri"), 0, 2);
        detay.Controls.Add(_saatListesi, 0, 3);
        detay.Controls.Add(_saatAraclari, 0, 4);
        detay.Controls.Add(Tema.BolumBasligi("Bölümler"), 0, 5);
        detay.Controls.Add(_bolumListesi, 0, 6);
        detay.Controls.Add(_bolumAraclari, 0, 7);
        detay.Controls.Add(Tema.BolumBasligi("Hizmetler  (Hizmetler ekranından yönetilir)"), 0, 8);
        detay.Controls.Add(_hizmetListesi, 0, 9);
        yerlesim.Controls.Add(KartIcineAl(detay), 1, 0);

        Controls.Add(yerlesim);

        _gunKutusu.SelectedIndex = 0;
        AgaciDoldur(seciliSubeId: null);
    }

    // =====================================================================
    // Ağaç ve seçim
    // =====================================================================

    /// <summary>Ağacı yeniden doldurur; verilen şubeyi (yoksa ilk şubeyi) seçili yapar.</summary>
    private void AgaciDoldur(Guid? seciliSubeId)
    {
        _agac.Nodes.Clear();
        TreeNode? secilecekDugum = null;

        foreach (Kurum kurum in _veri.Kurumlar)
        {
            // Tag özelliğine nesnenin kendisini koyuyoruz; seçildiğinde geri alacağız.
            var kurumDugumu = new TreeNode($"{kurum.Ad}  ({kurum.TurAdi})") { Tag = kurum };
            foreach (Sube sube in kurum.Subeler)
            {
                var subeDugumu = new TreeNode(sube.Ad) { Tag = sube };
                kurumDugumu.Nodes.Add(subeDugumu);

                if (seciliSubeId is null)
                    secilecekDugum ??= subeDugumu;
                else if (sube.Id == seciliSubeId)
                    secilecekDugum = subeDugumu;
            }
            _agac.Nodes.Add(kurumDugumu);
        }

        _agac.ExpandAll();
        if (secilecekDugum is not null)
        {
            _agac.SelectedNode = secilecekDugum;
            SecimiGoster(secilecekDugum.Tag);
        }
        else
        {
            SecimiGoster(null);
        }
    }

    /// <summary>
    /// Seçilen düğümün Tag'i Kurum mu Sube mi? "switch" ile türüne göre farklı gösterim yapılır.
    /// </summary>
    private void SecimiGoster(object? secilen)
    {
        _saatListesi.Items.Clear();
        _bolumListesi.Items.Clear();
        _hizmetListesi.Items.Clear();
        _seciliSube = null;
        _seciliKurum = null;

        switch (secilen)
        {
            case Sube sube:
                _seciliSube = sube;
                _seciliKurum = _veri.SubeninKurumu(sube.Id);
                _secilenBaslik.Text = sube.Ad;
                _secilenBilgi.Text = $"{_seciliKurum?.Ad} · {sube.Adres}";

                foreach (CalismaSaati saat in sube.CalismaSaatleri)
                    _saatListesi.Items.Add(saat);
                foreach (Bolum bolum in sube.Bolumler)
                    _bolumListesi.Items.Add(bolum);

                if (sube.Hizmetler.Count == 0)
                    _hizmetListesi.Items.Add("Hizmet tanımlanmamış");
                foreach (SaglikHizmeti hizmet in sube.Hizmetler)
                    _hizmetListesi.Items.Add(hizmet); // "Ad (süre, ücret)" şeklinde görünür

                SeciliGununSaatleriniYukle();
                break;

            case Kurum kurum:
                _seciliKurum = kurum;
                _secilenBaslik.Text = kurum.Ad;
                _secilenBilgi.Text = $"{kurum.TurAdi} · {kurum.Subeler.Count} şube" +
                                     (kurum.CokSubeliMi ? " (çok şubeli)" : " (tek şubeli)");
                _hizmetListesi.Items.Add("Saat, bölüm ve hizmetleri görmek için soldan bir şube seçin.");
                break;

            default:
                _secilenBaslik.Text = "Kurum yok";
                _secilenBilgi.Text = "Yeni bir kurum ekleyin.";
                break;
        }

        // Şube seçili değilse düzenleme araçları kullanılamaz.
        _saatAraclari.Enabled = _seciliSube is not null;
        _bolumAraclari.Enabled = _seciliSube is not null;
    }

    // =====================================================================
    // Kurum ve şube ekleme (sadece Sistem Yöneticisi)
    // =====================================================================

    private void YeniKurum()
    {
        using var form = new KurumFormu(_veri);
        if (form.ShowDialog(this) != DialogResult.OK || form.OlusturulanKurum is null)
            return;

        Sube ilkSube = form.OlusturulanKurum.Subeler[0];
        AgaciDoldur(ilkSube.Id);
        Tema.BilgiGoster($"\"{form.OlusturulanKurum.Ad}\" eklendi.\n\n" +
                         "Şimdi şubenin çalışma saatlerini ve bölümlerini tanımlayın, " +
                         "ardından Hizmetler ekranından hizmet ekleyin.");
    }

    private void YeniSube()
    {
        if (_seciliKurum is not { } kurum)
        {
            Tema.HataGoster("Şube eklemek için soldan bir kurum (veya o kurumun bir şubesini) seçin.");
            return;
        }

        using var form = new SubeFormu(kurum);
        if (form.ShowDialog(this) != DialogResult.OK || form.OlusturulanSube is null)
            return;

        AgaciDoldur(form.OlusturulanSube.Id);
        Tema.BilgiGoster($"\"{form.OlusturulanSube.Ad}\" şubesi {kurum.Ad} kurumuna eklendi.\n\n" +
                         "Şimdi şubenin çalışma saatlerini ve bölümlerini tanımlayın.");
    }

    // =====================================================================
    // Çalışma saatleri
    // =====================================================================

    private DayOfWeek SeciliGun => CalismaSaati.HaftaninGunleri[Math.Max(0, _gunKutusu.SelectedIndex)];

    /// <summary>Seçilen günün mevcut saatlerini kutulara yazar (o gün kapalıysa 09:00 - 17:00 önerir).</summary>
    private void SeciliGununSaatleriniYukle()
    {
        CalismaSaati? saat = _seciliSube?.CalismaSaatiGetir(SeciliGun);
        TimeOnly acilis = saat?.Acilis ?? new TimeOnly(9, 0);
        TimeOnly kapanis = saat?.Kapanis ?? new TimeOnly(17, 0);
        _acilisSaati.Value = DateTime.Today.Add(acilis.ToTimeSpan());
        _kapanisSaati.Value = DateTime.Today.Add(kapanis.ToTimeSpan());
    }

    private void CalismaSaatiKaydet()
    {
        if (_seciliSube is not { } sube)
            return;

        try
        {
            var acilis = new TimeOnly(_acilisSaati.Value.Hour, _acilisSaati.Value.Minute);
            var kapanis = new TimeOnly(_kapanisSaati.Value.Hour, _kapanisSaati.Value.Minute);
            sube.CalismaSaatiBelirle(SeciliGun, acilis, kapanis); // kapanış açılıştan önceyse hata fırlar

            int silinen = CalisanUygunluklariniDuzelt(sube);
            AgaciDoldur(sube.Id);
            Tema.BilgiGoster($"{CalismaSaati.GunAdi(SeciliGun)} çalışma saati {acilis:HH:mm} - {kapanis:HH:mm} olarak kaydedildi." +
                             UygunlukUyarisi(silinen));
        }
        catch (ArgumentException hata)
        {
            Tema.HataGoster(hata.Message);
        }
    }

    private void GunuKapat()
    {
        if (_seciliSube is not { } sube)
            return;

        string gunAdi = CalismaSaati.GunAdi(SeciliGun);
        if (sube.CalismaSaatiGetir(SeciliGun) is null)
        {
            Tema.HataGoster($"{sube.Ad} zaten {gunAdi} günleri kapalı.");
            return;
        }

        DialogResult cevap = MessageBox.Show(
            $"{sube.Ad}, {gunAdi} günleri kapalı olarak işaretlensin mi?",
            "Günü Kapat", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (cevap != DialogResult.Yes)
            return;

        sube.CalismaGunuKaldir(SeciliGun);
        int silinen = CalisanUygunluklariniDuzelt(sube);
        AgaciDoldur(sube.Id);
        Tema.BilgiGoster($"{sube.Ad} artık {gunAdi} günleri kapalı." + UygunlukUyarisi(silinen));
    }

    /// <summary>
    /// Şubenin saatleri değişince, bu şubedeki çalışanların yeni saatlere uymayan uygunluk
    /// dilimlerini temizler (Calisan.SubeyeAta bu temizliği zaten yapıyor). Silinen dilim sayısını döndürür.
    /// </summary>
    private int CalisanUygunluklariniDuzelt(Sube sube)
    {
        List<Calisan> subeCalisanlari = _veri.Calisanlar.Where(c => c.SubeId == sube.Id).ToList();
        int onceki = subeCalisanlari.Sum(c => c.UygunlukDilimleri.Count);

        foreach (Calisan calisan in subeCalisanlari)
            calisan.SubeyeAta(sube);

        int sonraki = subeCalisanlari.Sum(c => c.UygunlukDilimleri.Count);
        return onceki - sonraki;
    }

    private static string UygunlukUyarisi(int silinenDilim) =>
        silinenDilim > 0
            ? $"\n\nYeni saatlerin dışında kalan {silinenDilim} çalışan uygunluk dilimi silindi."
            : "";

    // =====================================================================
    // Bölümler
    // =====================================================================

    private void BolumEkle()
    {
        if (_seciliSube is not { } sube)
            return;

        try
        {
            Bolum bolum = sube.BolumEkle(_bolumAdiKutusu.Text); // boşsa veya aynı isim varsa hata fırlar
            _bolumAdiKutusu.Clear();
            AgaciDoldur(sube.Id);
            Tema.BilgiGoster($"\"{bolum.Ad}\" bölümü eklendi.");
        }
        catch (Exception hata) when (hata is ArgumentException or InvalidOperationException)
        {
            Tema.HataGoster(hata.Message);
        }
    }

    private void BolumSil()
    {
        if (_seciliSube is not { } sube)
            return;
        if (_bolumListesi.SelectedItem is not Bolum bolum)
        {
            Tema.HataGoster("Silmek için listeden bir bölüm seçin.");
            return;
        }

        sube.BolumKaldir(bolum.Id);
        AgaciDoldur(sube.Id);
        Tema.BilgiGoster($"\"{bolum.Ad}\" bölümü silindi.");
    }

    // =====================================================================
    // Yardımcılar
    // =====================================================================

    private static ListBox YeniListe(bool secilebilir) => new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None,
        IntegralHeight = false,
        Font = new Font("Segoe UI", 11f),
        SelectionMode = secilebilir ? SelectionMode.One : SelectionMode.None
    };

    private static DateTimePicker SaatSecici() => new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "HH:mm",
        ShowUpDown = true,
        Width = 72,
        Value = DateTime.Today.AddHours(9),
        Margin = new Padding(0, 6, 6, 0)
    };

    private static Button KucukButon(string metin, bool birincil)
    {
        Button buton = Tema.Buton(metin, birincil);
        buton.MinimumSize = new Size(80, 32);
        buton.Margin = new Padding(0, 4, 6, 0);
        return buton;
    }

    /// <summary>Kontrolü beyaz arkaplanlı, kenar boşluklu bir "kart" panelin içine koyar.</summary>
    private static Panel KartIcineAl(Control icerik)
    {
        var kart = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Tema.Kart,
            Padding = new Padding(16),
            Margin = new Padding(0)
        };
        icerik.Dock = DockStyle.Fill;
        kart.Controls.Add(icerik);
        return kart;
    }
}
