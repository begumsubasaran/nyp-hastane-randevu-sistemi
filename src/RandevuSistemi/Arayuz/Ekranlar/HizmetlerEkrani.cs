using RandevuSistemi.Arayuz.Formlar;
using RandevuSistemi.Models.Hizmetler;
using RandevuSistemi.Models.Kullanicilar;
using RandevuSistemi.Models.Randevular;
using RandevuSistemi.Servisler;
using RandevuSistemi.Veri;

namespace RandevuSistemi.Arayuz.Ekranlar;

/// <summary>
/// Şubelerin sağlık hizmetlerini (süre ve ücretleriyle) listeler.
/// "Tüm şubeler" seçilirse bütün şubelerin hizmetleri, hangi şubeye ait oldukları ile birlikte görünür.
/// Yeni hizmet eklenebilir veya seçili hizmet silinebilir.
/// Hizmet silinince o hizmete alınmış ileri tarihli randevular (onay alınarak) iptal edilir.
/// </summary>
public class HizmetlerEkrani : UserControl
{
    private const string TumSubeler = "Tüm şubeler";

    private readonly UygulamaVerisi _veri;
    private readonly Kullanici _islemYapan;
    private readonly ComboBox _subeKutusu;
    private readonly DataGridView _tablo;

    /// <summary>Tablodaki her satırın hangi şubenin hangi hizmeti olduğunu tutar.</summary>
    private sealed record HizmetSatiri(SubeSecenegi Secenek, SaglikHizmeti Hizmet);

    /// <param name="islemYapan">Hizmet silinince iptal edilen randevularda "son işlemi yapan" olarak kaydedilir.</param>
    public HizmetlerEkrani(UygulamaVerisi veri, Kullanici islemYapan)
    {
        _veri = veri;
        _islemYapan = islemYapan;
        BackColor = Tema.Arkaplan;

        var yerlesim = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        yerlesim.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        yerlesim.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // ---- Üst çubuk ----
        var ustCubuk = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        ustCubuk.Controls.Add(new Label
        {
            Text = "Şube:",
            AutoSize = true,
            Margin = new Padding(0, 9, 6, 0),
            ForeColor = Tema.SolukYazi
        });
        _subeKutusu = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 340,
            DropDownWidth = 400,
            Margin = new Padding(0, 5, 16, 0)
        };
        _subeKutusu.Items.Add(TumSubeler);
        foreach (SubeSecenegi secenek in SubeSecenegi.Tumu(_veri))
            _subeKutusu.Items.Add(secenek); // ComboBox ekranda secenek.ToString() gösterir
        ustCubuk.Controls.Add(_subeKutusu);

        Button yeniButon = Tema.Buton("+ Yeni Hizmet");
        Button silButon = Tema.Buton("Sil", birincil: false);
        yeniButon.Click += (_, _) => YeniHizmet();
        silButon.Click += (_, _) => SeciliHizmetiSil();
        ustCubuk.Controls.Add(yeniButon);
        ustCubuk.Controls.Add(silButon);
        yerlesim.Controls.Add(ustCubuk, 0, 0);

        // ---- Hizmet tablosu ----
        _tablo = new DataGridView();
        Tema.TabloyuBicimlendir(_tablo);
        _tablo.Columns.Add("ad", "Hizmet");
        _tablo.Columns.Add("sube", "Şube");
        _tablo.Columns.Add("kategori", "Kategori");
        _tablo.Columns.Add("sure", "Süre");
        _tablo.Columns.Add("ucret", "Ücret");
        _tablo.Columns.Add("aciklama", "Açıklama");
        _tablo.Columns["ad"]!.FillWeight = 20;
        _tablo.Columns["sube"]!.FillWeight = 26;
        _tablo.Columns["kategori"]!.FillWeight = 13;
        _tablo.Columns["sure"]!.FillWeight = 9;
        _tablo.Columns["ucret"]!.FillWeight = 11;
        _tablo.Columns["aciklama"]!.FillWeight = 21;
        _tablo.Columns["ucret"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        yerlesim.Controls.Add(Tema.KartaKoy(_tablo), 0, 1);

        Controls.Add(yerlesim);

        _subeKutusu.SelectedIndexChanged += (_, _) => TabloyuDoldur();
        _subeKutusu.SelectedIndex = 0;
    }

