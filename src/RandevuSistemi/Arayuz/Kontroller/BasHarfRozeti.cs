using System.Drawing.Drawing2D;

namespace RandevuSistemi.Arayuz.Kontroller;

/// <summary>
/// Kullanıcının adının baş harflerini turkuaz bir daire içinde gösterir (ör. "AY" → Ayşe Yılmaz).
/// Control sınıfından türeyen, tamamen kendini çizen küçük bir kontrol.
/// </summary>
public class BasHarfRozeti : Control
{
    private static readonly Font HarfYazisi = new("Segoe UI Semibold", 11f);

    public BasHarfRozeti(string ad, string soyad)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        Harfler = $"{IlkHarf(ad)}{IlkHarf(soyad)}";
        Size = new Size(40, 40);
    }

    public string Harfler { get; }

    private static string IlkHarf(string metin) =>
        string.IsNullOrWhiteSpace(metin) ? "" : metin.Trim()[0].ToString().ToUpper(new System.Globalization.CultureInfo("tr-TR"));

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Tema.Kart);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var daire = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var firca = new SolidBrush(Tema.Vurgu))
            g.FillEllipse(firca, daire);
        TextRenderer.DrawText(g, Harfler, HarfYazisi, daire, Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}
