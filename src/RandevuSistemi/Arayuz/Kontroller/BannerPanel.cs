using System.Drawing.Drawing2D;

namespace RandevuSistemi.Arayuz.Kontroller;

/// <summary>
/// Turkuazdan maviye geçişli (gradyan) karşılama şeridi. Ana sayfanın üstünde ve giriş ekranının
/// solunda kullanılır. Başlık ve alt başlık yazılarını kendisi çizer.
/// </summary>
public class BannerPanel : Panel
{
    private static readonly Font BaslikYazisi = new("Segoe UI Semibold", 19f);
    private static readonly Font AltYazi = new("Segoe UI", 10.5f);

    public BannerPanel()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        Margin = new Padding(0);
    }

    public string Baslik { get; set; } = "";
    public string AltBaslik { get; set; } = "";
    public string Ikon { get; set; } = Tema.Ikonlar.Saglik;
    public int KoseYaricapi { get; set; } = 14;

    /// <summary>true ise yazılar dikey olarak ortalanır (giriş ekranında), false ise üstten başlar.</summary>
    public bool Ortala { get; set; }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Tema.Arkaplan);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var alan = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
        using GraphicsPath yol = Tema.YuvarlakYol(alan, KoseYaricapi);
        using var gecis = new LinearGradientBrush(alan, Tema.Vurgu, Color.FromArgb(28, 104, 168), 20f);
        g.FillPath(gecis, yol);

        // Büyük, soluk bir simge (süs): yatay şeritte sağda, dikey panelde (giriş ekranı) sağ altta
        int boyut = Ortala ? (int)(Math.Min(Width, Height) * 0.55) : Height;
        Rectangle ikonAlani = Ortala
            ? new Rectangle(Width - boyut - 12, Height - boyut - 12, boyut, boyut)
            : new Rectangle(Width - (int)(Height * 1.1), 0, Height, Height);
        using var buyukIkon = new Font(Tema.Ikon.FontFamily, Math.Max(24f, boyut * 0.45f));
        TextRenderer.DrawText(g, Ikon, buyukIkon, ikonAlani,
            Tema.Acik(Tema.Vurgu, 0.30f), // TextRenderer saydamlığı desteklemez; açık ton kullanıyoruz
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Font baslikFontu = BaslikYazisi;
        int baslikYuksekligi = TextRenderer.MeasureText(Baslik, baslikFontu).Height;
        int ustBosluk = Ortala ? Math.Max(20, Height / 2 - baslikYuksekligi - 70) : 26;

        var baslikAlani = new Rectangle(28, ustBosluk, Width - 56, baslikYuksekligi + 4);
        var altAlan = new Rectangle(28, baslikAlani.Bottom + 4, Width - 56 - (Ortala ? 0 : Height), Height - baslikAlani.Bottom - 8);

        TextRenderer.DrawText(e.Graphics, Baslik, baslikFontu, baslikAlani, Color.White,
            TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(e.Graphics, AltBaslik, AltYazi, altAlan, Color.FromArgb(232, 246, 248),
            TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak);
    }
}
