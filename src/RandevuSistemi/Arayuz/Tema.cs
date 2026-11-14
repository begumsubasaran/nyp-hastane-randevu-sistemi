using System.Drawing.Drawing2D;
using RandevuSistemi.Arayuz.Kontroller;
using RandevuSistemi.Models.Randevular;

namespace RandevuSistemi.Arayuz;

/// <summary>
/// Uygulamanın renkleri, yazı tipleri ve sık kullanılan kontrol ayarları.
/// Tüm ekranlar buradan beslendiği için görünüm tek yerden değiştirilebilir.
///
/// Görünüm: açık renkli, beyaz kartlı, turkuaz vurgulu modern bir sağlık uygulaması tarzı.
/// </summary>
public static class Tema
{
    // ---- Renkler ----
    public static readonly Color Vurgu = Color.FromArgb(0, 150, 160);         // turkuaz: butonlar, seçili menü
    public static readonly Color VurguKoyu = Color.FromArgb(0, 118, 128);     // üzerine gelince
    public static readonly Color VurguAcik = Color.FromArgb(226, 244, 245);   // seçili satır / menü zemini
    public static readonly Color Lacivert = Color.FromArgb(24, 52, 94);       // başlıklar
    public static readonly Color Arkaplan = Color.FromArgb(243, 246, 249);
    public static readonly Color Kart = Color.White;
    public static readonly Color Yazi = Color.FromArgb(33, 43, 54);
    public static readonly Color SolukYazi = Color.FromArgb(112, 122, 138);
    public static readonly Color Cizgi = Color.FromArgb(228, 232, 238);
    public static readonly Color PasifZemin = Color.FromArgb(236, 239, 243);

    // Eski adlar (bazı ekranlar kullanıyor)
    public static readonly Color Birincil = Lacivert;
    public static readonly Color BirincilKoyu = VurguKoyu;

    // ---- Yazı tipleri ----
    public static readonly Font Normal = new("Segoe UI", 10f);
    public static readonly Font Kalin = new("Segoe UI Semibold", 10f);
    public static readonly Font Kucuk = new("Segoe UI", 9f);
    public static readonly Font AltBaslik = new("Segoe UI Semibold", 12f);
    public static readonly Font Baslik = new("Segoe UI Semibold", 17f);

    /// <summary>
    /// Windows'un simge yazı tipi. Harf yerine küçük simgeler içerir (takvim, kişi, kalp...).
    /// Windows 10/11'de hazır gelir.
    /// </summary>
    public static readonly Font Ikon = new("Segoe MDL2 Assets", 13f);

    /// <summary>Kullanılan simgeler (Segoe MDL2 Assets karakter kodları).</summary>
    public static class Ikonlar
    {
        public const string AnaSayfa = "";
        public const string Takvim = "";
        public const string Kurum = "";
        public const string Saglik = "";
        public const string Kisiler = "";
        public const string Kisi = "";
        public const string Saat = "";
        public const string Cikis = "";
        public const string Ekle = "";
        public const string Onay = "";
        public const string Bekle = "";
        public const string Liste = "";
    }

    // =====================================================================
    // Kontrol üreticileri
    // =====================================================================

    /// <summary>
    /// Ekranlardaki işlem butonu (köşeleri yuvarlatılmış). birincil = true ise turkuaz dolgulu
    /// (Kaydet, Yeni gibi), false ise beyaz zeminli ve çerçeveli (İptal, Sil gibi) olur.
    /// </summary>
    public static Button Buton(string metin, bool birincil = true) => new YuvarlakButon
    {
        Text = metin,
        AutoSize = true,
        MinimumSize = new Size(110, 36),
        Padding = new Padding(12, 0, 12, 0),
        Margin = new Padding(0, 0, 8, 0),
        DolguRengi = birincil ? Vurgu : Kart,
        YaziRengi = birincil ? Color.White : Yazi,
        KenarRengi = birincil ? Color.Empty : Cizgi
    };

    /// <summary>Bir kontrolü beyaz, köşeleri yuvarlak bir kartın içine yerleştirir.</summary>
    public static KartPanel KartaKoy(Control icerik, int icBosluk = 12)
    {
        var kart = new KartPanel { Dock = DockStyle.Fill, Padding = new Padding(icBosluk) };
        icerik.Dock = DockStyle.Fill;
        kart.Controls.Add(icerik);
        return kart;
    }

    /// <summary>Ekranlardaki küçük bölüm başlıkları ("Çalışma Saatleri" gibi).</summary>
    public static Label BolumBasligi(string metin) => new()
    {
        Text = metin,
        Dock = DockStyle.Fill,
        Font = Kalin,
        ForeColor = Lacivert,
        TextAlign = ContentAlignment.BottomLeft
    };

    // =====================================================================
    // Mesaj kutuları
    // =====================================================================

