namespace RandevuSistemi.Arayuz.Formlar;

/// <summary>
/// Kullanıcıdan kısa bir metin (ör. reddetme veya iptal sebebi) isteyen küçük pencere.
/// zorunlu = true ise boş bırakılıp onaylanamaz.
/// </summary>
public class MetinGirisFormu : Form
{
    private readonly TextBox _metinKutusu;
    private readonly bool _zorunlu;

    public MetinGirisFormu(string baslik, string aciklama, bool zorunlu)
    {
        _zorunlu = zorunlu;

        Text = baslik;
        ClientSize = new Size(440, 230);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = Tema.Normal;
        BackColor = Tema.Kart;

        var tablo = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(20) };
        tablo.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        tablo.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        tablo.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

        var etiket = new Label
        {
            Text = aciklama + (zorunlu ? "" : " (isteğe bağlı)"),
            Dock = DockStyle.Fill,
            ForeColor = Tema.SolukYazi,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _metinKutusu = new TextBox { Dock = DockStyle.Fill, Multiline = true };

        var butonlar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 10, 0, 0) };
        Button tamam = Tema.Buton("Tamam");
        Button iptal = Tema.Buton("Vazgeç", birincil: false);
        iptal.DialogResult = DialogResult.Cancel;
        tamam.Click += (_, _) => Onayla();
        butonlar.Controls.Add(tamam);
        butonlar.Controls.Add(iptal);

        tablo.Controls.Add(etiket, 0, 0);
        tablo.Controls.Add(_metinKutusu, 0, 1);
        tablo.Controls.Add(butonlar, 0, 2);
        Controls.Add(tablo);
        CancelButton = iptal;
    }

    /// <summary>Girilen metin (kırpılmış).</summary>
    public string Metin => _metinKutusu.Text.Trim();

    private void Onayla()
    {
        if (_zorunlu && string.IsNullOrWhiteSpace(_metinKutusu.Text))
        {
            Tema.HataGoster("Bu alan boş bırakılamaz.");
            return;
        }
        DialogResult = DialogResult.OK;
    }
}
