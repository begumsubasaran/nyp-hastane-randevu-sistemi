using System.Drawing.Drawing2D;

namespace RandevuSistemi.Arayuz.Kontroller;

/// <summary>
/// Beyaz, köşeleri yuvarlatılmış, ince çerçeveli "kart" paneli.
/// Panel sınıfından türer; arka planını OnPaintBackground'u override ederek kendisi çizer.
/// İçine konan kontroller (tablo, liste...) Padding sayesinde köşelerden içeride durur.
/// </summary>
public class KartPanel : Panel
{
    public KartPanel()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        BackColor = Tema.Kart;  // içindeki kontroller beyaz zemini devralır
        Padding = new Padding(16);
        Margin = new Padding(0);
    }

    public int KoseYaricapi { get; set; } = 12;

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Tema.Arkaplan); // köşelerin dışı: ebeveynin rengi
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var alan = new Rectangle(0, 0, Width - 1, Height - 1);
        using GraphicsPath yol = Tema.YuvarlakYol(alan, KoseYaricapi);
        using (var firca = new SolidBrush(BackColor))
            g.FillPath(firca, yol);
        using (var kalem = new Pen(Tema.Cizgi))
            g.DrawPath(kalem, yol);
    }
}