    /// <summary>Hata mesajını standart bir uyarı kutusunda gösterir.</summary>
    public static void HataGoster(string mesaj) =>
        MessageBox.Show(mesaj, "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    /// <summary>İşlemin başarıyla tamamlandığını bilgi kutusunda gösterir.</summary>
    public static void BilgiGoster(string mesaj) =>
        MessageBox.Show(mesaj, "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);

    /// <summary>
    /// Randevu listesini mesaj kutusunda gösterilecek kısa metne çevirir (en fazla 10 satır).
    /// Örnek: "• 16.11.2026 09:00  Kardiyoloji Muayenesi  (Ayşe Yılmaz)"
    /// </summary>
    public static string RandevuListesiMetni(IReadOnlyCollection<Randevu> randevular)
    {
        string liste = string.Join("\n", randevular
            .Take(10)
            .Select(r => $"• {r.Baslangic:dd.MM.yyyy HH:mm}  {r.HizmetAdi}  ({r.HastaAdi})"));
        return randevular.Count > 10 ? $"{liste}\n... ve {randevular.Count - 10} randevu daha" : liste;
    }

    /// <summary>
    /// "Şu randevular iptal edilecek, devam edilsin mi?" sorusunu sorar.
    /// Liste boşsa soru sormadan true döndürür.
    /// </summary>
    public static bool IptalOnayiAl(IReadOnlyCollection<Randevu> iptalEdilecekler, string degisiklik)
    {
        if (iptalEdilecekler.Count == 0)
            return true;

        DialogResult cevap = MessageBox.Show(
            $"{degisiklik}\n\nBu değişiklik nedeniyle aşağıdaki {iptalEdilecekler.Count} ileri tarihli randevu " +
            $"yeni uygunluk saatlerinin dışında kalacak ve İPTAL EDİLECEK:\n\n{RandevuListesiMetni(iptalEdilecekler)}\n\n" +
            "Yeni saatlere uyan randevular korunacak. Devam edilsin mi?",
            "Randevular İptal Edilecek", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        return cevap == DialogResult.Yes;
    }

    // =====================================================================
    // Renk ve çizim yardımcıları
    // =====================================================================

    /// <summary>Randevu durumlarını renkle ayırt etmek için.</summary>
    public static Color DurumRengi(RandevuDurumu durum) => durum switch
    {
        RandevuDurumu.Beklemede => Color.FromArgb(214, 137, 16),    // turuncu
        RandevuDurumu.Onaylandi => Color.FromArgb(25, 135, 84),     // yeşil
        RandevuDurumu.Reddedildi => Color.FromArgb(200, 35, 51),    // kırmızı
        RandevuDurumu.IptalEdildi => Color.FromArgb(108, 117, 125), // gri
        RandevuDurumu.Tamamlandi => Color.FromArgb(13, 110, 253),   // mavi
        _ => Yazi
    };

    /// <summary>Bir rengi beyazla karıştırarak açık tonunu üretir (oran 0..1, büyüdükçe daha açık).</summary>
    public static Color Acik(Color renk, float oran = 0.86f) => Color.FromArgb(
        (int)(renk.R + (255 - renk.R) * oran),
        (int)(renk.G + (255 - renk.G) * oran),
        (int)(renk.B + (255 - renk.B) * oran));

    /// <summary>Bir rengi koyulaştırır (oran 0..1).</summary>
    public static Color Koyu(Color renk, float oran = 0.12f) => Color.FromArgb(
        (int)(renk.R * (1 - oran)),
        (int)(renk.G * (1 - oran)),
        (int)(renk.B * (1 - oran)));

    /// <summary>Köşeleri yuvarlatılmış dikdörtgen çizim yolu oluşturur.</summary>
    public static GraphicsPath YuvarlakYol(Rectangle alan, int yaricap)
    {
        var yol = new GraphicsPath();
        if (yaricap <= 0)
        {
            yol.AddRectangle(alan);
            return yol;
        }

        int cap = Math.Min(yaricap * 2, Math.Min(alan.Width, alan.Height));
        yol.AddArc(alan.X, alan.Y, cap, cap, 180, 90);
        yol.AddArc(alan.Right - cap, alan.Y, cap, cap, 270, 90);
        yol.AddArc(alan.Right - cap, alan.Bottom - cap, cap, cap, 0, 90);
        yol.AddArc(alan.X, alan.Bottom - cap, cap, cap, 90, 90);
        yol.CloseFigure();
        return yol;
    }

    /// <summary>
    /// Durum metnini renkli bir "hap" (yuvarlak etiket) olarak çizer.
    /// Örnek: yeşil zemin üzerinde "Onaylandı".
    /// </summary>
    public static void DurumHapiCiz(Graphics g, Rectangle alan, string metin, Color renk)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Size yaziBoyutu = TextRenderer.MeasureText(metin, Kucuk);
        int genislik = yaziBoyutu.Width + 18;
        int yukseklik = 24;
        var hap = new Rectangle(alan.X, alan.Y + (alan.Height - yukseklik) / 2, genislik, yukseklik);

        using (GraphicsPath yol = YuvarlakYol(hap, yukseklik / 2))
        using (var firca = new SolidBrush(Acik(renk)))
            g.FillPath(firca, yol);

        TextRenderer.DrawText(g, metin, Kucuk, hap, renk,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }

    // =====================================================================
    // Tablolar
    // =====================================================================

    /// <summary>Tablolara (DataGridView) ortak, sade ve ferah bir görünüm verir.</summary>
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

        // Başlık satırı: beyaz zemin, gri kalın yazı
        tablo.EnableHeadersVisualStyles = false;
        tablo.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        tablo.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        tablo.ColumnHeadersHeight = 44;
        tablo.ColumnHeadersDefaultCellStyle.BackColor = Kart;
        tablo.ColumnHeadersDefaultCellStyle.ForeColor = SolukYazi;
        tablo.ColumnHeadersDefaultCellStyle.Font = Kalin;
        tablo.ColumnHeadersDefaultCellStyle.SelectionBackColor = Kart;
        tablo.ColumnHeadersDefaultCellStyle.SelectionForeColor = SolukYazi;
        tablo.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);

        // Satırlar: yüksek, sade; seçili satır açık turkuaz
        tablo.RowTemplate.Height = 42;
        tablo.DefaultCellStyle.Font = Normal;
        tablo.DefaultCellStyle.BackColor = Kart;
        tablo.DefaultCellStyle.ForeColor = Yazi;
        tablo.DefaultCellStyle.SelectionBackColor = VurguAcik;
        tablo.DefaultCellStyle.SelectionForeColor = Yazi;
        tablo.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        tablo.AlternatingRowsDefaultCellStyle.BackColor = Kart;
    }
}
