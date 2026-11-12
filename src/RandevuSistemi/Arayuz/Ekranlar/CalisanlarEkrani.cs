using RandevuSistemi.Models.Hizmetler;
using RandevuSistemi.Models.Kullanicilar;
using RandevuSistemi.Models.Kurumlar;
using RandevuSistemi.Veri;

namespace RandevuSistemi.Arayuz.Ekranlar;

/// <summary>
/// Doktor ve sağlık personelini listeler. Seçilen çalışanın şubesi değiştirilebilir ve
/// sunduğu hizmetler işaretlenerek belirlenebilir.
/// </summary>
public class CalisanlarEkrani : UserControl
{
    private readonly UygulamaVerisi _veri;
    private readonly DataGridView _tablo;
    private readonly Label _seciliBaslik;
    private readonly Label _seciliUnvan;
    private readonly ComboBox _subeKutusu;
    private readonly CheckedListBox _hizmetListesi;
    private readonly Label _hizmetNotu;

    private Calisan? _seciliCalisan;
    private bool _yukleniyor; // ekran doldurulurken olayların (event) tetiklenmesini engellemek için

    public CalisanlarEkrani(UygulamaVerisi veri)
    {
        _veri = veri;
        BackColor = Tema.Arkaplan;

        var yerlesim = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        yerlesim.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        yerlesim.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));

        // ---- Sol: çalışan tablosu ----
        _tablo = new DataGridView { Margin = new Padding(0, 0, 12, 0) };
        Tema.TabloyuBicimlendir(_tablo);
        _tablo.Columns.Add("ad", "Ad Soyad");
        _tablo.Columns.Add("unvan", "Unvan / Görev");
        _tablo.Columns.Add("sube", "Şube");
        _tablo.Columns.Add("hizmet", "Hizmet");
        _tablo.Columns["ad"]!.FillWeight = 26;
        _tablo.Columns["unvan"]!.FillWeight = 32;
        _tablo.Columns["sube"]!.FillWeight = 30;
        _tablo.Columns["hizmet"]!.FillWeight = 12;
        _tablo.SelectionChanged += (_, _) =>
        {
            if (_tablo.CurrentRow?.Tag is Calisan calisan)
                DetayGoster(calisan);
        };
        yerlesim.Controls.Add(_tablo, 0, 0);

        // ---- Sağ: seçili çalışanın ayrıntıları ----
        var detay = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7,
            BackColor = Tema.Kart,
            Padding = new Padding(16)
        };
        detay.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));  // ad
        detay.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));  // unvan
        detay.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));  // "Çalıştığı şube"
        detay.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));  // şube kutusu
        detay.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));  // "Sunduğu hizmetler"
        detay.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // hizmet listesi
        detay.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));  // not

        _seciliBaslik = new Label { Dock = DockStyle.Fill, Font = Tema.AltBaslik, ForeColor = Tema.Yazi };
        _seciliUnvan = new Label { Dock = DockStyle.Fill, ForeColor = Tema.SolukYazi };

        _subeKutusu = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        foreach (SubeSecenegi secenek in SubeSecenegi.Tumu(_veri))
            _subeKutusu.Items.Add(secenek);
        _subeKutusu.SelectedIndexChanged += (_, _) => SubeDegisti();

        _hizmetListesi = new CheckedListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            CheckOnClick = true,
            IntegralHeight = false,
            Font = new Font("Segoe UI", 10.5f)
        };
        _hizmetListesi.ItemCheck += HizmetIsaretlendi;

        _hizmetNotu = new Label { Dock = DockStyle.Fill, ForeColor = Tema.SolukYazi, Font = new Font("Segoe UI", 9f) };

        detay.Controls.Add(_seciliBaslik, 0, 0);
        detay.Controls.Add(_seciliUnvan, 0, 1);
        detay.Controls.Add(Tema.BolumBasligi("Çalıştığı şube"), 0, 2);
        detay.Controls.Add(_subeKutusu, 0, 3);
        detay.Controls.Add(Tema.BolumBasligi("Sunduğu hizmetler"), 0, 4);
        detay.Controls.Add(_hizmetListesi, 0, 5);
        detay.Controls.Add(_hizmetNotu, 0, 6);
        yerlesim.Controls.Add(detay, 1, 0);

        Controls.Add(yerlesim);

        TabloyuDoldur();
        Calisan? ilk = _veri.Calisanlar.FirstOrDefault();
        if (ilk is not null)
            DetayGoster(ilk);
    }

    private void TabloyuDoldur()
    {
        _tablo.Rows.Clear();
        foreach (Calisan calisan in _veri.Calisanlar)
        {
            int satir = _tablo.Rows.Add(calisan.TamAd, "", "", "");
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
        satir.Cells["unvan"].Value = calisan.Unvan;
        satir.Cells["sube"].Value = kurum is null || sube is null ? "— atanmamış —" : $"{kurum.Ad} / {sube.Ad}";
        satir.Cells["hizmet"].Value = calisan.HizmetIdleri.Count;
    }

    private void DetayGoster(Calisan calisan)
    {
        _yukleniyor = true;
        _seciliCalisan = calisan;

        _seciliBaslik.Text = calisan.TamAd;
        _seciliUnvan.Text = $"{calisan.Rol.GorunenAd()} · {calisan.Unvan}";

        // Çalışanın şubesini kutuda seçili yap
        _subeKutusu.SelectedIndex = -1;
        for (int i = 0; i < _subeKutusu.Items.Count; i++)
        {
            if (_subeKutusu.Items[i] is SubeSecenegi secenek && secenek.Sube.Id == calisan.SubeId)
            {
                _subeKutusu.SelectedIndex = i;
                break;
            }
        }

        HizmetListesiniDoldur();
        _yukleniyor = false;
    }

    /// <summary>Çalışanın şubesindeki hizmetleri listeler; sunduklarını işaretli gösterir.</summary>
    private void HizmetListesiniDoldur()
    {
        bool oncekiDurum = _yukleniyor;
        _yukleniyor = true;
        _hizmetListesi.Items.Clear();

        Sube? sube = _seciliCalisan is null ? null : _veri.SubeBul(_seciliCalisan.SubeId);
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
                : "Hasta, bu çalışandan sadece işaretli hizmetler için randevu alabilir.";
        }

        _yukleniyor = oncekiDurum;
    }

    private void SubeDegisti()
    {
        if (_yukleniyor || _seciliCalisan is null || _subeKutusu.SelectedItem is not SubeSecenegi secenek)
            return;

        _seciliCalisan.SubeyeAta(secenek.Sube);

        // Yeni şubede olmayan hizmetleri çalışandan çıkar (her şubenin hizmetleri ayrıdır).
        foreach (Guid hizmetId in _seciliCalisan.HizmetIdleri.ToList())
        {
            if (secenek.Sube.HizmetBul(hizmetId) is null)
                _seciliCalisan.HizmetCikar(hizmetId);
        }

        HizmetListesiniDoldur();
        SeciliSatiriGuncelle();
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
}
