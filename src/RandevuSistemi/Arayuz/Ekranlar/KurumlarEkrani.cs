using System.Drawing.Drawing2D;
using RandevuSistemi.Arayuz.Formlar;
using RandevuSistemi.Arayuz.Kontroller;
using RandevuSistemi.Models.Hizmetler;
using RandevuSistemi.Models.Kullanicilar;
using RandevuSistemi.Models.Kurumlar;
using RandevuSistemi.Models.Randevular;
using RandevuSistemi.Servisler;
using RandevuSistemi.Veri;

namespace RandevuSistemi.Arayuz.Ekranlar;

/// <summary>
/// Kurumları ve şubelerini ağaç yapısında gösterir ve yönetir.
/// - Sistem Yöneticisi: yeni hastane/klinik ve şube ekleyebilir ("yetki çerçevesinde").
/// - Yöneticiler: seçilen şubenin çalışma saatlerini ve bölümlerini düzenleyebilir.
/// - Hasta: SADECE GÖRÜNTÜLER (çalışma saatleri, bölümler, hizmetlerin süre ve ücretleri);
///   düzenleme araçları gizlenir.
/// Hizmetler burada sadece listelenir; eklemek/silmek için Hizmetler ekranı kullanılır.
/// </summary>
public class KurumlarEkrani : UserControl
{
    private readonly UygulamaVerisi _veri;
    private readonly Kullanici _aktifKullanici;

    private readonly TreeView _agac;
    private readonly Label _secilenBaslik;
    private readonly Label _secilenBilgi;
    private readonly ListBox _saatListesi;   // haftanın 7 günü (kapalı günler dahil), kendimiz çiziyoruz
    private readonly ListBox _bolumListesi;  // bölümler + çalışan sayısı, kendimiz çiziyoruz
    private readonly ListBox _hizmetListesi;

    // Çalışma saati düzenleme: gün listeden seçilir, saatler alttaki şeritte düzenlenir
    private readonly Label _seciliGunEtiketi;
    private readonly DateTimePicker _acilisSaati;
    private readonly DateTimePicker _kapanisSaati;
    private readonly Control _saatAraclari;
    private DayOfWeek _seciliGun = DayOfWeek.Monday;

    // Bölüm düzenleme
    private readonly TextBox _bolumAdiKutusu;
    private readonly Control _bolumAraclari;

    /// <summary>Seçili şubedeki her bölümde kaç aktif çalışan olduğu (bölüm satırında gösterilir).</summary>
    private Dictionary<Guid, int> _bolumCalisanSayilari = new();

    private Sube? _seciliSube;
    private Kurum? _seciliKurum;

