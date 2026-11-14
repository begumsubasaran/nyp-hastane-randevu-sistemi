using System.Drawing.Drawing2D;

namespace RandevuSistemi.Arayuz.Kontroller;

/// <summary>
/// Yan menüdeki gezinme butonu: solda simge, yanında yazı.
/// Seçiliyken açık turkuaz zemin ve turkuaz yazıyla vurgulanır.
/// Button sınıfından türer ve kendini OnPaint ile çizer.
/// </summary>
public class MenuButonu : Button
{
    private static readonly Font KalinYazi = new("Segoe UI Semibold", 10.5f);
    private bool _secili;
    private bool _fareUzerinde;

    public MenuButonu(string ikon, string metin)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Ikon = ikon;
        Text = metin;
        Width = 196;
        Height = 44;
        Margin = new Padding(12, 2, 12, 2);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        Font = new Font("Segoe UI", 10.5f);
    }

    /// <summary>Segoe MDL2 Assets yazı tipindeki simge karakteri (Tema.Ikonlar).</summary>
    public string Ikon { get; set; }

    /// <summary>Şu an açık olan ekranın butonu mu?</summary>
    public bool Secili
    {
        get => _secili;
        set
        {
            _secili = value;
            Invalidate(); // yeniden çiz
        }
    }

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
        Graphics g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Tema.Kart);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var alan = new Rectangle(0, 0, Width - 1, Height - 1);
        Color zemin = Secili ? Tema.VurguAcik : _fareUzerinde ? Tema.Arkaplan : Color.Empty;
        if (zemin != Color.Empty)
        {
            using GraphicsPath yol = Tema.YuvarlakYol(alan, 10);
            using var firca = new SolidBrush(zemin);
            g.FillPath(firca, yol);
        }

        Color renk = Secili ? Tema.Vurgu : Tema.Yazi;
        var ikonAlani = new Rectangle(12, 0, 28, Height);
        var yaziAlani = new Rectangle(46, 0, Width - 50, Height);

        TextRenderer.DrawText(g, Ikon, Tema.Ikon, ikonAlani, renk,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        TextRenderer.DrawText(g, Text, Secili ? KalinYazi : Font, yaziAlani, renk,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}