    /// <summary>Kutuda belirli bir şube seçiliyse onu, "Tüm şubeler" seçiliyse null döndürür.</summary>
    private SubeSecenegi? SeciliSube => _subeKutusu.SelectedItem as SubeSecenegi;

    private void TabloyuDoldur()
    {
        _tablo.Rows.Clear();

        // Belirli bir şube seçiliyse sadece onu, değilse tüm şubeleri dolaş.
        List<SubeSecenegi> subeler = SeciliSube is { } secili
            ? new List<SubeSecenegi> { secili }
            : SubeSecenegi.Tumu(_veri);

        foreach (SubeSecenegi secenek in subeler)
        {
            foreach (SaglikHizmeti hizmet in secenek.Sube.Hizmetler)
            {
                int satir = _tablo.Rows.Add(
                    hizmet.Ad,
                    secenek.ToString(),          // "Kurum / Şube"
                    hizmet.Kategori.GorunenAd(),
                    $"{hizmet.SureDakika} dk",
                    hizmet.UcretMetni,
                    hizmet.Aciklama ?? "");
                _tablo.Rows[satir].Tag = new HizmetSatiri(secenek, hizmet);
            }
        }
    }

    private void YeniHizmet()
    {
        if (SeciliSube is null)
        {
            Tema.HataGoster("Yeni hizmet eklemek için yukarıdan bir şube seçin.");
            return;
        }

        using var form = new HizmetFormu(SeciliSube);
        if (form.ShowDialog(this) == DialogResult.OK)
            TabloyuDoldur();
    }

    private void SeciliHizmetiSil()
    {
        if (_tablo.CurrentRow?.Tag is not HizmetSatiri satir)
        {
            Tema.HataGoster("Silmek için tablodan bir hizmet seçin.");
            return;
        }

        var servis = new RandevuServisi(_veri);

        // Silmeden önce: bu hizmete alınmış ileri tarihli randevuları bul ve kullanıcıya göster.
        List<Randevu> iptalEdilecekler = servis.HizmetinGelecekRandevulari(satir.Hizmet);
        string soru = $"'{satir.Secenek}' şubesindeki '{satir.Hizmet.Ad}' hizmeti silinsin mi?\n" +
                      "Bu hizmeti sunan çalışanların listesinden de çıkarılacak.";
        if (iptalEdilecekler.Count > 0)
            soru += $"\n\nBu hizmete alınmış aşağıdaki {iptalEdilecekler.Count} ileri tarihli randevu İPTAL EDİLECEK:\n\n" +
                    Tema.RandevuListesiMetni(iptalEdilecekler) +
                    "\n\nGeçmiş randevular olduğu gibi korunur.";

        DialogResult cevap = MessageBox.Show(soru, "Hizmeti Sil", MessageBoxButtons.YesNo,
            iptalEdilecekler.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Question);
        if (cevap != DialogResult.Yes)
            return;

        List<Randevu> iptalEdilenler;
        try
        {
            iptalEdilenler = servis.HizmetiSil(satir.Secenek.Sube, satir.Hizmet, _islemYapan);
        }
        catch (Exception hata) when (hata is UnauthorizedAccessException or InvalidOperationException)
        {
            Tema.HataGoster(hata.Message);
            return;
        }

        TabloyuDoldur();
        Tema.BilgiGoster($"'{satir.Hizmet.Ad}' hizmeti silindi." +
            (iptalEdilenler.Count == 0
                ? ""
                : $"\n\n{iptalEdilenler.Count} ileri tarihli randevu iptal edildi:\n" +
                  Tema.RandevuListesiMetni(iptalEdilenler)));
    }
}
