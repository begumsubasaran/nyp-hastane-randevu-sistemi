using RandevuSistemi.Arayuz.Formlar;
using RandevuSistemi.Models.Kullanicilar;
using RandevuSistemi.Models.Kurumlar;
using RandevuSistemi.Models.Randevular;
using RandevuSistemi.Veri;

namespace RandevuSistemi.Arayuz.Ekranlar;

/// <summary>
/// Tüm randevuları tarih sırasıyla listeler; üstteki kutudan duruma göre filtrelenebilir.
/// Her satırda randevu detayları (işlem, süre, ücret, çalışan, tarih/saat) ve durumu görünür.
///
/// - "+ Yeni Randevu" butonu randevu oluşturma penceresini açar.
/// - Alttaki butonlarla seçili randevu onaylanır, reddedilir, iptal edilir veya tamamlandı yapılır.
///   Butonlar, randevunun durumuna ve işlemi yapan kullanıcının yetkisine göre açılıp kapanır.
/// </summary>
public class RandevularEkrani : UserControl
{
    private readonly UygulamaVerisi _veri;
    private readonly Kullanici? _islemYapan;
    private readonly ComboBox _durumFiltresi;
    private readonly Label _sayac;
    private readonly DataGridView _tablo;

    private readonly Button _onaylaButon;
    private readonly Button _reddetButon;
    private readonly Button _iptalButon;
    private readonly Button _tamamlaButon;

