using System.Drawing.Drawing2D;
using System.Globalization;
using RandevuSistemi.Models.Randevular;

namespace RandevuSistemi.Arayuz.Kontroller;

/// <summary>
/// Ana sayfadaki "Yaklaşan Randevular" listesinin bir satırı:
/// [16 Kas]  Kardiyoloji Muayenesi                      (Onaylandı)
///           09:00 - 09:20 · Mehmet Kaya (Uzm. Dr.)
/// Tamamen OnPaint ile çizilir.
/// </summary>
public class RandevuOzetSatiri : Control
{
    private static readonly CultureInfo Turkce = new("tr-TR");
    private static readonly Font GunYazisi = new("Segoe UI Semibold", 15f);
    private static readonly Font AyYazisi = new("Segoe UI", 8.5f);

    private readonly Randevu _randevu;
    private readonly bool _calisanGorunumu;

    /// <param name="calisanGorunumu">true ise alt satırda çalışan yerine hasta adı yazar (doktor ekranı için).</param>
    public RandevuOzetSatiri(Randevu randevu, bool calisanGorunumu)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        _randevu = randevu;
        _calisanGorunumu = calisanGorunumu;
        Height = 66;
        Dock = DockStyle.Top;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Tema.Kart);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Sol: tarih kutusu (gün + ay)
        var tarihKutusu = new Rectangle(0, 9, 52, 48);
        using (GraphicsPath yol = Tema.YuvarlakYol(tarihKutusu, 10))
        using (var firca = new SolidBrush(Tema.VurguAcik))
            g.FillPath(firca, yol);
        TextRenderer.DrawText(g, _randevu.Baslangic.ToString("dd"), GunYazisi,
            new Rectangle(tarihKutusu.X, tarihKutusu.Y + 2, tarihKutusu.Width, 28), Tema.Vurgu,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        TextRenderer.DrawText(g, _randevu.Baslangic.ToString("MMM", Turkce), AyYazisi,
            new Rectangle(tarihKutusu.X, tarihKutusu.Y + 28, tarihKutusu.Width, 16), Tema.Vurgu,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        // Sağ: durum hapı
        string durumMetni = _randevu.Durum.GorunenAd();
        int hapGenisligi = TextRenderer.MeasureText(durumMetni, Tema.Kucuk).Width + 18;
        Tema.DurumHapiCiz(g, new Rectangle(Width - hapGenisligi - 4, 0, hapGenisligi, Height),
            durumMetni, Tema.DurumRengi(_randevu.Durum));

        // Orta: işlem adı ve ayrıntı
        int x = tarihKutusu.Right + 14;
        int genislik = Width - x - hapGenisligi - 16;
        string kim = _calisanGorunumu ? _randevu.HastaAdi : _randevu.CalisanAdi;
        TextRenderer.DrawText(g, _randevu.HizmetAdi, Tema.Kalin, new Rectangle(x, 12, genislik, 22), Tema.Yazi,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(g, $"{_randevu.Baslangic:HH:mm} - {_randevu.Bitis:HH:mm}  ·  {kim}", Tema.Kucuk,
            new Rectangle(x, 34, genislik, 20), Tema.SolukYazi,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        // Alt çizgi
        using var kalem = new Pen(Tema.Cizgi);
        g.DrawLine(kalem, x, Height - 1, Width, Height - 1);
    }
}
