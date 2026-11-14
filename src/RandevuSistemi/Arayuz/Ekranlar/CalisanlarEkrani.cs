using RandevuSistemi.Arayuz.Formlar;
using RandevuSistemi.Models.Hizmetler;
using RandevuSistemi.Models.Kullanicilar;
using RandevuSistemi.Models.Kurumlar;
using RandevuSistemi.Models.Randevular;
using RandevuSistemi.Servisler;
using RandevuSistemi.Veri;

namespace RandevuSistemi.Arayuz.Ekranlar;

/// <summary>
/// Doktor ve sağlık personelini listeler. Seçilen çalışanın:
/// - çalıştığı şubesi değiştirilebilir,
/// - sunduğu hizmetler işaretlenerek belirlenebilir,
/// - haftalık uygunluk saatleri (randevu verebildiği zaman dilimleri) eklenebilir, düzenlenebilir, silinebilir.
///
/// Doktor/personel giriş yaptığında ("sadeceBuCalisan" dolu) sadece kendi bilgileri görünür;
/// şubesini ve hizmetlerini değiştiremez (bunu yönetici yapar), uygunluk saatlerini düzenleyebilir.
/// </summary>
public class CalisanlarEkrani : UserControl
{
    private readonly UygulamaVerisi _veri;
    private readonly DataGridView _tablo;
    private readonly Label _seciliBaslik;
    private readonly Label _seciliUnvan;
    private readonly ComboBox _subeKutusu;

    // "Sunduğu Hizmetler" sekmesi
    private readonly CheckedListBox _hizmetListesi;
    private readonly Label _hizmetNotu;

    // "Uygunluk Saatleri" sekmesi
    private readonly ComboBox _gunKutusu;
    private readonly DateTimePicker _basSaat;
    private readonly DateTimePicker _bitSaat;
    private readonly ListBox _dilimListesi;
    private readonly Label _uygunlukNotu;

    private readonly Calisan? _sadeceBuCalisan;
    private readonly Kullanici _aktifKullanici;
    private readonly Button _aktiflikButonu;
    private Calisan? _seciliCalisan;
    private bool _yukleniyor; // ekran doldurulurken olayların (event) tetiklenmesini engellemek için

