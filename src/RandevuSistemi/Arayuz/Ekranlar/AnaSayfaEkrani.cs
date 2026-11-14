using System.Globalization;
using RandevuSistemi.Arayuz.Formlar;
using RandevuSistemi.Arayuz.Kontroller;
using RandevuSistemi.Models.Kullanicilar;
using RandevuSistemi.Models.Randevular;
using RandevuSistemi.Veri;

namespace RandevuSistemi.Arayuz.Ekranlar;

/// <summary>
/// Girişten sonra açılan karşılama ekranı (özet panosu):
/// - üstte "Merhaba, Ayşe" karşılama şeridi,
/// - ortada role göre özet kartları (yaklaşan randevu, onay bekleyen vb.),
/// - altta yaklaşan randevular listesi ve hızlı işlem kısayolları.
/// İçerik giriş yapan kullanıcının rolüne göre değişir.
/// </summary>
public class AnaSayfaEkrani : UserControl
{
    private static readonly CultureInfo Turkce = new("tr-TR");

    private readonly UygulamaVerisi _veri;
    private readonly Kullanici _kullanici;
    private readonly Action<string> _menuyeGit;

    /// <param name="menuyeGit">Kısayol kartına tıklanınca ilgili menü ekranına geçmek için (menü başlığını alır).</param>
    public AnaSayfaEkrani(UygulamaVerisi veri, Kullanici kullanici, Action<string> menuyeGit)
    {
        _veri = veri;
        _kullanici = kullanici;
        _menuyeGit = menuyeGit;
        BackColor = Tema.Arkaplan;

        DateTime simdi = DateTime.Now;
        List<Randevu> gorunur = _veri.Randevular.Where(r => kullanici.RandevuyuGorebilirMi(r)).ToList();
        List<Randevu> yaklasan = gorunur
            .Where(r => r.Baslangic >= simdi && r.Durum is RandevuDurumu.Beklemede or RandevuDurumu.Onaylandi)
            .OrderBy(r => r.Baslangic)
            .ToList();

        var yerlesim = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        yerlesim.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));  // karşılama şeridi
        yerlesim.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));  // özet kartları
        yerlesim.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // liste + kısayollar

        // ---- 1) Karşılama şeridi ----
        var banner = new BannerPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 18),
            Baslik = $"Merhaba, {kullanici.Ad}",
            AltBaslik = KarsilamaMetni(gorunur, yaklasan)
        };
        yerlesim.Controls.Add(banner, 0, 0);

        // ---- 2) Özet kartları ----
        List<IstatistikKarti> kartlar = OzetKartlari(gorunur, yaklasan);
        var kartSatiri = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = kartlar.Count,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 18)
        };
        for (int i = 0; i < kartlar.Count; i++)
        {
            kartSatiri.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / kartlar.Count));
            kartlar[i].Dock = DockStyle.Fill;
            if (i == kartlar.Count - 1)
                kartlar[i].Margin = new Padding(0);
            kartSatiri.Controls.Add(kartlar[i], i, 0);
        }
        yerlesim.Controls.Add(kartSatiri, 0, 1);

        // ---- 3) Alt bölüm: yaklaşan randevular (sol) + hızlı işlemler (sağ) ----
        var altBolum = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
        altBolum.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        altBolum.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        altBolum.Controls.Add(YaklasanRandevularKarti(yaklasan), 0, 0);
        altBolum.Controls.Add(HizliIslemler(), 1, 0);
        yerlesim.Controls.Add(altBolum, 0, 2);

        Controls.Add(yerlesim);
    }

    // =====================================================================
    // İçerik (role göre)
    // =====================================================================

    private string KarsilamaMetni(List<Randevu> gorunur, List<Randevu> yaklasan)
    {
        switch (_kullanici)
        {
            case Hasta:
                Randevu? siradaki = yaklasan.FirstOrDefault();
                return siradaki is null
                    ? "Yaklaşan bir randevunuz yok. Aşağıdaki \"Randevu Al\" ile hemen randevu alabilirsiniz."
                    : $"Sıradaki randevunuz: {siradaki.Baslangic.ToString("d MMMM dddd, HH:mm", Turkce)}  ·  " +
                      $"{siradaki.HizmetAdi}  ·  {siradaki.CalisanAdi}";

            case Calisan:
                int bugun = yaklasan.Count(r => r.Baslangic.Date == DateTime.Today);
                int bekleyen = yaklasan.Count(r => r.Durum == RandevuDurumu.Beklemede);
                return $"Bugün {bugun} randevunuz var. Onayınızı bekleyen {bekleyen} randevu bulunuyor.";

            default:
                int onayBekleyen = yaklasan.Count(r => r.Durum == RandevuDurumu.Beklemede);
                return $"Sistemde {gorunur.Count} randevu kayıtlı; {onayBekleyen} randevu onay bekliyor.";
        }
    }

    private List<IstatistikKarti> OzetKartlari(List<Randevu> gorunur, List<Randevu> yaklasan)
    {
        Color turuncu = Tema.DurumRengi(RandevuDurumu.Beklemede);
        Color mavi = Tema.DurumRengi(RandevuDurumu.Tamamlandi);
        Color yesil = Tema.DurumRengi(RandevuDurumu.Onaylandi);
        int bekleyen = yaklasan.Count(r => r.Durum == RandevuDurumu.Beklemede);

        switch (_kullanici)
        {
            case Hasta:
                return
                [
                    new IstatistikKarti(Tema.Ikonlar.Takvim, yaklasan.Count.ToString(), "Yaklaşan randevu", Tema.Vurgu),
                    new IstatistikKarti(Tema.Ikonlar.Bekle, bekleyen.ToString(), "Onay bekleyen", turuncu),
                    new IstatistikKarti(Tema.Ikonlar.Onay,
                        gorunur.Count(r => r.Durum == RandevuDurumu.Tamamlandi).ToString(), "Tamamlanan", mavi)
                ];

            case Calisan:
                return
                [
                    new IstatistikKarti(Tema.Ikonlar.Takvim,
                        yaklasan.Count(r => r.Baslangic.Date == DateTime.Today).ToString(), "Bugünkü randevu", Tema.Vurgu),
                    new IstatistikKarti(Tema.Ikonlar.Bekle, bekleyen.ToString(), "Onay bekleyen", turuncu),
                    new IstatistikKarti(Tema.Ikonlar.Liste,
                        yaklasan.Count(r => r.Baslangic < DateTime.Today.AddDays(7)).ToString(), "Önümüzdeki 7 gün", yesil)
                ];

            default:
                int subeSayisi = _veri.Kurumlar.Sum(k => k.Subeler.Count);
                return
                [
                    new IstatistikKarti(Tema.Ikonlar.Kurum, $"{_veri.Kurumlar.Count} / {subeSayisi}", "Kurum / Şube", Tema.Vurgu),
                    new IstatistikKarti(Tema.Ikonlar.Kisiler,
                        _veri.Calisanlar.Count(c => c.AktifMi).ToString(), "Aktif çalışan", yesil),
                    new IstatistikKarti(Tema.Ikonlar.Kisi, _veri.Hastalar.Count().ToString(), "Kayıtlı hasta", mavi),
                    new IstatistikKarti(Tema.Ikonlar.Bekle, bekleyen.ToString(), "Onay bekleyen", turuncu)
                ];
        }
    }

    // =====================================================================
    // Alt bölüm
    // =====================================================================

    private Control YaklasanRandevularKarti(List<Randevu> yaklasan)
    {
        var kart = new KartPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 18, 0), Padding = new Padding(20, 14, 20, 10) };

        var liste = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        bool calisanGorunumu = _kullanici is Calisan;

        if (yaklasan.Count == 0)
        {
            liste.Controls.Add(new Label
            {
                Text = "Yaklaşan randevu bulunmuyor.",
                Dock = DockStyle.Top,
                Height = 40,
                ForeColor = Tema.SolukYazi,
                TextAlign = ContentAlignment.MiddleLeft
            });
        }

        // Dock = Top ile dizilen kontrollerde son eklenen en üste çıkar;
        // bu yüzden en geç tarihli randevudan başlayarak ekliyoruz.
        foreach (Randevu randevu in yaklasan.Take(8).Reverse())
            liste.Controls.Add(new RandevuOzetSatiri(randevu, calisanGorunumu));

        var baslik = new Label
        {
            Text = _kullanici is Hasta ? "Yaklaşan Randevularım" : "Yaklaşan Randevular",
            Dock = DockStyle.Top,
            Height = 36,
            Font = Tema.AltBaslik,
            ForeColor = Tema.Lacivert,
            TextAlign = ContentAlignment.MiddleLeft
        };

        kart.Controls.Add(liste);
        kart.Controls.Add(baslik);
        return kart;
    }

    private Control HizliIslemler()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
        var kisayollar = new List<HizliIslemKarti>();

        switch (_kullanici)
        {
            case Hasta hasta:
                kisayollar.Add(new HizliIslemKarti(Tema.Ikonlar.Ekle, "Randevu Al",
                    "Bölüm, doktor ve saat seçin", () => RandevuAl(hasta, "Randevularım")));
                kisayollar.Add(new HizliIslemKarti(Tema.Ikonlar.Takvim, "Randevularım",
                    "Randevularınızı görün veya iptal edin", () => _menuyeGit("Randevularım")));
                kisayollar.Add(new HizliIslemKarti(Tema.Ikonlar.Kurum, "Kurumlar ve Hizmetler",
                    "Çalışma saatleri, bölümler, ücretler", () => _menuyeGit("Kurumlar ve Hizmetler")));
                break;

            case Calisan:
                kisayollar.Add(new HizliIslemKarti(Tema.Ikonlar.Onay, "Randevularım",
                    "Bekleyen randevuları onaylayın", () => _menuyeGit("Randevularım")));
                kisayollar.Add(new HizliIslemKarti(Tema.Ikonlar.Saat, "Çalışma Bilgilerim",
                    "Uygunluk saatlerinizi düzenleyin", () => _menuyeGit("Çalışma Bilgilerim")));
                break;

            default:
                kisayollar.Add(new HizliIslemKarti(Tema.Ikonlar.Ekle, "Yeni Randevu",
                    "Hasta adına randevu oluşturun", () => RandevuAl(null, "Randevular")));
                kisayollar.Add(new HizliIslemKarti(Tema.Ikonlar.Takvim, "Randevular",
                    "Onay bekleyenleri yönetin", () => _menuyeGit("Randevular")));
                kisayollar.Add(new HizliIslemKarti(Tema.Ikonlar.Kisiler, "Çalışanlar",
                    "Doktor ve personel işlemleri", () => _menuyeGit("Çalışanlar")));
                kisayollar.Add(new HizliIslemKarti(Tema.Ikonlar.Kurum, "Kurumlar",
                    "Şube, saat ve bölüm tanımları", () => _menuyeGit("Kurumlar")));
                break;
        }

        // Dock = Top: ters sırayla ekle, aralara boşluk koy
        foreach (HizliIslemKarti kart in Enumerable.Reverse(kisayollar))
        {
            panel.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 12 });
            panel.Controls.Add(kart);
        }

        panel.Controls.Add(new Label
        {
            Text = "Hızlı İşlemler",
            Dock = DockStyle.Top,
            Height = 36,
            Font = Tema.AltBaslik,
            ForeColor = Tema.Lacivert,
            TextAlign = ContentAlignment.MiddleLeft
        });
        return panel;
    }

    /// <summary>Randevu oluşturma penceresini açar; randevu alınırsa randevu listesine geçer.</summary>
    private void RandevuAl(Hasta? hasta, string sonrakiEkran)
    {
        if (!_veri.Hastalar.Any())
        {
            Tema.HataGoster("Sistemde kayıtlı hasta yok.");
            return;
        }

        using var form = new RandevuFormu(_veri, hasta);
        if (form.ShowDialog(this) == DialogResult.OK)
            _menuyeGit(sonrakiEkran);
    }
}
