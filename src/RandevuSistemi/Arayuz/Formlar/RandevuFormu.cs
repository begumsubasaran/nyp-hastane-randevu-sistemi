using RandevuSistemi.Models.Hizmetler;
using RandevuSistemi.Models.Kullanicilar;
using RandevuSistemi.Models.Kurumlar;
using RandevuSistemi.Models.Randevular;
using RandevuSistemi.Servisler;
using RandevuSistemi.Veri;

namespace RandevuSistemi.Arayuz.Formlar;

/// <summary>
/// Yeni randevu oluşturma penceresi. İki şekilde çalışır:
///  - HASTA kendisi randevu alırken: Şube → BÖLÜM → Doktor → İşlem → Tarih → Saat.
///    İşlem listesinde sadece seçilen doktorun sunduğu hizmetler vardır; muayene varsa o otomatik seçilir.
///    (Laboratuvar doktoru gibi muayenesi olmayanlarda ilk hizmeti seçilir: Kan Tahlili gibi.)
///  - YÖNETİCİ hasta adına randevu oluştururken: Hasta → Şube → İşlem (hizmet) → Doktor/Personel → Tarih → Saat.
///    Böylece tetkik/görüntüleme gibi işlemler (EKG, MR, kan tahlili) için de randevu verilebilir.
/// Her seçim bir sonraki listeyi süzer. Saat listesinde çalışanın uygunluk dilimleri
/// hizmet süresine göre bölünerek gösterilir.
/// Tüm kurallar (çakışma, uygunluk...) RandevuServisi'nde kontrol edilir.
/// </summary>
public class RandevuFormu : Form
{
    private readonly UygulamaVerisi _veri;
    private readonly RandevuServisi _servis;

    private readonly ComboBox _hastaKutusu;
    private readonly ComboBox _subeKutusu;
    private readonly ComboBox _hizmetKutusu;
    private readonly ComboBox _bolumKutusu;
    private readonly ComboBox _calisanKutusu;
    private readonly DateTimePicker _tarihSecici;
    private readonly ListBox _saatListesi;
    private readonly Label _bilgi;

    /// <summary>true: hasta bölüm seçerek randevu alıyor; false: yönetici işlem (hizmet) seçiyor.</summary>
    private readonly bool _bolumIle;

    /// <summary>Çalışanı açılır kutuda "Ad Soyad - Unvan" şeklinde göstermek için.</summary>
    private sealed record CalisanSecenegi(Calisan Calisan)
    {
        public override string ToString() => $"{Calisan.TamAd}  -  {Calisan.Unvan}";
    }

