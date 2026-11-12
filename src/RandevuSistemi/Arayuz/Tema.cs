namespace RandevuSistemi.Arayuz;

/// <summary>
/// Uygulamanın renkleri, yazı tipleri ve sık kullanılan kontrol ayarları.
/// Tüm ekranlar buradan beslendiği için görünüm tek yerden değiştirilebilir.
/// </summary>
public static class Tema
{
    // ---- Renkler ----
    public static readonly Color Birincil = Color.FromArgb(30, 64, 110);      // yan menü
    public static readonly Color BirincilKoyu = Color.FromArgb(20, 45, 80);   // seçili menü
    public static readonly Color Vurgu = Color.FromArgb(0, 137, 123);         // önemli butonlar
    public static readonly Color Arkaplan = Color.FromArgb(244, 246, 249);
    public static readonly Color Kart = Color.White;
    public static readonly Color Yazi = Color.FromArgb(33, 37, 41);
    public static readonly Color SolukYazi = Color.FromArgb(108, 117, 125);
    public static readonly Color Cizgi = Color.FromArgb(222, 226, 230);

    // ---- Yazı tipleri ----
    public static readonly Font Normal = new("Segoe UI", 10f);
    public static readonly Font Kalin = new("Segoe UI Semibold", 10f);
    public static readonly Font AltBaslik = new("Segoe UI Semibold", 12f);
    public static readonly Font Baslik = new("Segoe UI Semibold", 16f);

    /// <summary>Yan menüdeki gezinme butonu.</summary>
    public static Button MenuButonu(string metin)
    {
        var buton = new Button
        {
            Text = "   " + metin,
            Width = 220,
            Height = 46,
            Margin = new Padding(0),
            FlatStyle = FlatStyle.Flat,
            BackColor = Birincil,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 11f),
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor = Cursors.Hand
        };
        buton.FlatAppearance.BorderSize = 0;
        buton.FlatAppearance.MouseOverBackColor = BirincilKoyu;
        return buton;
    }

    /// <summary>
    /// Ekranlardaki işlem butonu. birincil = true ise renkli (Kaydet, Yeni gibi),
    /// false ise beyaz zeminli (İptal, Sil gibi) olur.
    /// </summary>
    public static Button Buton(string metin, bool birincil = true)
    {
        var buton = new Button
        {
            Text = metin,
            AutoSize = true,
            MinimumSize = new Size(110, 34),
            Padding = new Padding(10, 0, 10, 0),
            Margin = new Padding(0, 0, 8, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = birincil ? Vurgu : Kart,
            ForeColor = birincil ? Color.White : Yazi,
            Font = Kalin,
            Cursor = Cursors.Hand
        };
        buton.FlatAppearance.BorderSize = birincil ? 0 : 1;
        buton.FlatAppearance.BorderColor = Cizgi;
        return buton;
    }

    /// <summary>Hata mesajını standart bir uyarı kutusunda gösterir.</summary>
    public static void HataGoster(string mesaj) =>
        MessageBox.Show(mesaj, "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    /// <summary>Ekranlardaki küçük bölüm başlıkları ("Çalışma Saatleri" gibi).</summary>
    public static Label BolumBasligi(string metin) => new()
    {
        Text = metin,
        Dock = DockStyle.Fill,
        Font = Kalin,
        ForeColor = SolukYazi,
        TextAlign = ContentAlignment.BottomLeft
    };

    /// <summary>Tablolara (DataGridView) ortak, sade bir görünüm verir.</summary>
    public static void TabloyuBicimlendir(DataGridView tablo)
    {
        tablo.Dock = DockStyle.Fill;
        tablo.ReadOnly = true;
        tablo.AllowUserToAddRows = false;
        tablo.AllowUserToDeleteRows = false;
        tablo.AllowUserToResizeRows = false;
        tablo.RowHeadersVisible = false;
        tablo.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        tablo.MultiSelect = false;
        tablo.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        tablo.BackgroundColor = Kart;
        tablo.BorderStyle = BorderStyle.None;
        tablo.GridColor = Cizgi;
        tablo.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

        tablo.EnableHeadersVisualStyles = false;
        tablo.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        tablo.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        tablo.ColumnHeadersHeight = 38;
        tablo.ColumnHeadersDefaultCellStyle.BackColor = Birincil;
        tablo.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        tablo.ColumnHeadersDefaultCellStyle.Font = Kalin;
        tablo.ColumnHeadersDefaultCellStyle.SelectionBackColor = Birincil;

        tablo.RowTemplate.Height = 34;
        tablo.DefaultCellStyle.Font = Normal;
        tablo.DefaultCellStyle.ForeColor = Yazi;
        tablo.DefaultCellStyle.SelectionBackColor = Color.FromArgb(214, 228, 245);
        tablo.DefaultCellStyle.SelectionForeColor = Yazi;
        tablo.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
        tablo.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 252);
    }
}