    public RandevularEkrani(UygulamaVerisi veri, Kullanici? islemYapan)
    {
        _veri = veri;
        _islemYapan = islemYapan;
        BackColor = Tema.Arkaplan;

        var yerlesim = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        yerlesim.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        yerlesim.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        yerlesim.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));

        // ---- Üst çubuk: yeni randevu butonu + durum filtresi ----
        var ustCubuk = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        Button yeniButon = Tema.Buton("+ Yeni Randevu");
        yeniButon.Margin = new Padding(0, 0, 24, 0);
        yeniButon.Click += (_, _) => YeniRandevu();
        ustCubuk.Controls.Add(yeniButon);
        ustCubuk.Controls.Add(new Label
        {
            Text = "Durum:",
            AutoSize = true,
            Margin = new Padding(0, 9, 6, 0),
            ForeColor = Tema.SolukYazi
        });
        _durumFiltresi = new KaydirmasizComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 200,
            Margin = new Padding(0, 5, 16, 0)
        };
        _durumFiltresi.Items.Add("Tümü");
        foreach (RandevuDurumu durum in Enum.GetValues<RandevuDurumu>())
            _durumFiltresi.Items.Add(durum.GorunenAd());
        ustCubuk.Controls.Add(_durumFiltresi);

        _sayac = new Label { AutoSize = true, Margin = new Padding(0, 9, 0, 0), ForeColor = Tema.SolukYazi };
        ustCubuk.Controls.Add(_sayac);
        yerlesim.Controls.Add(ustCubuk, 0, 0);

        // ---- Randevu tablosu ----
        _tablo = new DataGridView();
        Tema.TabloyuBicimlendir(_tablo);
        _tablo.Columns.Add("tarih", "Tarih");
        _tablo.Columns.Add("saat", "Saat");
        _tablo.Columns.Add("hasta", "Hasta");
        _tablo.Columns.Add("calisan", "Doktor / Personel");
        _tablo.Columns.Add("sube", "Şube");
        _tablo.Columns.Add("hizmet", "İşlem");
        _tablo.Columns.Add("ucret", "Ücret");
        _tablo.Columns.Add("durum", "Durum");
        _tablo.Columns.Add("not", "Açıklama");
        _tablo.Columns["tarih"]!.FillWeight = 13;
        _tablo.Columns["saat"]!.FillWeight = 13;
        _tablo.Columns["hasta"]!.FillWeight = 10;
        _tablo.Columns["calisan"]!.FillWeight = 17;
        _tablo.Columns["sube"]!.FillWeight = 15;
        _tablo.Columns["hizmet"]!.FillWeight = 12;
        _tablo.Columns["ucret"]!.FillWeight = 8;
        _tablo.Columns["durum"]!.FillWeight = 10;
        _tablo.Columns["not"]!.FillWeight = 12;
        _tablo.Columns["ucret"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _tablo.Columns["durum"]!.DefaultCellStyle.Font = Tema.Kalin;
        _tablo.SelectionChanged += (_, _) => ButonlariGuncelle();
        yerlesim.Controls.Add(_tablo, 0, 1);

        // ---- Alt çubuk: onay mekanizması butonları ----
        var altCubuk = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(0, 12, 0, 0) };
        _onaylaButon = Tema.Buton("✓ Onayla");
        _reddetButon = Tema.Buton("Reddet", birincil: false);
        _iptalButon = Tema.Buton("İptal Et", birincil: false);
        _tamamlaButon = Tema.Buton("Tamamlandı", birincil: false);
        _onaylaButon.Click += (_, _) => Onayla();
        _reddetButon.Click += (_, _) => Reddet();
        _iptalButon.Click += (_, _) => IptalEt();
        _tamamlaButon.Click += (_, _) => Tamamla();

        var islemYapanEtiketi = new Label
        {
            AutoSize = true,
            Margin = new Padding(16, 8, 0, 0),
            ForeColor = Tema.SolukYazi,
            Text = _islemYapan is null
                ? "İşlem yapan kullanıcı yok"
                : $"İşlem yapan: {_islemYapan.TamAd} ({_islemYapan.Rol.GorunenAd()})"
        };

        altCubuk.Controls.Add(_onaylaButon);
        altCubuk.Controls.Add(_reddetButon);
        altCubuk.Controls.Add(_iptalButon);
        altCubuk.Controls.Add(_tamamlaButon);
        altCubuk.Controls.Add(islemYapanEtiketi);
        yerlesim.Controls.Add(altCubuk, 0, 2);

        Controls.Add(yerlesim);

        _durumFiltresi.SelectedIndexChanged += (_, _) => TabloyuDoldur();
        _durumFiltresi.SelectedIndex = 0;
    }

    private Randevu? SeciliRandevu => _tablo.CurrentRow?.Tag as Randevu;

    // =====================================================================
    // Tablo
    // =====================================================================

    private void TabloyuDoldur()
    {
        // İlk seçenek "Tümü", sonrakiler RandevuDurumu enum'unun sırasıyla aynı.
        RandevuDurumu? secilenDurum = _durumFiltresi.SelectedIndex <= 0
            ? null
            : (RandevuDurumu)(_durumFiltresi.SelectedIndex - 1);

        IEnumerable<Randevu> randevular = _veri.Randevular
            .Where(r => secilenDurum is null || r.Durum == secilenDurum)
            .OrderBy(r => r.Baslangic);

        _tablo.Rows.Clear();
        foreach (Randevu randevu in randevular)
        {
            int satir = _tablo.Rows.Add(
                $"{randevu.Baslangic:dd.MM.yyyy} {CalismaSaati.GunAdi(randevu.Baslangic.DayOfWeek)}",
                $"{randevu.Baslangic:HH:mm} - {randevu.Bitis:HH:mm}  ({randevu.SureDakika} dk)",
                randevu.HastaAdi,
                randevu.CalisanAdi,
                SubeAdi(randevu.SubeId),
                randevu.HizmetAdi,
                randevu.UcretMetni,
                randevu.Durum.GorunenAd(),
                randevu.Not ?? "");

            DataGridViewRow satirNesnesi = _tablo.Rows[satir];
            satirNesnesi.Tag = randevu;

            DataGridViewCell durumHucresi = satirNesnesi.Cells["durum"];
            durumHucresi.Style.ForeColor = Tema.DurumRengi(randevu.Durum);
            durumHucresi.Style.SelectionForeColor = Tema.DurumRengi(randevu.Durum);
            if (randevu.SonIslemYapan is not null)
                durumHucresi.ToolTipText = $"Son işlem: {randevu.SonIslemYapan}, {randevu.SonIslemZamani:dd.MM.yyyy HH:mm}";
        }

        _sayac.Text = $"{_tablo.Rows.Count} randevu";
        ButonlariGuncelle();
    }

    /// <summary>Tablo yenilendikten sonra aynı randevuyu tekrar seçili yapar.</summary>
    private void RandevuyuSec(Guid randevuId)
    {
        foreach (DataGridViewRow satir in _tablo.Rows)
        {
            if (satir.Tag is Randevu r && r.Id == randevuId)
            {
                _tablo.CurrentCell = satir.Cells[0];
                break;
            }
        }
    }

    /// <summary>
    /// Butonları seçili randevunun durumuna ve kullanıcının yetkisine göre açar/kapatır.
    /// Yetki kontrolü kullanıcının kendi sınıfındaki (Hasta, Calisan, Yonetici) override edilmiş
    /// metotla yapılır — hangi rolde olduğunu burada bilmemize gerek yok (çok biçimlilik).
    /// </summary>
    private void ButonlariGuncelle()
    {
        Randevu? r = SeciliRandevu;
        Kullanici? k = _islemYapan;

        if (r is null || k is null)
        {
            _onaylaButon.Enabled = _reddetButon.Enabled = _iptalButon.Enabled = _tamamlaButon.Enabled = false;
            return;
        }

        bool onaylayabilir = k.RandevuOnaylayabilirMi(r);
        _onaylaButon.Enabled = onaylayabilir && r.GecisMumkunMu(RandevuDurumu.Onaylandi);
        _reddetButon.Enabled = onaylayabilir && r.GecisMumkunMu(RandevuDurumu.Reddedildi);
        _tamamlaButon.Enabled = onaylayabilir && r.GecisMumkunMu(RandevuDurumu.Tamamlandi);
        _iptalButon.Enabled = k.RandevuIptalEdebilirMi(r) && r.GecisMumkunMu(RandevuDurumu.IptalEdildi);
    }

    // =====================================================================
    // Onay mekanizması işlemleri
    // =====================================================================

    private void Onayla() =>
        IslemYap("Randevu onaylandı.", (randevu, yapan) =>
        {
            randevu.Onayla(yapan);
            return true;
        });

    private void Reddet() =>
        IslemYap("Randevu reddedildi.", (randevu, yapan) =>
        {
            using var form = new MetinGirisFormu("Randevuyu Reddet", "Reddetme sebebi:", zorunlu: true);
            if (form.ShowDialog(this) != DialogResult.OK)
                return false; // kullanıcı vazgeçti

            randevu.Reddet(yapan, form.Metin);
            return true;
        });

    private void IptalEt() =>
        IslemYap("Randevu iptal edildi.", (randevu, yapan) =>
        {
            using var form = new MetinGirisFormu("Randevuyu İptal Et", "İptal sebebi:", zorunlu: false);
            if (form.ShowDialog(this) != DialogResult.OK)
                return false;

            randevu.IptalEt(yapan, form.Metin);
            return true;
        });

    private void Tamamla() =>
        IslemYap("Randevu tamamlandı olarak işaretlendi.", (randevu, yapan) =>
        {
            randevu.Tamamla(yapan);
            return true;
        });

    /// <summary>
    /// Dört işlemin ortak kısmı: seçim kontrolü, hata yakalama, tabloyu yenileme ve bilgi mesajı.
    /// islem: randevu ve işlemi yapan kullanıcıyı alıp işlemi yapan fonksiyon;
    /// kullanıcı vazgeçerse false döndürür.
    /// </summary>
    private void IslemYap(string basariMesaji, Func<Randevu, Kullanici, bool> islem)
    {
        if (_islemYapan is not { } yapan)
        {
            Tema.HataGoster("İşlem yapan kullanıcı bulunamadı.");
            return;
        }
        if (SeciliRandevu is not { } randevu)
        {
            Tema.HataGoster("Lütfen tablodan bir randevu seçin.");
            return;
        }

        try
        {
            if (!islem(randevu, yapan))
                return;

            TabloyuDoldur();
            RandevuyuSec(randevu.Id);
            Tema.BilgiGoster($"{basariMesaji}\n\n{randevu.Baslangic:dd.MM.yyyy HH:mm} · {randevu.HizmetAdi} · {randevu.HastaAdi}");
        }
        catch (Exception hata) when (hata is InvalidOperationException or UnauthorizedAccessException or ArgumentException)
        {
            // Durum geçişi veya yetki kuralı ihlal edildiyse Randevu sınıfı hata fırlatır.
            Tema.HataGoster(hata.Message);
        }
    }

    // =====================================================================
    // Diğer
    // =====================================================================

    private void YeniRandevu()
    {
        if (!_veri.Hastalar.Any())
        {
            Tema.HataGoster("Sistemde kayıtlı hasta yok.");
            return;
        }

        using var form = new RandevuFormu(_veri);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            TabloyuDoldur();
            if (form.OlusturulanRandevu is not null)
                RandevuyuSec(form.OlusturulanRandevu.Id);
        }
    }

    private string SubeAdi(Guid subeId)
    {
        Kurum? kurum = _veri.SubeninKurumu(subeId);
        Sube? sube = _veri.SubeBul(subeId);
        return kurum is null || sube is null ? "-" : $"{kurum.Ad} / {sube.Ad}";
    }
}