    public CalisanlarEkrani(UygulamaVerisi veri, Kullanici aktifKullanici, Calisan? sadeceBuCalisan = null)
    {
        _veri = veri;
        _aktifKullanici = aktifKullanici;
        _sadeceBuCalisan = sadeceBuCalisan;
        BackColor = Tema.Arkaplan;

        var yerlesim = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        yerlesim.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        yerlesim.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));

        // ================= Sol: çalışan tablosu =================
        _tablo = new DataGridView { Margin = new Padding(0, 0, 12, 0) };
        Tema.TabloyuBicimlendir(_tablo);
        _tablo.Columns.Add("ad", "Ad Soyad");
        _tablo.Columns.Add("unvan", "Unvan / Görev");
        _tablo.Columns.Add("sube", "Şube");
        _tablo.Columns.Add("hizmet", "Hizmet");
        _tablo.Columns.Add("uygunluk", "Uygunluk");
        _tablo.Columns["ad"]!.FillWeight = 22;
        _tablo.Columns["unvan"]!.FillWeight = 28;
        _tablo.Columns["sube"]!.FillWeight = 28;
        _tablo.Columns["hizmet"]!.FillWeight = 10;
        _tablo.Columns["uygunluk"]!.FillWeight = 12;
        _tablo.SelectionChanged += (_, _) =>
        {
            if (_tablo.CurrentRow?.Tag is Calisan calisan)
                DetayGoster(calisan);
        };

        // Tablonun üstünde: yeni çalışan kaydı ve pasife alma (sadece yönetici görünümünde)
        var solTaraf = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0, 0, 12, 0) };
        solTaraf.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        solTaraf.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var solButonlar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        _aktiflikButonu = Tema.Buton("Pasife Al", birincil: false);
        _aktiflikButonu.Click += (_, _) => AktiflikDegistir();
        if (_sadeceBuCalisan is null)
        {
            Button yeniButon = Tema.Buton("+ Yeni Çalışan");
            yeniButon.Click += (_, _) => YeniCalisan();
            solButonlar.Controls.Add(yeniButon);
            solButonlar.Controls.Add(_aktiflikButonu);
        }
        solTaraf.Controls.Add(solButonlar, 0, 0);

        _tablo.Margin = new Padding(0);
        solTaraf.Controls.Add(Tema.KartaKoy(_tablo), 0, 1);
        yerlesim.Controls.Add(solTaraf, 0, 0);

        // ================= Sağ: seçili çalışanın ayrıntıları =================
        var detay = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Tema.Kart,
            Padding = new Padding(16)
        };
        detay.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));  // ad
        detay.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));  // unvan
        detay.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));  // "Çalıştığı şube"
        detay.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));  // şube kutusu
        detay.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // sekmeler

        _seciliBaslik = new Label { Dock = DockStyle.Fill, Font = Tema.AltBaslik, ForeColor = Tema.Yazi };
        _seciliUnvan = new Label { Dock = DockStyle.Fill, ForeColor = Tema.SolukYazi };

        // Tekerlekle yanlışlıkla şube değişmesin diye özel ComboBox kullanıyoruz.
        _subeKutusu = new KaydirmasizComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        foreach (SubeSecenegi secenek in SubeSecenegi.Tumu(_veri))
            _subeKutusu.Items.Add(secenek);
        _subeKutusu.SelectedIndexChanged += (_, _) => SubeDegisti();

        // ---- Sekme 1: Sunduğu hizmetler ----
        _hizmetListesi = new CheckedListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            CheckOnClick = true,
            IntegralHeight = false,
            Font = new Font("Segoe UI", 10.5f)
        };
        _hizmetListesi.ItemCheck += HizmetIsaretlendi;
        _hizmetNotu = NotEtiketi();

        var hizmetSayfasi = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        hizmetSayfasi.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        hizmetSayfasi.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        hizmetSayfasi.Controls.Add(_hizmetListesi, 0, 0);
        hizmetSayfasi.Controls.Add(_hizmetNotu, 0, 1);

        // ---- Sekme 2: Uygunluk saatleri ----
        _gunKutusu = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 110,
            Margin = new Padding(0, 4, 6, 0)
        };
        foreach (DayOfWeek gun in CalismaSaati.HaftaninGunleri)
            _gunKutusu.Items.Add(CalismaSaati.GunAdi(gun));
        _gunKutusu.SelectedIndexChanged += (_, _) => SaatleriSubeyeGoreAyarla();

        _basSaat = SaatSecici();
        _bitSaat = SaatSecici();

        Button ekleButon = Tema.Buton("Ekle");
        ekleButon.MinimumSize = new Size(70, 32);
        ekleButon.Click += (_, _) => DilimEkle();

        Button guncelleButon = Tema.Buton("Güncelle", birincil: false);
        guncelleButon.MinimumSize = new Size(90, 32);
        guncelleButon.Click += (_, _) => DilimGuncelle();

        Button silButon = Tema.Buton("Seçiliyi Sil", birincil: false);
        silButon.MinimumSize = new Size(90, 32);
        silButon.Click += (_, _) => DilimSil();

        var ekleSatiri = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true };
        ekleSatiri.Controls.Add(_gunKutusu);
        ekleSatiri.Controls.Add(_basSaat);
        ekleSatiri.Controls.Add(new Label { Text = "–", AutoSize = true, Margin = new Padding(0, 7, 6, 0) });
        ekleSatiri.Controls.Add(_bitSaat);
        ekleSatiri.Controls.Add(ekleButon);
        ekleSatiri.Controls.Add(guncelleButon);
        ekleSatiri.Controls.Add(silButon);

        _dilimListesi = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            IntegralHeight = false,
            Font = new Font("Segoe UI", 10.5f)
        };
        // Listeden bir dilim seçilince değerleri düzenlenmek üzere üstteki kutulara yüklenir.
        _dilimListesi.SelectedIndexChanged += (_, _) => SeciliDilimiKutularaYukle();
        _uygunlukNotu = NotEtiketi();

        var uygunlukSayfasi = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        uygunlukSayfasi.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));   // ekleme satırı (sığmazsa 2 satıra kayar)
        uygunlukSayfasi.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // dilim listesi
        uygunlukSayfasi.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));   // not
        uygunlukSayfasi.Controls.Add(ekleSatiri, 0, 0);
        uygunlukSayfasi.Controls.Add(_dilimListesi, 0, 1);
        uygunlukSayfasi.Controls.Add(_uygunlukNotu, 0, 2);

        // ---- Sekmeler ----
        var sekmeler = new TabControl { Dock = DockStyle.Fill, Padding = new Point(14, 6) };
        sekmeler.TabPages.Add(Sekme("Sunduğu Hizmetler", hizmetSayfasi));
        sekmeler.TabPages.Add(Sekme("Uygunluk Saatleri", uygunlukSayfasi));

        detay.Controls.Add(_seciliBaslik, 0, 0);
        detay.Controls.Add(_seciliUnvan, 0, 1);
        detay.Controls.Add(Tema.BolumBasligi("Çalıştığı şube"), 0, 2);
        detay.Controls.Add(_subeKutusu, 0, 3);
        detay.Controls.Add(sekmeler, 0, 4);
        yerlesim.Controls.Add(Tema.KartaKoy(detay, 6), 1, 0);

        Controls.Add(yerlesim);

        // Doktor/personel kendi ekranında şube ve hizmetlerini değiştiremez.
        if (_sadeceBuCalisan is not null)
        {
            _subeKutusu.Enabled = false;
            _hizmetListesi.Enabled = false;
        }

        _gunKutusu.SelectedIndex = 0;
        TabloyuDoldur();
        Calisan? ilk = GosterilecekCalisanlar().FirstOrDefault();
        if (ilk is not null)
            DetayGoster(ilk);
    }

    /// <summary>Yönetici için tüm çalışanlar; doktor/personel için sadece kendisi.</summary>
    private IEnumerable<Calisan> GosterilecekCalisanlar() =>
        _sadeceBuCalisan is null ? _veri.Calisanlar : [_sadeceBuCalisan];

    // =====================================================================
    // Tablo
    // =====================================================================

    private void TabloyuDoldur()
    {
        _tablo.Rows.Clear();
        foreach (Calisan calisan in GosterilecekCalisanlar())
        {
            int satir = _tablo.Rows.Add(calisan.TamAd, "", "", "", "");
            _tablo.Rows[satir].Tag = calisan;
            SatiriGuncelle(_tablo.Rows[satir]);
        }
    }

    /// <summary>Tablodaki bir satırı çalışanın güncel bilgileriyle yeniler.</summary>
    private void SatiriGuncelle(DataGridViewRow satir)
    {
        if (satir.Tag is not Calisan calisan)
            return;

        Kurum? kurum = _veri.SubeninKurumu(calisan.SubeId);
        Sube? sube = _veri.SubeBul(calisan.SubeId);

        // ÇOK BİÇİMLİLİK: Unvan soyut bir property; Doktor ve SaglikPersoneli farklı değer döndürür.
        satir.Cells["ad"].Value = calisan.TamAd + (calisan.AktifMi ? "" : "  (pasif)");
        satir.Cells["unvan"].Value = calisan.Unvan;
        satir.Cells["sube"].Value = kurum is null || sube is null ? "— atanmamış —" : $"{kurum.Ad} / {sube.Ad}";
        satir.Cells["hizmet"].Value = calisan.HizmetIdleri.Count;
        satir.Cells["uygunluk"].Value = $"{calisan.UygunlukDilimleri.Count} dilim";
    }

    private void SeciliSatiriGuncelle()
    {
        foreach (DataGridViewRow satir in _tablo.Rows)
        {
            if (ReferenceEquals(satir.Tag, _seciliCalisan))
            {
                SatiriGuncelle(satir);
                break;
            }
        }
    }

    // =====================================================================
    // Seçili çalışanın ayrıntıları
    // =====================================================================

    private void DetayGoster(Calisan calisan)
    {
        _yukleniyor = true;
        _seciliCalisan = calisan;

        _seciliBaslik.Text = calisan.TamAd;
        _seciliUnvan.Text = $"{calisan.Rol.GorunenAd()} · {calisan.Unvan}";

        SubeKutusunuAyarla(calisan);

        HizmetListesiniDoldur();
        UygunlukListesiniDoldur();
        SaatleriSubeyeGoreAyarla();
        _aktiflikButonu.Text = calisan.AktifMi ? "Pasife Al" : "Aktif Et";
        _yukleniyor = false;
    }

    // =====================================================================
    // Çalışan kaydı ve aktif/pasif (yönetici)
    // =====================================================================

    private void YeniCalisan()
    {
        if (!_veri.Kurumlar.SelectMany(k => k.Subeler).Any())
        {
            Tema.HataGoster("Önce Kurumlar ekranından en az bir şube tanımlayın.");
            return;
        }

        using var form = new CalisanFormu(_veri);
        if (form.ShowDialog(this) != DialogResult.OK || form.OlusturulanKullanici is not Calisan yeniCalisan)
            return;

        TabloyuDoldur();
        SatiriSec(yeniCalisan);
        DetayGoster(yeniCalisan);
        Tema.BilgiGoster($"{yeniCalisan.TamAd} ({yeniCalisan.Unvan}) kaydedildi.\n\n" +
                         "Şimdi sağ taraftan sunduğu hizmetleri ve uygunluk saatlerini tanımlayın.");
    }

    /// <summary>
    /// İşten ayrılan çalışan silinmez, pasife alınır: geçmiş randevuları korunur,
    /// ama yeni randevu alınamaz ve sisteme giriş yapamaz.
    /// </summary>
    private void AktiflikDegistir()
    {
        if (_seciliCalisan is not { } calisan)
            return;

        if (!calisan.AktifMi)
        {
            calisan.AktifEt();
            SeciliSatiriGuncelle();
            DetayGoster(calisan);
            Tema.BilgiGoster($"{calisan.TamAd} tekrar aktif edildi.\n\n" +
                             "Not: Pasife alınırken iptal edilen randevular geri gelmez.");
            return;
        }

        var servis = new RandevuServisi(_veri);
        int iptalSayisi = servis.GelecekRandevulari(calisan).Count;

        DialogResult cevap = MessageBox.Show(
            $"{calisan.TamAd} pasife alınsın mı?\n\n" +
            "• Pasif çalışana yeni randevu alınamaz ve sisteme giriş yapamaz.\n" +
            $"• İleri tarihli {iptalSayisi} randevusu İPTAL EDİLECEK.\n" +
            "• Geçmiş ve tamamlanmış randevuları korunur.",
            "Pasife Al", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (cevap != DialogResult.Yes)
            return;

        try
        {
            List<Randevu> iptalEdilenler = servis.CalisaniPasifeAl(calisan, _aktifKullanici);

            SeciliSatiriGuncelle();
            DetayGoster(calisan);

            string ozet = iptalEdilenler.Count == 0
                ? "İptal edilecek ileri tarihli randevusu yoktu."
                : $"{iptalEdilenler.Count} randevu iptal edildi:\n" + string.Join("\n", iptalEdilenler
                      .Take(10)
                      .Select(r => $"• {r.Baslangic:dd.MM.yyyy HH:mm}  {r.HizmetAdi}  ({r.HastaAdi})"))
                  + (iptalEdilenler.Count > 10 ? $"\n... ve {iptalEdilenler.Count - 10} randevu daha" : "");

            Tema.BilgiGoster($"{calisan.TamAd} pasife alındı.\n\n{ozet}");
        }
        catch (Exception hata) when (hata is UnauthorizedAccessException or InvalidOperationException)
        {
            Tema.HataGoster(hata.Message);
        }
    }

    private void SatiriSec(Calisan calisan)
    {
        foreach (DataGridViewRow satir in _tablo.Rows)
        {
            if (ReferenceEquals(satir.Tag, calisan))
            {
                _tablo.CurrentCell = satir.Cells[0];
                break;
            }
        }
    }

    private void SubeDegisti()
    {
        if (_yukleniyor || _seciliCalisan is null || _subeKutusu.SelectedItem is not SubeSecenegi secenek)
            return;
        if (secenek.Sube.Id == _seciliCalisan.SubeId)
            return; // aynı şube seçildi, değişiklik yok

        Calisan calisan = _seciliCalisan;
        var servis = new RandevuServisi(_veri);

        // Değişiklikten SONRA kalacak dilimler: yeni şubenin çalışma saatlerine uyanlar.
        // Eski şubedeki ileri tarihli randevular ve yeni saatlere uymayanlar iptal edilecek.
        List<UygunlukDilimi> kalacakDilimler = calisan.SubeyeGoreDuzeltilmisDilimler(secenek.Sube);
        List<Randevu> iptalEdilecekler = servis.UygunlukDisiKalanlar(calisan, kalacakDilimler, secenek.Sube.Id);

        // Şube değişikliği bazı bilgileri sildiği için kullanıcıdan onay alıyoruz.
        if (calisan.SubeId is not null)
        {
            DialogResult cevap = MessageBox.Show(
                $"{calisan.TamAd} adlı çalışanın şubesi \"{secenek}\" olarak değiştirilsin mi?\n\n" +
                "• Yeni şubede bulunmayan hizmetler çıkarılacak; uygunluk dilimleri yeni şubenin saatlerine göre kısaltılacak veya silinecek.\n" +
                $"• İleri tarihli {iptalEdilecekler.Count} randevusu İPTAL EDİLECEK" +
                (iptalEdilecekler.Count > 0 ? $":\n\n{Tema.RandevuListesiMetni(iptalEdilecekler)}" : "."),
                "Şube Değişikliği", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (cevap != DialogResult.Yes)
            {
                SubeKutusunuAyarla(calisan); // eski şubeye geri dön
                return;
            }
        }

        // Yeni şubede geçerli olmayan hizmet ve uygunluk dilimlerini Calisan sınıfı kendisi temizler.
        calisan.SubeyeAta(secenek.Sube);
        List<Randevu> iptalEdilenler = UygunlukDisiRandevulariIptalEt(calisan, "şubesi değiştiği");

        HizmetListesiniDoldur();
        UygunlukListesiniDoldur();
        SaatleriSubeyeGoreAyarla();
        SeciliSatiriGuncelle();

        if (iptalEdilenler.Count > 0)
            Tema.BilgiGoster($"{calisan.TamAd} \"{secenek}\" şubesine taşındı.\n\n" +
                             $"{iptalEdilenler.Count} randevu iptal edildi:\n{Tema.RandevuListesiMetni(iptalEdilenler)}");
    }

    /// <summary>Şube kutusunda çalışanın mevcut şubesini (olay tetiklemeden) seçili yapar.</summary>
    private void SubeKutusunuAyarla(Calisan calisan)
    {
        bool oncekiDurum = _yukleniyor;
        _yukleniyor = true;

        _subeKutusu.SelectedIndex = -1;
        for (int i = 0; i < _subeKutusu.Items.Count; i++)
        {
            if (_subeKutusu.Items[i] is SubeSecenegi secenek && secenek.Sube.Id == calisan.SubeId)
            {
                _subeKutusu.SelectedIndex = i;
                break;
            }
        }

        _yukleniyor = oncekiDurum;
    }

    private Sube? SeciliCalisaninSubesi() =>
        _seciliCalisan is null ? null : _veri.SubeBul(_seciliCalisan.SubeId);

    // =====================================================================
    // Sekme 1: Sunduğu hizmetler
    // =====================================================================

    /// <summary>Çalışanın şubesindeki hizmetleri listeler; sunduklarını işaretli gösterir.</summary>
    private void HizmetListesiniDoldur()
    {
        bool oncekiDurum = _yukleniyor;
        _yukleniyor = true;
        _hizmetListesi.Items.Clear();

        Sube? sube = SeciliCalisaninSubesi();
        if (_seciliCalisan is null || sube is null)
        {
            _hizmetNotu.Text = "Hizmet seçebilmek için önce çalışanı bir şubeye atayın.";
        }
        else
        {
            foreach (SaglikHizmeti hizmet in sube.Hizmetler)
                _hizmetListesi.Items.Add(hizmet, _seciliCalisan.HizmetSunuyorMu(hizmet.Id));

            _hizmetNotu.Text = sube.Hizmetler.Count == 0
                ? $"{sube.Ad} için henüz hizmet tanımlanmamış (Hizmetler ekranından ekleyin)."
                : _sadeceBuCalisan is not null
                    ? "Sunduğunuz hizmetler yönetici tarafından belirlenir."
                    : "Hasta, bu çalışandan sadece işaretli hizmetler için randevu alabilir.";
        }

        _yukleniyor = oncekiDurum;
    }

    private void HizmetIsaretlendi(object? sender, ItemCheckEventArgs e)
    {
        if (_yukleniyor || _seciliCalisan is null)
            return;
        if (_hizmetListesi.Items[e.Index] is not SaglikHizmeti hizmet)
            return;

        // ItemCheck olayı işaret DEĞİŞMEDEN önce gelir; yeni durum e.NewValue'dadır.
        if (e.NewValue == CheckState.Checked)
            _seciliCalisan.HizmetEkle(hizmet);
        else
            _seciliCalisan.HizmetCikar(hizmet.Id);

        SeciliSatiriGuncelle();
    }

    // =====================================================================
    // Sekme 2: Uygunluk saatleri
    // =====================================================================

    private void UygunlukListesiniDoldur()
    {
        _dilimListesi.Items.Clear();

        Sube? sube = SeciliCalisaninSubesi();
        if (_seciliCalisan is null || sube is null)
        {
            _uygunlukNotu.Text = "Uygunluk saati ekleyebilmek için önce çalışanı bir şubeye atayın.";
            return;
        }

        foreach (UygunlukDilimi dilim in _seciliCalisan.UygunlukDilimleri)
            _dilimListesi.Items.Add(dilim);

        _uygunlukNotu.Text = _seciliCalisan.UygunlukDilimleri.Count == 0
            ? "Uygunluk saati yok: bu çalışana henüz randevu alınamaz."
            : "Düzenlemek için listeden bir dilim seçin, saatleri değiştirip Güncelle'ye basın.\n" +
              "Hastalar sadece bu zaman dilimlerinin içine randevu alabilir.";
    }

    /// <summary>Seçilen dilimin gün ve saatlerini üstteki kutulara yazar.</summary>
    private void SeciliDilimiKutularaYukle()
    {
        if (_dilimListesi.SelectedItem is not UygunlukDilimi dilim)
            return;

        // Önce gün seçilir (bu, saatleri şube saatine ayarlar), sonra dilimin kendi saatleri yazılır.
        _gunKutusu.SelectedIndex = CalismaSaati.GunSirasi(dilim.Gun);
        _basSaat.Value = DateTime.Today.Add(dilim.Baslangic.ToTimeSpan());
        _bitSaat.Value = DateTime.Today.Add(dilim.Bitis.ToTimeSpan());
    }

    /// <summary>
    /// Gün değişince saat kutularını şubenin o günkü açılış-kapanış saatine ayarlar
    /// (kullanıcıya kolaylık; istenirse değiştirilebilir).
    /// </summary>
    private void SaatleriSubeyeGoreAyarla()
    {
        if (_gunKutusu.SelectedIndex < 0)
            return;

        DayOfWeek gun = CalismaSaati.HaftaninGunleri[_gunKutusu.SelectedIndex];
        CalismaSaati? saat = SeciliCalisaninSubesi()?.CalismaSaatiGetir(gun);
        if (saat is null)
            return;

        _basSaat.Value = DateTime.Today.Add(saat.Acilis.ToTimeSpan());
        _bitSaat.Value = DateTime.Today.Add(saat.Kapanis.ToTimeSpan());
    }

    private void DilimEkle()
    {
        if (_seciliCalisan is null)
            return;

        Sube? sube = SeciliCalisaninSubesi();
        if (sube is null)
        {
            Tema.HataGoster("Önce çalışanı bir şubeye atayın.");
            return;
        }

        DayOfWeek gun = CalismaSaati.HaftaninGunleri[_gunKutusu.SelectedIndex];
        try
        {
            // Kurallar (şube saatleri, çakışma vb.) Calisan sınıfının içinde kontrol edilir.
            UygunlukDilimi yeniDilim = _seciliCalisan.UygunlukEkle(sube, gun, SaatAl(_basSaat), SaatAl(_bitSaat));
            UygunlukListesiniDoldur();
            SeciliSatiriGuncelle();
            Tema.BilgiGoster($"\"{yeniDilim}\" uygunluk dilimi eklendi.");
        }
        catch (Exception hata) when (hata is ArgumentException or InvalidOperationException)
        {
            Tema.HataGoster(hata.Message);
        }
    }

    private void DilimGuncelle()
    {
        if (_seciliCalisan is null || _dilimListesi.SelectedItem is not UygunlukDilimi eskiDilim)
        {
            Tema.HataGoster("Düzenlemek için listeden bir zaman dilimi seçin.");
            return;
        }

        Sube? sube = SeciliCalisaninSubesi();
        if (sube is null)
        {
            Tema.HataGoster("Önce çalışanı bir şubeye atayın.");
            return;
        }

        DayOfWeek gun = CalismaSaati.HaftaninGunleri[_gunKutusu.SelectedIndex];
        Calisan calisan = _seciliCalisan;
        try
        {
            // 1) Değişiklik yapılmadan önce: yeni saatlerin dışında kalacak randevuları bul ve onay iste.
            var aday = new UygunlukDilimi(gun, SaatAl(_basSaat), SaatAl(_bitSaat)); // saatler hatalıysa hata fırlar
            IEnumerable<UygunlukDilimi> yeniDilimler = calisan.UygunlukDilimleri
                .Where(d => !ReferenceEquals(d, eskiDilim))
                .Append(aday);
            List<Randevu> iptalEdilecekler = new RandevuServisi(_veri).UygunlukDisiKalanlar(calisan, yeniDilimler);

            if (!Tema.IptalOnayiAl(iptalEdilecekler, $"\"{eskiDilim}\" dilimi \"{aday}\" olarak değiştirilecek."))
                return;

            // 2) Dilimi güncelle (şube saatleri, çakışma gibi kurallar Calisan sınıfında kontrol edilir).
            UygunlukDilimi yeniDilim = calisan.UygunlukGuncelle(eskiDilim, sube, gun, aday.Baslangic, aday.Bitis);

            // 3) Yeni saatlerin dışında kalan randevuları iptal et; uyanlar korunur.
            List<Randevu> iptalEdilenler = UygunlukDisiRandevulariIptalEt(calisan, "uygunluk saatleri değiştiği");

            UygunlukListesiniDoldur();
            _dilimListesi.SelectedItem = yeniDilim; // güncellenen dilim seçili kalsın
            SeciliSatiriGuncelle();
            Tema.BilgiGoster($"Uygunluk dilimi güncellendi:\n\"{eskiDilim}\"  →  \"{yeniDilim}\"" +
                             IptalOzeti(iptalEdilenler));
        }
        catch (Exception hata) when (hata is ArgumentException or InvalidOperationException)
        {
            Tema.HataGoster(hata.Message);
        }
    }

    private void DilimSil()
    {
        if (_seciliCalisan is null || _dilimListesi.SelectedItem is not UygunlukDilimi dilim)
        {
            Tema.HataGoster("Silmek için listeden bir zaman dilimi seçin.");
            return;
        }

        Calisan calisan = _seciliCalisan;

        // Silmeden önce: bu dilim kalkınca dışarıda kalacak randevuları bul ve onay iste.
        List<Randevu> iptalEdilecekler = new RandevuServisi(_veri).UygunlukDisiKalanlar(
            calisan, calisan.UygunlukDilimleri.Where(d => !ReferenceEquals(d, dilim)));
        if (!Tema.IptalOnayiAl(iptalEdilecekler, $"\"{dilim}\" uygunluk dilimi silinecek."))
            return;

        calisan.UygunlukKaldir(dilim);
        List<Randevu> iptalEdilenler = UygunlukDisiRandevulariIptalEt(calisan, "uygunluk saatleri değiştiği");

        UygunlukListesiniDoldur();
        SeciliSatiriGuncelle();
        Tema.BilgiGoster($"\"{dilim}\" uygunluk dilimi silindi." + IptalOzeti(iptalEdilenler));
    }

    /// <summary>
    /// Uygunluk (veya şube) değiştikten sonra, yeni saatlerin dışında kalan ileri tarihli randevuları iptal eder.
    /// Yetki hatası olursa kullanıcıya gösterir ve boş liste döndürür.
    /// </summary>
    private List<Randevu> UygunlukDisiRandevulariIptalEt(Calisan calisan, string neden)
    {
        try
        {
            string sebep = $"{calisan.TamAd} adlı çalışanın {neden} için iptal edildi " +
                           $"({DateTime.Now:dd.MM.yyyy}; randevu yeni uygunluk saatlerinin dışında kaldı).";
            return new RandevuServisi(_veri).UygunlukDisiRandevulariIptalEt(calisan, _aktifKullanici, sebep);
        }
        catch (Exception hata) when (hata is UnauthorizedAccessException or InvalidOperationException)
        {
            Tema.HataGoster(hata.Message);
            return [];
        }
    }

    private static string IptalOzeti(List<Randevu> iptalEdilenler) =>
        iptalEdilenler.Count == 0
            ? ""
            : $"\n\n{iptalEdilenler.Count} randevu yeni saatlerin dışında kaldığı için iptal edildi:\n" +
              Tema.RandevuListesiMetni(iptalEdilenler);

    // =====================================================================
    // Yardımcılar
    // =====================================================================

    /// <summary>Sadece saat ve dakika seçilebilen kutu (ör. 09:30).</summary>
    private static DateTimePicker SaatSecici() => new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "HH:mm",
        ShowUpDown = true,
        Width = 72,
        Value = DateTime.Today.AddHours(9),
        Margin = new Padding(0, 4, 6, 0)
    };

    /// <summary>DateTimePicker'dan saniyesiz TimeOnly değeri alır.</summary>
    private static TimeOnly SaatAl(DateTimePicker secici) => new(secici.Value.Hour, secici.Value.Minute);

    private static Label NotEtiketi() => new()
    {
        Dock = DockStyle.Fill,
        ForeColor = Tema.SolukYazi,
        Font = new Font("Segoe UI", 9f),
        TextAlign = ContentAlignment.MiddleLeft
    };

    private static TabPage Sekme(string baslik, Control icerik)
    {
        var sayfa = new TabPage(baslik)
        {
            BackColor = Tema.Kart,
            Padding = new Padding(8),
            UseVisualStyleBackColor = false
        };
        icerik.Dock = DockStyle.Fill;
        sayfa.Controls.Add(icerik);
        return sayfa;
    }
}
