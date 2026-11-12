using RandevuSistemi.Models.Kurumlar;
using RandevuSistemi.Models.Randevular;
using RandevuSistemi.Veri;

namespace RandevuSistemi.Arayuz.Ekranlar;

/// <summary>
/// Tüm randevuları tarih sırasıyla listeler; üstteki kutudan duruma göre filtrelenebilir.
/// Her satırda randevu detayları (işlem, süre, ücret, çalışan, tarih/saat) ve durumu görünür.
/// </summary>
public class RandevularEkrani : UserControl
{
    private readonly UygulamaVerisi _veri;
    private readonly ComboBox _durumFiltresi;
    private readonly Label _sayac;
    private readonly DataGridView _tablo;

    public RandevularEkrani(UygulamaVerisi veri)
    {
        _veri = veri;
        BackColor = Tema.Arkaplan;

        var yerlesim = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        yerlesim.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        yerlesim.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // ---- Üst çubuk: durum filtresi ----
        var ustCubuk = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
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
        _tablo.Columns["tarih"]!.FillWeight = 14;
        _tablo.Columns["saat"]!.FillWeight = 11;
        _tablo.Columns["hasta"]!.FillWeight = 12;
        _tablo.Columns["calisan"]!.FillWeight = 20;
        _tablo.Columns["sube"]!.FillWeight = 18;
        _tablo.Columns["hizmet"]!.FillWeight = 14;
        _tablo.Columns["ucret"]!.FillWeight = 9;
        _tablo.Columns["durum"]!.FillWeight = 11;
        _tablo.Columns["ucret"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _tablo.Columns["durum"]!.DefaultCellStyle.Font = Tema.Kalin;
        yerlesim.Controls.Add(_tablo, 0, 1);

        Controls.Add(yerlesim);

        _durumFiltresi.SelectedIndexChanged += (_, _) => TabloyuDoldur();
        _durumFiltresi.SelectedIndex = 0;
    }

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
                randevu.Durum.GorunenAd());

            DataGridViewRow satirNesnesi = _tablo.Rows[satir];
            satirNesnesi.Tag = randevu;
            satirNesnesi.Cells["durum"].Style.ForeColor = Tema.DurumRengi(randevu.Durum);
            satirNesnesi.Cells["durum"].Style.SelectionForeColor = Tema.DurumRengi(randevu.Durum);
        }

        _sayac.Text = $"{_tablo.Rows.Count} randevu";
    }

    private string SubeAdi(Guid subeId)
    {
        Kurum? kurum = _veri.SubeninKurumu(subeId);
        Sube? sube = _veri.SubeBul(subeId);
        return kurum is null || sube is null ? "-" : $"{kurum.Ad} / {sube.Ad}";
    }
}
