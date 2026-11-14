using System.Drawing.Drawing2D;

namespace RandevuSistemi.Arayuz.Kontroller;

/// <summary>
/// Ana sayfadaki tıklanabilir kısayol kartı: simge + başlık + kısa açıklama.
/// Örnek: [+]  "Randevu Al"  "Uygun doktor ve saati seçin"
/// KartPanel'den türer; fareyle üzerine gelince kenarı turkuaz olur.
/// </summary>
public class HizliIslemKarti : KartPanel
{
    private bool _fareUzerinde;

    public HizliIslemKarti(string ikon, string baslik, string aciklama, Action tiklaninca)
    {
        Ikon = ikon;
        Baslik = baslik;
        Aciklama = aciklama;
        Cursor = Cursors.Hand;
        Height = 76;
        Dock = DockStyle.Top;
        Click += (_, _) => tiklaninca();
    }

    public string Ikon { get; }
    public string Baslik { get; }
    public string Aciklama { get; }

    protected override void OnMouseEnter(EventArgs e)
    {
        _fareUzerinde = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _fareUzerinde = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        if (_fareUzerinde)
        {
            using GraphicsPath yol = Tema.YuvarlakYol(new Rectangle(0, 0, Width - 1, Height - 1), KoseYaricapi);
            using var kalem = new Pen(Tema.Vurgu, 1.5f);
            g.DrawPath(kalem, yol);
        }

        var ikonKutusu = new Rectangle(16, (Height - 44) / 2, 44, 44);
        using (GraphicsPath kutu = Tema.YuvarlakYol(ikonKutusu, 10))
        using (var firca = new SolidBrush(Tema.VurguAcik))
            g.FillPath(firca, kutu);
        TextRenderer.DrawText(g, Ikon, Tema.Ikon, ikonKutusu, Tema.Vurgu,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        int x = ikonKutusu.Right + 14;
        TextRenderer.DrawText(g, Baslik, Tema.Kalin, new Rectangle(x, Height / 2 - 22, Width - x - 30, 22), Tema.Yazi,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(g, Aciklama, Tema.Kucuk, new Rectangle(x, Height / 2, Width - x - 30, 20), Tema.SolukYazi,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        // Sağda ok işareti
        TextRenderer.DrawText(g, "", Tema.Ikon, new Rectangle(Width - 34, 0, 24, Height), Tema.SolukYazi,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}
