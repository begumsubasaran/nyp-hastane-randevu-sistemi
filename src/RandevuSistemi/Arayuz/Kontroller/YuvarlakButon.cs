using System.Drawing.Drawing2D;

namespace RandevuSistemi.Arayuz.Kontroller;

/// <summary>
/// Köşeleri yuvarlatılmış buton.
/// .NET'in Button sınıfından TÜRER ve OnPaint metodunu OVERRIDE ederek kendini kendisi çizer
/// (kalıtım + çok biçimlilik). Fareyle üzerine gelince koyulaşır, pasifken gri görünür.
/// </summary>
public class YuvarlakButon : Button
{
    private bool _fareUzerinde;

    public YuvarlakButon()
    {
        // Çizimi tamamen biz yapacağımızı Windows'a söylüyoruz; titremeyi önlemek için çift tamponlama.
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        Font = Tema.Kalin;
    }

    public Color DolguRengi { get; set; } = Tema.Vurgu;
    public Color YaziRengi { get; set; } = Color.White;
    public Color KenarRengi { get; set; } = Color.Empty;
    public int KoseYaricapi { get; set; } = 8;

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

    protected override void OnEnabledChanged(EventArgs e)
    {
        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        // Köşelerin dışında kalan alan, butonun durduğu panelin rengiyle boyanır.
        g.Clear(Parent?.BackColor ?? Tema.Kart);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Color dolgu = !Enabled ? Tema.PasifZemin
                    : _fareUzerinde ? (DolguRengi == Tema.Kart ? Tema.Arkaplan : Tema.Koyu(DolguRengi))
                    : DolguRengi;
        Color yazi = Enabled ? YaziRengi : Color.FromArgb(160, 166, 175);

        var alan = new Rectangle(0, 0, Width - 1, Height - 1);
        using (GraphicsPath yol = Tema.YuvarlakYol(alan, KoseYaricapi))
        {
            using (var firca = new SolidBrush(dolgu))
                g.FillPath(firca, yol);

            if (KenarRengi.A > 0 && Enabled)
            {
                using var kalem = new Pen(KenarRengi);
                g.DrawPath(kalem, yol);
            }
        }

        TextRenderer.DrawText(g, Text, Font, alan, yazi,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
    }
}