    /// <summary>Çalışma saatleri listesinin bir satırı: gün ve (açıksa) saatleri. Kapalı günlerde Saat null'dur.</summary>
    private sealed record GunSatiri(DayOfWeek Gun, CalismaSaati? Saat);

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
                Text = _aktifKullanici.KurumBilgileriniDuzenleyebilirMi
                    ? "Yeni kurum/şube sadece Sistem Yöneticisi tarafından eklenebilir."
                    : "Bir şube seçerek çalışma saatlerini, bölümlerini ve hizmetlerini görebilirsiniz.",
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
            ShowLines = false,
            DrawMode = TreeViewDrawMode.OwnerDrawText  // düğümleri DugumuCiz metodu çizer
        };
        _agac.DrawNode += DugumuCiz;
        _agac.AfterSelect += (_, e) => SecimiGoster(e.Node?.Tag);
        solTaraf.Controls.Add(KartIcineAl(_agac), 0, 1);
        yerlesim.Controls.Add(solTaraf, 0, 0);

        // ================= Sağ: seçilen öğenin ayrıntıları =================
        // Düzen:  [Şube adı / bilgi]
        //         [Çalışma Saatleri kutusu] [Bölümler kutusu]   ← yan yana
        //         [Hizmetler]
        bool duzenleyebilir = _aktifKullanici.KurumBilgileriniDuzenleyebilirMi;

        var detay = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, BackColor = Tema.Kart };
        detay.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));   // 0 başlık
        detay.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));   // 1 bilgi
        detay.RowStyles.Add(new RowStyle(SizeType.Percent, 60));    // 2 saatler + bölümler
        detay.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));   // 3 "Hizmetler"
        detay.RowStyles.Add(new RowStyle(SizeType.Percent, 40));    // 4 hizmet listesi

        _secilenBaslik = new Label { Dock = DockStyle.Fill, Font = Tema.AltBaslik, ForeColor = Tema.Yazi };
        _secilenBilgi = new Label { Dock = DockStyle.Fill, ForeColor = Tema.SolukYazi, TextAlign = ContentAlignment.TopLeft };

        // ---- Çalışma saatleri: 7 günlük liste + düzenleme şeridi ----
        _saatListesi = CizilenListe(GunSatiriniCiz);
        _saatListesi.SelectedIndexChanged += (_, _) =>
        {
            if (_saatListesi.SelectedItem is GunSatiri satir)
            {
                _seciliGun = satir.Gun;
                SeciliGununSaatleriniYukle();
            }
        };

        _seciliGunEtiketi = new Label
        {
            AutoSize = false,
            Size = new Size(100, 30),
            Font = Tema.Kalin,
            ForeColor = Tema.Lacivert,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 2, 8, 0)
        };
        _acilisSaati = SaatSecici();
        _kapanisSaati = SaatSecici();
        Button saatKaydet = KucukButon("Kaydet", birincil: true);
        Button gunuKapat = KucukButon("Günü Kapat", birincil: false);
        saatKaydet.Click += (_, _) => CalismaSaatiKaydet();
        gunuKapat.Click += (_, _) => GunuKapat();

        var saatSatiri = YatayDizi(_seciliGunEtiketi, KucukEtiket("Açılış"), _acilisSaati, KucukEtiket("Kapanış"), _kapanisSaati);
        var saatButonlari = YatayDizi(saatKaydet, gunuKapat, IpucuEtiketi("Listeden gün seçin"));
        _saatAraclari = DuzenlemeSeridi(saatSatiri, saatButonlari);

        // ---- Bölümler: liste (çalışan sayısıyla) + ekleme şeridi ----
        _bolumListesi = CizilenListe(BolumSatiriniCiz);

        _bolumAdiKutusu = new TextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 10.5f),
            PlaceholderText = "Yeni bölüm adı (ör. Kardiyoloji)"
        };
        _bolumAdiKutusu.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // "bip" sesini engelle
                BolumEkle();
            }
        };
        // Kenarlıksız metin kutusunu beyaz, yuvarlak köşeli bir kutunun içine koyuyoruz
        var bolumAdiCercevesi = new KartPanel
        {
            Dock = DockStyle.Fill,
            KoseYaricapi = 8,
            Padding = new Padding(10, 8, 10, 4),
            Margin = new Padding(0, 0, 8, 0)
        };
        bolumAdiCercevesi.Controls.Add(_bolumAdiKutusu);

        Button bolumEkle = KucukButon("+ Ekle", birincil: true);
        Button bolumSil = KucukButon("Seçiliyi Sil", birincil: false);
        bolumEkle.Click += (_, _) => BolumEkle();
        bolumSil.Click += (_, _) => BolumSil();

        var bolumGirisSatiri = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
        bolumGirisSatiri.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bolumGirisSatiri.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bolumGirisSatiri.Controls.Add(bolumAdiCercevesi, 0, 0);
        bolumGirisSatiri.Controls.Add(bolumEkle, 1, 0);
        var bolumButonlari = YatayDizi(bolumSil, IpucuEtiketi("Aktif çalışanı olan bölüm silinemez"));
        _bolumAraclari = DuzenlemeSeridi(bolumGirisSatiri, bolumButonlari);

        // Düzenleme yetkisi yoksa (hasta) düzenleme şeritleri hiç eklenmez: ekran salt okunur olur.
        var ortaBolum = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 6, 0, 0) };
        ortaBolum.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        ortaBolum.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        ortaBolum.Controls.Add(Kutucuk(Tema.Ikonlar.Saat, "Çalışma Saatleri", _saatListesi,
            duzenleyebilir ? _saatAraclari : null, new Padding(0, 0, 8, 0)), 0, 0);
        ortaBolum.Controls.Add(Kutucuk(Tema.Ikonlar.Liste, "Bölümler", _bolumListesi,
            duzenleyebilir ? _bolumAraclari : null, new Padding(8, 0, 0, 0)), 1, 0);

        _hizmetListesi = YeniListe(secilebilir: false);

        detay.Controls.Add(_secilenBaslik, 0, 0);
        detay.Controls.Add(_secilenBilgi, 0, 1);
        detay.Controls.Add(ortaBolum, 0, 2);
        detay.Controls.Add(KutucukBasligi(Tema.Ikonlar.Saglik, duzenleyebilir
            ? "Hizmetler  (Hizmetler ekranından yönetilir)"
            : "Hizmetler  (süre ve ücret)"), 0, 3);
        detay.Controls.Add(_hizmetListesi, 0, 4);
        yerlesim.Controls.Add(KartIcineAl(detay), 1, 0);

        Controls.Add(yerlesim);

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
            var kurumDugumu = new TreeNode($"{kurum.Ad}  ({kurum.TurAdi})") { Tag = kurum, NodeFont = KurumDugumuYazisi };
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

                // Haftanın 7 günü listelenir; kapalı günler "Kapalı" etiketiyle görünür.
                foreach (DayOfWeek gun in CalismaSaati.HaftaninGunleri)
                    _saatListesi.Items.Add(new GunSatiri(gun, sube.CalismaSaatiGetir(gun)));

                // Her bölümdeki aktif çalışan sayısı (satırda "2 çalışan" olarak gösterilir).
                var servis = new RandevuServisi(_veri);
                _bolumCalisanSayilari = sube.Bolumler.ToDictionary(
                    b => b.Id, b => servis.BolumdekiAktifCalisanlar(sube, b).Count);
                foreach (Bolum bolum in sube.Bolumler)
                    _bolumListesi.Items.Add(bolum);
                if (sube.Bolumler.Count == 0)
                    _bolumListesi.Items.Add("Henüz bölüm eklenmemiş");

                if (sube.Hizmetler.Count == 0)
                    _hizmetListesi.Items.Add("Hizmet tanımlanmamış");
                foreach (SaglikHizmeti hizmet in sube.Hizmetler)
                    _hizmetListesi.Items.Add(hizmet); // "Ad (süre, ücret)" şeklinde görünür

                // Önceden seçili gün tekrar seçilir (kaydettikten sonra liste yenilense de aynı gün kalır).
                _saatListesi.SelectedIndex = CalismaSaati.GunSirasi(_seciliGun);
                break;

            case Kurum kurum:
                _seciliKurum = kurum;
                _secilenBaslik.Text = kurum.Ad;
                _secilenBilgi.Text = $"{kurum.TurAdi} · {kurum.Subeler.Count} şube" +
                                     (kurum.CokSubeliMi ? " (çok şubeli)" : " (tek şubeli)");
                _saatListesi.Items.Add("Bir şube seçin");
                _bolumListesi.Items.Add("Bir şube seçin");
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

    private DayOfWeek SeciliGun => _seciliGun;

    /// <summary>Seçilen günün mevcut saatlerini kutulara yazar (o gün kapalıysa 09:00 - 17:00 önerir).</summary>
    private void SeciliGununSaatleriniYukle()
    {
        _seciliGunEtiketi.Text = CalismaSaati.GunAdi(_seciliGun);
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
            DayOfWeek gun = SeciliGun;
            var yeniSaat = new CalismaSaati(gun, acilis, kapanis); // kapanış açılıştan önceyse hata fırlar

            // Değişiklikten önce: yeni saatlere sığmayacak uygunluk dilimleri yüzünden iptal olacak randevular
            // O günün dilimleri yeni saatlere kırpılır (ör. 09:00-16:00 → 09:20-16:00); diğer günler aynı kalır.
            List<Randevu> iptalEdilecekler = EtkilenecekRandevular(sube,
                d => d.Gun != gun ? d : d.AraligaKirp(yeniSaat.Acilis, yeniSaat.Kapanis));
            if (!Tema.IptalOnayiAl(iptalEdilecekler,
                    $"{sube.Ad}, {CalismaSaati.GunAdi(gun)} çalışma saati {acilis:HH:mm} - {kapanis:HH:mm} olarak değiştirilecek."))
                return;

            sube.CalismaSaatiBelirle(gun, acilis, kapanis);
            int silinen = CalisanUygunluklariniDuzelt(sube);
            int iptalSayisi = SaatDisiRandevulariIptalEt(sube);

            AgaciDoldur(sube.Id);
            Tema.BilgiGoster($"{CalismaSaati.GunAdi(gun)} çalışma saati {acilis:HH:mm} - {kapanis:HH:mm} olarak kaydedildi." +
                             UygunlukUyarisi(silinen, iptalSayisi));
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

        DayOfWeek gun = SeciliGun;
        List<Randevu> iptalEdilecekler = EtkilenecekRandevular(sube, d => d.Gun != gun ? d : null);

        DialogResult cevap = MessageBox.Show(
            $"{sube.Ad}, {gunAdi} günleri kapalı olarak işaretlensin mi?" +
            (iptalEdilecekler.Count == 0
                ? ""
                : $"\n\nBu güne alınmış {iptalEdilecekler.Count} ileri tarihli randevu İPTAL EDİLECEK:\n\n" +
                  Tema.RandevuListesiMetni(iptalEdilecekler)),
            "Günü Kapat", MessageBoxButtons.YesNo,
            iptalEdilecekler.Count == 0 ? MessageBoxIcon.Question : MessageBoxIcon.Warning);
        if (cevap != DialogResult.Yes)
            return;

        sube.CalismaGunuKaldir(gun);
        int silinen = CalisanUygunluklariniDuzelt(sube);
        int iptalSayisi = SaatDisiRandevulariIptalEt(sube);

        AgaciDoldur(sube.Id);
        Tema.BilgiGoster($"{sube.Ad} artık {gunAdi} günleri kapalı." + UygunlukUyarisi(silinen, iptalSayisi));
    }

    /// <summary>
    /// Şubenin saatleri değişince, bu şubedeki çalışanların uygunluk dilimlerini yeni saatlere kırpar
    /// (Calisan.SubeyeAta bunu yapıyor). Kısaltılan veya silinen dilim sayısını döndürür.
    /// </summary>
    private int CalisanUygunluklariniDuzelt(Sube sube)
    {
        List<Calisan> subeCalisanlari = _veri.Calisanlar.Where(c => c.SubeId == sube.Id).ToList();
        int degisen = 0;

        foreach (Calisan calisan in subeCalisanlari)
        {
            List<UygunlukDilimi> onceki = calisan.UygunlukDilimleri.ToList();
            calisan.SubeyeAta(sube);
            // Değişmeyen dilimler aynı nesne olarak kalır; yeni listede olmayan eski dilimler değişmiş/silinmiştir.
            degisen += onceki.Count(d => !calisan.UygunlukDilimleri.Contains(d));
        }

        return degisen;
    }

    /// <summary>
    /// Şube saatleri değişirse, şubedeki çalışanların hangi ileri tarihli randevularının iptal olacağını hesaplar.
    /// yeniHali: bir uygunluk diliminin değişiklikten sonraki halini (kırpılmış dilim veya silinecekse null) veren fonksiyon.
    /// </summary>
    private List<Randevu> EtkilenecekRandevular(Sube sube, Func<UygunlukDilimi, UygunlukDilimi?> yeniHali)
    {
        var servis = new RandevuServisi(_veri);
        return _veri.Calisanlar
            .Where(c => c.SubeId == sube.Id)
            .SelectMany(c => servis.UygunlukDisiKalanlar(c, c.UygunlukDilimleri.Select(yeniHali).OfType<UygunlukDilimi>()))
            .OrderBy(r => r.Baslangic)
            .ToList();
    }

    /// <summary>
    /// Şube saatleri değiştikten sonra, şubedeki çalışanların yeni uygunluk saatleri dışında kalan
    /// ileri tarihli randevularını iptal eder. İptal edilen randevu sayısını döndürür.
    /// </summary>
    private int SaatDisiRandevulariIptalEt(Sube sube)
    {
        var servis = new RandevuServisi(_veri);
        string sebep = $"{sube.Ad} şubesinin çalışma saatleri {DateTime.Now:dd.MM.yyyy} tarihinde değiştiği için iptal edildi " +
                       "(randevu yeni saatlerin dışında kaldı).";
        int toplam = 0;

        try
        {
            foreach (Calisan calisan in _veri.Calisanlar.Where(c => c.SubeId == sube.Id).ToList())
                toplam += servis.UygunlukDisiRandevulariIptalEt(calisan, _aktifKullanici, sebep).Count;
        }
        catch (Exception hata) when (hata is UnauthorizedAccessException or InvalidOperationException)
        {
            Tema.HataGoster(hata.Message);
        }

        return toplam;
    }

    private static string UygunlukUyarisi(int silinenDilim, int iptalEdilenRandevu)
    {
        string mesaj = "";
        if (silinenDilim > 0)
            mesaj += $"\n\n{silinenDilim} çalışan uygunluk dilimi yeni saatlere göre kısaltıldı veya silindi.";
        if (iptalEdilenRandevu > 0)
            mesaj += $"\n{iptalEdilenRandevu} ileri tarihli randevu yeni saatlerin dışında kaldığı için iptal edildi.";
        return mesaj;
    }

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

        var servis = new RandevuServisi(_veri);

        // Bölümde aktif doktor / fizyoterapist varsa silmeye izin verilmez (isimleriyle uyarı gösterilir).
        if (servis.BolumSilmeEngeli(sube, bolum) is { } engel)
        {
            Tema.HataGoster(engel);
            return;
        }

        DialogResult cevap = MessageBox.Show(
            $"\"{sube.Ad}\" şubesindeki \"{bolum.Ad}\" bölümü silinsin mi?\n\n" +
            "Bu bölümde aktif çalışan yok; hiçbir randevu etkilenmeyecek.",
            "Bölümü Sil", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (cevap != DialogResult.Yes)
            return;

        try
        {
            servis.BolumuSil(sube, bolum);
        }
        catch (RandevuKuraliException hata)
        {
            Tema.HataGoster(hata.Message);
            return;
        }

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
        Width = 78,
        Font = new Font("Segoe UI", 10.5f),
        Value = DateTime.Today.AddHours(9),
        Margin = new Padding(0, 2, 14, 0)
    };

    private static Button KucukButon(string metin, bool birincil)
    {
        Button buton = Tema.Buton(metin, birincil);
        buton.MinimumSize = new Size(96, 34);
        buton.Margin = new Padding(0, 0, 8, 0);
        return buton;
    }

    private static Label KucukEtiket(string metin) => new()
    {
        Text = metin,
        AutoSize = true,
        ForeColor = Tema.SolukYazi,
        Margin = new Padding(0, 8, 6, 0)
    };

    private static Label IpucuEtiketi(string metin) => new()
    {
        Text = metin,
        AutoSize = true,
        Font = Tema.Kucuk,
        ForeColor = Tema.SolukYazi,
        Margin = new Padding(4, 10, 0, 0)
    };

    /// <summary>Kontrolleri soldan sağa tek satırda dizer.</summary>
    private static FlowLayoutPanel YatayDizi(params Control[] kontroller)
    {
        var dizi = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = new Padding(0) };
        dizi.Controls.AddRange(kontroller);
        return dizi;
    }

    /// <summary>
    /// Düzenleme araçlarını açık gri, yuvarlak köşeli bir şeridin içine iki satır halinde koyar
    /// (üstte giriş alanları, altta butonlar). Listeden görsel olarak ayrılır.
    /// </summary>
    private static Control DuzenlemeSeridi(Control ustSatir, Control altSatir)
    {
        var serit = new KartPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Tema.Arkaplan,
            KoseYaricapi = 10,
            Padding = new Padding(12, 10, 12, 8),
            Margin = new Padding(0, 8, 0, 0)
        };
        var ic = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0) };
        ic.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        ic.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        ic.Controls.Add(ustSatir, 0, 0);
        ic.Controls.Add(altSatir, 0, 1);
        serit.Controls.Add(ic);
        return serit;
    }

    /// <summary>Simgeli bölüm başlığı: [simge] Çalışma Saatleri</summary>
    private static Control KutucukBasligi(string ikon, string metin)
    {
        var baslik = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
        baslik.Controls.Add(new Label
        {
            Text = metin,
            Dock = DockStyle.Fill,
            Font = Tema.Kalin,
            ForeColor = Tema.Lacivert,
            TextAlign = ContentAlignment.MiddleLeft
        });
        baslik.Controls.Add(new Label
        {
            Text = ikon,
            Dock = DockStyle.Left,
            Width = 28,
            Font = Tema.Ikon,
            ForeColor = Tema.Vurgu,
            TextAlign = ContentAlignment.MiddleLeft
        });
        return baslik;
    }

    /// <summary>
    /// Başlık + liste + (varsa) düzenleme şeridinden oluşan, çerçeveli bir kutu.
    /// araclar null ise (salt okunur kullanıcı) düzenleme şeridi eklenmez.
    /// </summary>
    private static Control Kutucuk(string ikon, string baslik, Control liste, Control? araclar, Padding disBosluk)
    {
        var kutu = new KartPanel { Dock = DockStyle.Fill, Padding = new Padding(14, 8, 14, 12), Margin = disBosluk };
        var ic = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = new Padding(0) };
        ic.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        ic.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        ic.RowStyles.Add(new RowStyle(SizeType.Absolute, araclar is null ? 0 : 108));
        ic.Controls.Add(KutucukBasligi(ikon, baslik), 0, 0);
        ic.Controls.Add(liste, 0, 1);
        if (araclar is not null)
            ic.Controls.Add(araclar, 0, 2);
        kutu.Controls.Add(ic);
        return kutu;
    }

    /// <summary>Satırlarını kendimiz çizdiğimiz liste (DrawMode = OwnerDrawFixed).</summary>
    private static ListBox CizilenListe(DrawItemEventHandler ciz)
    {
        var liste = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            IntegralHeight = false,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 34,
            Font = Tema.Normal,
            Margin = new Padding(0)
        };
        liste.DrawItem += ciz;
        return liste;
    }

    /// <summary>Satır zemini: seçiliyse açık turkuaz + solda turkuaz şerit; altında ince çizgi.</summary>
    private static void SatirZemininiCiz(Graphics g, Rectangle alan, bool secili, bool sonSatir, Color zemin)
    {
        using (var firca = new SolidBrush(secili ? Tema.VurguAcik : zemin))
            g.FillRectangle(firca, alan);
        if (secili)
        {
            using var serit = new SolidBrush(Tema.Vurgu);
            g.FillRectangle(serit, alan.X, alan.Y + 6, 3, alan.Height - 12);
        }
        if (!sonSatir)
        {
            using var kalem = new Pen(Tema.Cizgi);
            g.DrawLine(kalem, alan.X + 12, alan.Bottom - 1, alan.Right - 8, alan.Bottom - 1);
        }
    }

    /// <summary>Liste boşken gösterilen soluk açıklama satırı ("Bir şube seçin" gibi).</summary>
    private static void AciklamaSatiriCiz(Graphics g, Rectangle alan, object? oge, Color zemin)
    {
        using (var firca = new SolidBrush(zemin))
            g.FillRectangle(firca, alan);
        TextRenderer.DrawText(g, oge?.ToString() ?? "", Tema.Normal, new Rectangle(alan.X + 12, alan.Y, alan.Width - 12, alan.Height),
            Tema.SolukYazi, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    /// <summary>Çalışma saati satırı:  Pazartesi ............ ● 10:00 – 17:00   veya   Cumartesi ...... (Kapalı)</summary>
    private void GunSatiriniCiz(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _saatListesi.Items.Count)
            return;
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle alan = e.Bounds;

        if (_saatListesi.Items[e.Index] is not GunSatiri satir)
        {
            AciklamaSatiriCiz(g, alan, _saatListesi.Items[e.Index], _saatListesi.BackColor);
            return;
        }

        bool secili = (e.State & DrawItemState.Selected) != 0;
        SatirZemininiCiz(g, alan, secili, e.Index == _saatListesi.Items.Count - 1, _saatListesi.BackColor);

        TextRenderer.DrawText(g, CalismaSaati.GunAdi(satir.Gun), secili ? Tema.Kalin : Tema.Normal,
            new Rectangle(alan.X + 14, alan.Y, 140, alan.Height), secili ? Tema.VurguKoyu : Tema.Yazi,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

        if (satir.Saat is { } saat)
        {
            string metin = $"{saat.Acilis:HH:mm} – {saat.Kapanis:HH:mm}";
            int metinGenisligi = TextRenderer.MeasureText(metin, Tema.Normal).Width;
            int x = alan.Right - metinGenisligi - 10;
            using (var nokta = new SolidBrush(Tema.DurumRengi(RandevuDurumu.Onaylandi)))   // yeşil "açık" noktası
                g.FillEllipse(nokta, x - 14, alan.Y + alan.Height / 2 - 4, 8, 8);
            TextRenderer.DrawText(g, metin, Tema.Normal, new Rectangle(x, alan.Y, metinGenisligi + 4, alan.Height),
                Tema.Yazi, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }
        else
        {
            int hapGenisligi = TextRenderer.MeasureText("Kapalı", Tema.Kucuk).Width + 18;
            Tema.DurumHapiCiz(g, new Rectangle(alan.Right - hapGenisligi - 8, alan.Y, hapGenisligi, alan.Height),
                "Kapalı", Tema.SolukYazi);
        }
    }

    /// <summary>Bölüm satırı:  [K] Kardiyoloji ............ (2 çalışan)   — boş bölüm "Boş" etiketiyle gösterilir.</summary>
    private void BolumSatiriniCiz(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _bolumListesi.Items.Count)
            return;
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle alan = e.Bounds;

        if (_bolumListesi.Items[e.Index] is not Bolum bolum)
        {
            AciklamaSatiriCiz(g, alan, _bolumListesi.Items[e.Index], _bolumListesi.BackColor);
            return;
        }

        bool secili = (e.State & DrawItemState.Selected) != 0;
        SatirZemininiCiz(g, alan, secili, e.Index == _bolumListesi.Items.Count - 1, _bolumListesi.BackColor);

        // Solda baş harf kutucuğu
        var harfKutusu = new Rectangle(alan.X + 12, alan.Y + (alan.Height - 24) / 2, 24, 24);
        using (GraphicsPath yol = Tema.YuvarlakYol(harfKutusu, 6))
        using (var firca = new SolidBrush(secili ? Color.White : Tema.VurguAcik))
            g.FillPath(firca, yol);
        string harf = bolum.Ad.Trim().Length > 0 ? bolum.Ad.Trim()[0].ToString().ToUpper(new System.Globalization.CultureInfo("tr-TR")) : "";
        TextRenderer.DrawText(g, harf, Tema.Kalin, harfKutusu, Tema.Vurgu,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        // Sağda çalışan sayısı etiketi
        int sayi = _bolumCalisanSayilari.GetValueOrDefault(bolum.Id);
        string hap = sayi > 0 ? $"{sayi} çalışan" : "Boş";
        int hapGenisligi = TextRenderer.MeasureText(hap, Tema.Kucuk).Width + 18;
        Tema.DurumHapiCiz(g, new Rectangle(alan.Right - hapGenisligi - 8, alan.Y, hapGenisligi, alan.Height),
            hap, sayi > 0 ? Tema.Vurgu : Tema.SolukYazi);

        // Ortada bölüm adı
        int x = harfKutusu.Right + 10;
        TextRenderer.DrawText(g, bolum.Ad, secili ? Tema.Kalin : Tema.Normal,
            new Rectangle(x, alan.Y, Math.Max(1, alan.Right - hapGenisligi - 16 - x), alan.Height),
            secili ? Tema.VurguKoyu : Tema.Yazi,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    /// <summary>Kontrolü köşeleri yuvarlak, beyaz bir "kart" panelin içine koyar.</summary>
    private static Panel KartIcineAl(Control icerik) => Tema.KartaKoy(icerik, 16);

    private static readonly Font KurumDugumuYazisi = new("Segoe UI Semibold", 11f);

    /// <summary>
    /// Ağaçtaki düğümleri kendimiz çizeriz: seçili düğüm açık turkuaz zeminli ve solunda
    /// turkuaz çizgili olur, kurum düğümleri kalın yazılır (TreeView.DrawMode = OwnerDrawText).
    /// </summary>
    private void DugumuCiz(object? sender, DrawTreeNodeEventArgs e)
    {
        if (e.Node is null || e.Bounds.IsEmpty)
            return;

        bool secili = (e.State & TreeNodeStates.Selected) != 0;
        Font yazi = e.Node.NodeFont ?? _agac.Font;
        var alan = new Rectangle(e.Bounds.X, e.Bounds.Y, Math.Max(1, _agac.ClientSize.Width - e.Bounds.X), e.Bounds.Height);

        using (var zemin = new SolidBrush(secili ? Tema.VurguAcik : _agac.BackColor))
            e.Graphics.FillRectangle(zemin, alan);
        if (secili)
        {
            using var serit = new SolidBrush(Tema.Vurgu);
            e.Graphics.FillRectangle(serit, alan.X, alan.Y + 5, 3, alan.Height - 10);
        }

        TextRenderer.DrawText(e.Graphics, e.Node.Text, yazi,
            new Rectangle(alan.X + 10, alan.Y, alan.Width - 10, alan.Height),
            secili ? Tema.VurguKoyu : Tema.Yazi,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}
