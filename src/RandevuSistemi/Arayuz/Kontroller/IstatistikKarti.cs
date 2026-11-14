using System.Drawing.Drawing2D;

namespace RandevuSistemi.Arayuz.Kontroller;

/// <summary>
/// Ana sayfadaki özet kartı: solda renkli daire içinde simge, yanında büyük bir sayı ve açıklaması.
/// Örnek: [takvim simgesi]  3  "Yaklaşan randevu"
/// KartPanel'den TÜRER (kart görünümünü ondan alır) ve içeriğini OnPaint'te çizer.
/// </summary>
public class IstatistikKarti : KartPanel
{
    private static readonly Font DegerYazisi = new("Segoe UI Semibold", 22f);

    public IstatistikKarti(string ikon, string deger, string baslik, Color renk)
    {
        Ikon = ikon;
        Deger = deger;
        Baslik = baslik;
        Renk = renk;
        Margin = new Padding(0, 0, 14, 0);
    }

    public string Ikon { get; }
    public string Deger { get; }
    public string Baslik { get; }
    public Color Renk { get; }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Renkli daire + simge
        int cap = 48;
        var daire = new Rectangle(20, (Height - cap) / 2, cap, cap);
        using (var firca = new SolidBrush(Tema.Acik(Renk)))
            g.FillEllipse(firca, daire);
        TextRenderer.DrawText(g, Ikon, Tema.Ikon, daire, Renk,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        // Sayı ve açıklama
        int x = daire.Right + 16;
        var degerAlani = new Rectangle(x, Height / 2 - 34, Width - x - 10, 40);
        var baslikAlani = new Rectangle(x, Height / 2 + 6, Width - x - 10, 24);
        TextRenderer.DrawText(g, Deger, DegerYazisi, degerAlani, Tema.Lacivert,
            TextFormatFlags.Left | TextFormatFlags.Bottom | TextFormatFlags.SingleLine);
        TextRenderer.DrawText(g, Baslik, Tema.Normal, baslikAlani, Tema.SolukYazi,
            TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis);
    }
}