    /// <param name="sabitHasta">
    /// Doluysa (hasta kendisi giriş yaptıysa) hasta seçimi bu kişiye kilitlenir.
    /// Boşsa (yönetici) tüm hastalar arasından seçim yapılabilir.
    /// </param>
    public RandevuFormu(UygulamaVerisi veri, Hasta? sabitHasta = null)
    {
        _veri = veri;
        _servis = new RandevuServisi(veri);
        _bolumIle = sabitHasta is not null;

        Text = "Yeni Randevu";
        ClientSize = new Size(640, _bolumIle ? 680 : 640);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = Tema.Normal;
        BackColor = Tema.Kart;

        // Hasta modunda bir satır fazla: Bölüm + Doktor + İşlem
        int seciciSatiri = _bolumIle ? 6 : 5;
        var tablo = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = seciciSatiri + 3, Padding = new Padding(20) };
        tablo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        tablo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < seciciSatiri; i++)
            tablo.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        tablo.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // saat listesi
        tablo.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));  // bilgi
        tablo.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // butonlar

        _hastaKutusu = Kutu();
        _hastaKutusu.DisplayMember = nameof(Kullanici.TamAd); // ekranda TamAd property'si görünsün
        if (sabitHasta is not null)
        {
            _hastaKutusu.Items.Add(sabitHasta);
            _hastaKutusu.Enabled = false;
        }
        else
        {
            foreach (Hasta hasta in _veri.Hastalar)
                _hastaKutusu.Items.Add(hasta);
        }

        _subeKutusu = Kutu();
        foreach (SubeSecenegi secenek in SubeSecenegi.Tumu(_veri))
            _subeKutusu.Items.Add(secenek);

        _hizmetKutusu = Kutu();
        _bolumKutusu = Kutu();
        _calisanKutusu = Kutu();

        _tarihSecici = new DateTimePicker
        {
            Dock = DockStyle.Fill,
            Format = DateTimePickerFormat.Long,
            MinDate = DateTime.Today
        };

        _saatListesi = new ListBox
        {
            Dock = DockStyle.Fill,
            IntegralHeight = false,
            Font = new Font("Segoe UI", 10.5f),
            BorderStyle = BorderStyle.FixedSingle
        };

        _bilgi = new Label { Dock = DockStyle.Fill, ForeColor = Tema.SolukYazi, TextAlign = ContentAlignment.MiddleLeft };

        tablo.Controls.Add(Etiket("Hasta"), 0, 0);
        tablo.Controls.Add(_hastaKutusu, 1, 0);
        tablo.Controls.Add(Etiket("Şube"), 0, 1);
        tablo.Controls.Add(_subeKutusu, 1, 1);
        int satir = 2;
        if (_bolumIle)
        {
            // Hasta: Bölüm → Doktor → İşlem (doktorun hizmetleri)
            tablo.Controls.Add(Etiket("Bölüm"), 0, satir);
            tablo.Controls.Add(_bolumKutusu, 1, satir++);
            tablo.Controls.Add(Etiket("Doktor"), 0, satir);
            tablo.Controls.Add(_calisanKutusu, 1, satir++);
            tablo.Controls.Add(Etiket("İşlem"), 0, satir);
            tablo.Controls.Add(_hizmetKutusu, 1, satir++);
        }
        else
        {
            // Yönetici: İşlem → Doktor/Personel (işlemi sunanlar)
            tablo.Controls.Add(Etiket("İşlem"), 0, satir);
            tablo.Controls.Add(_hizmetKutusu, 1, satir++);
            tablo.Controls.Add(Etiket("Doktor / Personel"), 0, satir);
            tablo.Controls.Add(_calisanKutusu, 1, satir++);
        }
        tablo.Controls.Add(Etiket("Tarih"), 0, satir);
        tablo.Controls.Add(_tarihSecici, 1, satir++);
        tablo.Controls.Add(Etiket("Saat"), 0, satir);
        tablo.Controls.Add(_saatListesi, 1, satir++);
        tablo.Controls.Add(_bilgi, 0, satir);
        tablo.SetColumnSpan(_bilgi, 2);
        satir++;

        var butonlar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 10, 0, 0) };
        Button olustur = Tema.Buton("Randevu Oluştur");
        Button iptal = Tema.Buton("İptal", birincil: false);
        iptal.DialogResult = DialogResult.Cancel;
        olustur.Click += (_, _) => RandevuOlustur();
        butonlar.Controls.Add(olustur);
        butonlar.Controls.Add(iptal);
        tablo.Controls.Add(butonlar, 0, satir);
        tablo.SetColumnSpan(butonlar, 2);

        Controls.Add(tablo);
        CancelButton = iptal;

        // ---- Seçimler birbirine bağlı: biri değişince sonraki liste yenilenir ----
        _hastaKutusu.SelectedIndexChanged += (_, _) => SaatleriDoldur();      // hasta çakışması için
        _subeKutusu.SelectedIndexChanged += (_, _) =>
        {
            if (_bolumIle) BolumleriDoldur();
            else HizmetleriDoldur();
        };
        _bolumKutusu.SelectedIndexChanged += (_, _) => CalisanlariDoldur();
        _hizmetKutusu.SelectedIndexChanged += (_, _) =>
        {
            if (_bolumIle) SaatleriDoldur();      // hasta: işlem en son seçilir
            else CalisanlariDoldur();             // yönetici: işleme göre çalışanlar süzülür
        };
        _calisanKutusu.SelectedIndexChanged += (_, _) =>
        {
            if (_bolumIle) DoktorunHizmetleriniDoldur();
            else SaatleriDoldur();
        };
        _tarihSecici.ValueChanged += (_, _) => SaatleriDoldur();
        _saatListesi.SelectedIndexChanged += (_, _) => BilgiyiGuncelle();

        if (_hastaKutusu.Items.Count > 0) _hastaKutusu.SelectedIndex = 0;
        if (_subeKutusu.Items.Count > 0) _subeKutusu.SelectedIndex = 0;
    }

    /// <summary>Oluşturulan randevu (form "Randevu Oluştur" ile kapandıysa dolu olur).</summary>
    public Randevu? OlusturulanRandevu { get; private set; }

    private Hasta? SeciliHasta => _hastaKutusu.SelectedItem as Hasta;
    private Sube? SeciliSube => (_subeKutusu.SelectedItem as SubeSecenegi)?.Sube;
    private Bolum? SeciliBolum => _bolumKutusu.SelectedItem as Bolum;

    private SaglikHizmeti? SeciliHizmet => _hizmetKutusu.SelectedItem as SaglikHizmeti;
    private Calisan? SeciliCalisan => (_calisanKutusu.SelectedItem as CalisanSecenegi)?.Calisan;
    private SaatSecenegi? SeciliSaat => _saatListesi.SelectedItem as SaatSecenegi;

    // =====================================================================
    // Listeleri doldurma (zincirleme)
    // =====================================================================

    private void HizmetleriDoldur()
    {
        _hizmetKutusu.Items.Clear();
        if (SeciliSube is not null)
        {
            foreach (SaglikHizmeti hizmet in SeciliSube.Hizmetler)
                _hizmetKutusu.Items.Add(hizmet);
        }

        if (_hizmetKutusu.Items.Count > 0)
            _hizmetKutusu.SelectedIndex = 0;   // bu, CalisanlariDoldur'u tetikler
        else
            CalisanlariDoldur();
    }

    /// <summary>
    /// Hasta için: seçilen doktorun sunduğu hizmetler. Muayene hizmetleri listenin başındadır,
    /// böylece muayenesi olan doktorda muayene otomatik seçili gelir.
    /// </summary>
    private void DoktorunHizmetleriniDoldur()
    {
        _hizmetKutusu.Items.Clear();
        if (SeciliCalisan is { } doktor)
        {
            foreach (SaglikHizmeti hizmet in _servis.SunduguHizmetler(doktor))
                _hizmetKutusu.Items.Add(hizmet);
        }

        if (_hizmetKutusu.Items.Count > 0)
            _hizmetKutusu.SelectedIndex = 0;   // bu, SaatleriDoldur'u tetikler
        else
            SaatleriDoldur();
    }

    /// <summary>
    /// Hasta için: seçilen şubenin bölümleri (Kardiyoloji, Dahiliye...).
    /// Sadece randevu veren en az bir doktoru olan bölümler listelenir
    /// (ör. doktoru olmayan Laboratuvar bölümü hastaya gösterilmez).
    /// </summary>
    private void BolumleriDoldur()
    {
        _bolumKutusu.Items.Clear();
        if (SeciliSube is { } sube)
        {
            foreach (Bolum bolum in sube.Bolumler.Where(b => _servis.BolumDoktorlari(sube, b).Count > 0))
                _bolumKutusu.Items.Add(bolum);
        }

        if (_bolumKutusu.Items.Count > 0)
            _bolumKutusu.SelectedIndex = 0;   // bu, CalisanlariDoldur'u tetikler
        else
            CalisanlariDoldur();
    }

    /// <summary>
    /// Hasta için: seçilen bölümün doktorları. Yönetici için: seçilen şubede çalışan,
    /// aktif ve seçilen hizmeti sunan kişiler.
    /// </summary>
    private void CalisanlariDoldur()
    {
        _calisanKutusu.Items.Clear();
        if (_bolumIle)
        {
            if (SeciliSube is { } sube && SeciliBolum is { } bolum)
            {
                foreach (Doktor doktor in _servis.BolumDoktorlari(sube, bolum))
                    _calisanKutusu.Items.Add(new CalisanSecenegi(doktor));
            }
        }
        else if (SeciliSube is { } sube && SeciliHizmet is { } hizmet)
        {
            IEnumerable<Calisan> uygunlar = _veri.Calisanlar.Where(c =>
                c.AktifMi && c.SubeId == sube.Id && c.HizmetSunuyorMu(hizmet.Id));

            foreach (Calisan calisan in uygunlar)
                _calisanKutusu.Items.Add(new CalisanSecenegi(calisan));
        }

        if (_calisanKutusu.Items.Count > 0)
            _calisanKutusu.SelectedIndex = 0;  // bu, sonraki listeyi doldurur
        else if (_bolumIle)
            DoktorunHizmetleriniDoldur();      // doktor yok → işlem listesini de boşalt
        else
            SaatleriDoldur();
    }

    private void SaatleriDoldur()
    {
        _saatListesi.Items.Clear();
        _bilgi.ForeColor = Tema.SolukYazi;

        if (SeciliCalisan is null || SeciliHizmet is null)
        {
            if (_bolumIle)
                _bilgi.Text = SeciliBolum is null
                    ? "Bu şubede şu anda randevu verilen bir bölüm yok. Başka bir şube seçin."
                    : $"{SeciliBolum.Ad} bölümünde şu anda randevu veren doktor bulunmuyor.";
            else
                _bilgi.Text = SeciliHizmet is null
                    ? "Bu şubede tanımlı hizmet yok."
                    : "Bu işlemi sunan bir doktor / personel bulunamadı.";
            return;
        }

        DateOnly gun = DateOnly.FromDateTime(_tarihSecici.Value);
        List<SaatSecenegi> secenekler = _servis.SaatSecenekleri(SeciliHasta, SeciliCalisan, SeciliHizmet, gun);

        foreach (SaatSecenegi secenek in secenekler)
            _saatListesi.Items.Add(secenek);

        if (secenekler.Count == 0)
        {
            // O gün uygunluk yoksa, çalışanın hangi günler uygun olduğunu söyleyelim.
            string gunler = string.Join(", ", SeciliCalisan.UygunlukDilimleri
                .Select(d => CalismaSaati.GunAdi(d.Gun)).Distinct());
            _bilgi.Text = string.IsNullOrEmpty(gunler)
                ? $"{SeciliCalisan.TamAd} için henüz uygunluk saati tanımlanmamış."
                : $"{SeciliCalisan.TamAd} bu gün randevu vermiyor. Uygun günler: {gunler}.";
            return;
        }

        int musaitSayisi = secenekler.Count(s => s.Musait);
        _bilgi.Text = $"{musaitSayisi} uygun saat var. Bir saat seçin.";
    }

    /// <summary>Seçilen saate göre özet bilgiyi veya uygun değilse nedenini gösterir.</summary>
    private void BilgiyiGuncelle()
    {
        if (SeciliSaat is null || SeciliHizmet is null || SeciliCalisan is null)
            return;

        if (SeciliSaat.Musait)
        {
            _bilgi.ForeColor = Tema.Yazi;
            _bilgi.Text = $"{SeciliHizmet.Ad}  ·  {SeciliHizmet.SureDakika} dk  ·  {SeciliHizmet.UcretMetni}\n" +
                          $"{SeciliCalisan.TamAd} ({SeciliCalisan.Unvan})  ·  " +
                          $"{SeciliSaat.Baslangic:dd.MM.yyyy} {SeciliSaat.Baslangic:HH:mm} - {SeciliSaat.Bitis:HH:mm}";
        }
        else
        {
            _bilgi.ForeColor = Tema.DurumRengi(RandevuDurumu.Reddedildi);
            _bilgi.Text = SeciliSaat.Neden;
        }
    }

    // =====================================================================
    // Randevu oluşturma
    // =====================================================================

    private void RandevuOlustur()
    {
        if (SeciliHasta is null || SeciliHizmet is null || SeciliCalisan is null || SeciliSaat is null)
        {
            Tema.HataGoster(_bolumIle
                ? "Lütfen bölüm, doktor, işlem ve saat seçin."
                : "Lütfen hasta, işlem, doktor/personel ve saat seçin.");
            return;
        }

        try
        {
            // Saat "uygun değil" olarak işaretli olsa bile servise gönderiyoruz:
            // kuralları kontrol eden asıl yer servis; ihlal varsa hata fırlatır.
            OlusturulanRandevu = _servis.RandevuOlustur(SeciliHasta, SeciliCalisan, SeciliHizmet, SeciliSaat.Baslangic);

            Tema.BilgiGoster(
                "Randevu oluşturuldu.\n\n" +
                $"{OlusturulanRandevu.Baslangic:dd.MM.yyyy HH:mm} - {OlusturulanRandevu.Bitis:HH:mm}\n" +
                $"{OlusturulanRandevu.HizmetAdi} · {OlusturulanRandevu.CalisanAdi}\n" +
                $"Ücret: {OlusturulanRandevu.UcretMetni}\n\n" +
                $"Durum: {OlusturulanRandevu.Durum.GorunenAd()}");

            DialogResult = DialogResult.OK;
        }
        catch (Exception hata) when (hata is InvalidOperationException or ArgumentException)
        {
            // RandevuKuraliException da InvalidOperationException'dan türediği için burada yakalanır.
            Tema.HataGoster(hata.Message);
            SaatleriDoldur(); // durum değişmiş olabilir, listeyi tazele
        }
    }

    // =====================================================================
    // Yardımcılar
    // =====================================================================

    private static ComboBox Kutu() => new KaydirmasizComboBox
    {
        Dock = DockStyle.Fill,
        DropDownStyle = ComboBoxStyle.DropDownList
    };

    private static Label Etiket(string metin) => new()
    {
        Text = metin,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        ForeColor = Tema.SolukYazi
    };
}
