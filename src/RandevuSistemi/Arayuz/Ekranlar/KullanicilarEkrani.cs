using RandevuSistemi.Models.Kullanicilar;

namespace RandevuSistemi.Arayuz.Ekranlar;

/// <summary>
/// Sistemdeki kullanıcıları tablo halinde listeler; üstteki kutudan role göre filtrelenebilir.
/// </summary>
public class KullanicilarEkrani : UserControl
{
    private readonly IReadOnlyList<Kullanici> _kullanicilar;
    private readonly DataGridView _tablo;
    private readonly ComboBox _rolFiltresi;
    private readonly Label _sayac;

    public KullanicilarEkrani(IReadOnlyList<Kullanici> kullanicilar)
    {
        _kullanicilar = kullanicilar;
        BackColor = Tema.Arkaplan;

        var yerlesim = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        yerlesim.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        yerlesim.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // ---- Üst çubuk: rol filtresi ve kayıt sayısı ----
        var ustCubuk = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        var filtreEtiketi = new Label
        {
            Text = "Rol:",
            AutoSize = true,
            Margin = new Padding(0, 8, 6, 0),
            ForeColor = Tema.SolukYazi
        };
        _rolFiltresi = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 200,
            Margin = new Padding(0, 4, 16, 0)
        };
        _rolFiltresi.Items.Add("Tümü");
        foreach (Rol rol in Enum.GetValues<Rol>())
            _rolFiltresi.Items.Add(rol.GorunenAd());

        _sayac = new Label { AutoSize = true, Margin = new Padding(0, 8, 0, 0), ForeColor = Tema.SolukYazi };

        ustCubuk.Controls.Add(filtreEtiketi);
        ustCubuk.Controls.Add(_rolFiltresi);
        ustCubuk.Controls.Add(_sayac);
        yerlesim.Controls.Add(ustCubuk, 0, 0);

        // ---- Tablo ----
        _tablo = new DataGridView();
        Tema.TabloyuBicimlendir(_tablo);
        _tablo.Columns.Add("rol", "Rol");
        _tablo.Columns.Add("adSoyad", "Ad Soyad");
        _tablo.Columns.Add("kullaniciAdi", "Kullanıcı Adı");
        _tablo.Columns.Add("ozet", "Bilgi");
        _tablo.Columns["rol"]!.FillWeight = 18;
        _tablo.Columns["adSoyad"]!.FillWeight = 22;
        _tablo.Columns["kullaniciAdi"]!.FillWeight = 18;
        _tablo.Columns["ozet"]!.FillWeight = 42;
        yerlesim.Controls.Add(_tablo, 0, 1);

        Controls.Add(yerlesim);

        // SelectedIndex değişince tablo yeniden doldurulur (ilk doldurma da burada olur).
        _rolFiltresi.SelectedIndexChanged += (_, _) => TabloyuDoldur();
        _rolFiltresi.SelectedIndex = 0;
    }

    private void TabloyuDoldur()
    {
        // İlk seçenek "Tümü", sonrakiler Rol enum'unun sırasıyla aynı.
        Rol? secilenRol = _rolFiltresi.SelectedIndex <= 0 ? null : (Rol)(_rolFiltresi.SelectedIndex - 1);

        IEnumerable<Kullanici> gosterilecekler = secilenRol is null
            ? _kullanicilar
            : _kullanicilar.Where(k => k.Rol == secilenRol);

        _tablo.Rows.Clear();
        foreach (Kullanici k in gosterilecekler)
        {
            // ÇOK BİÇİMLİLİK: k.Rol ve k.BilgiOzeti() her kullanıcı türünde farklı sonuç verir.
            _tablo.Rows.Add(k.Rol.GorunenAd(), k.TamAd, k.KullaniciAdi, k.BilgiOzeti());
        }

        _sayac.Text = $"{_tablo.Rows.Count} kullanıcı";
    }
}
