using RandevuSistemi.Models.Hizmetler;
using RandevuSistemi.Models.Kurumlar;

namespace RandevuSistemi.Arayuz.Ekranlar;

/// <summary>
/// Kurumları ve şubelerini ağaç yapısında gösterir.
/// Bir şube seçildiğinde sağ tarafta çalışma saatleri, bölümleri ve hizmetleri listelenir.
/// </summary>
public class KurumlarEkrani : UserControl
{
    private readonly TreeView _agac;
    private readonly Label _secilenBaslik;
    private readonly Label _secilenBilgi;
    private readonly ListBox _saatListesi;
    private readonly ListBox _bolumListesi;
    private readonly ListBox _hizmetListesi;

    public KurumlarEkrani(IReadOnlyList<Kurum> kurumlar)
    {
        BackColor = Tema.Arkaplan;

        var yerlesim = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        yerlesim.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        yerlesim.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));

        // ---- Sol: kurum / şube ağacı ----
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
        yerlesim.Controls.Add(KartIcineAl(_agac), 0, 0);

        // ---- Sağ: seçilen öğenin ayrıntıları ----
        var detay = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 8, BackColor = Tema.Kart };
        detay.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));   // başlık
        detay.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));   // bilgi
        detay.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));   // "Çalışma Saatleri"
        detay.RowStyles.Add(new RowStyle(SizeType.Percent, 38));    // saat listesi
        detay.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));   // "Bölümler"
        detay.RowStyles.Add(new RowStyle(SizeType.Percent, 27));    // bölüm listesi
        detay.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));   // "Hizmetler"
        detay.RowStyles.Add(new RowStyle(SizeType.Percent, 35));    // hizmet listesi

        _secilenBaslik = new Label { Dock = DockStyle.Fill, Font = Tema.AltBaslik, ForeColor = Tema.Yazi };
        _secilenBilgi = new Label { Dock = DockStyle.Fill, ForeColor = Tema.SolukYazi };
        _saatListesi = YeniListe();
        _bolumListesi = YeniListe();
        _hizmetListesi = YeniListe();

        detay.Controls.Add(_secilenBaslik, 0, 0);
        detay.Controls.Add(_secilenBilgi, 0, 1);
        detay.Controls.Add(Tema.BolumBasligi("Çalışma Saatleri"), 0, 2);
        detay.Controls.Add(_saatListesi, 0, 3);
        detay.Controls.Add(Tema.BolumBasligi("Bölümler"), 0, 4);
        detay.Controls.Add(_bolumListesi, 0, 5);
        detay.Controls.Add(Tema.BolumBasligi("Hizmetler"), 0, 6);
        detay.Controls.Add(_hizmetListesi, 0, 7);
        yerlesim.Controls.Add(KartIcineAl(detay), 1, 0);

        Controls.Add(yerlesim);

        AgaciDoldur(kurumlar);
    }

    private void AgaciDoldur(IReadOnlyList<Kurum> kurumlar)
    {
        _agac.Nodes.Clear();
        TreeNode? ilkSubeDugumu = null;

        foreach (Kurum kurum in kurumlar)
        {
            // Tag özelliğine nesnenin kendisini koyuyoruz; seçildiğinde geri alacağız.
            var kurumDugumu = new TreeNode($"{kurum.Ad}  ({kurum.TurAdi})") { Tag = kurum };
            foreach (Sube sube in kurum.Subeler)
            {
                var subeDugumu = new TreeNode(sube.Ad) { Tag = sube };
                kurumDugumu.Nodes.Add(subeDugumu);
                ilkSubeDugumu ??= subeDugumu;
            }
            _agac.Nodes.Add(kurumDugumu);
        }

        _agac.ExpandAll();
        if (ilkSubeDugumu is not null)
        {
            _agac.SelectedNode = ilkSubeDugumu;
            SecimiGoster(ilkSubeDugumu.Tag);
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

        switch (secilen)
        {
            case Sube sube:
                _secilenBaslik.Text = sube.Ad;
                _secilenBilgi.Text = sube.Adres;

                if (sube.CalismaSaatleri.Count == 0)
                    _saatListesi.Items.Add("Çalışma saati tanımlanmamış");
                foreach (CalismaSaati saat in sube.CalismaSaatleri)
                    _saatListesi.Items.Add(saat);

                if (sube.Bolumler.Count == 0)
                    _bolumListesi.Items.Add("Bölüm tanımlanmamış");
                foreach (Bolum bolum in sube.Bolumler)
                    _bolumListesi.Items.Add(bolum);

                if (sube.Hizmetler.Count == 0)
                    _hizmetListesi.Items.Add("Hizmet tanımlanmamış");
                foreach (SaglikHizmeti hizmet in sube.Hizmetler)
                    _hizmetListesi.Items.Add(hizmet); // "Ad (süre, ücret)" şeklinde görünür
                break;

            case Kurum kurum:
                _secilenBaslik.Text = kurum.Ad;
                _secilenBilgi.Text = $"{kurum.TurAdi} · {kurum.Subeler.Count} şube" +
                                     (kurum.CokSubeliMi ? " (çok şubeli)" : " (tek şubeli)");
                _saatListesi.Items.Add("Ayrıntıları görmek için soldan bir şube seçin.");
                break;
        }
    }

    private static ListBox YeniListe() => new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None,
        IntegralHeight = false,
        Font = new Font("Segoe UI", 11f),
        SelectionMode = SelectionMode.None
    };

    /// <summary>Kontrolü beyaz arkaplanlı, kenar boşluklu bir "kart" panelin içine koyar.</summary>
    private static Panel KartIcineAl(Control icerik)
    {
        var kart = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Tema.Kart,
            Padding = new Padding(16),
            Margin = new Padding(0, 0, 12, 0)
        };
        icerik.Dock = DockStyle.Fill;
        kart.Controls.Add(icerik);
        return kart;
    }
}
