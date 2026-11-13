using RandevuSistemi.Models.Kurumlar;

namespace RandevuSistemi.Arayuz.Formlar;

/// <summary>Var olan bir kuruma yeni şube ekleme penceresi (sadece Sistem Yöneticisi).</summary>
public class SubeFormu : DialogFormTemeli
{
    private readonly Kurum _kurum;
    private readonly TextBox _adKutusu = new();
    private readonly TextBox _adresKutusu = new();

    public SubeFormu(Kurum kurum) : base($"Yeni Şube - {kurum.Ad}")
    {
        _kurum = kurum;
        SatirEkle("Şube adı", _adKutusu);
        SatirEkle("Adres", _adresKutusu);
    }

    public Sube? OlusturulanSube { get; private set; }

    protected override bool Kaydet()
    {
        // Kurum.SubeEkle ad/adres boşsa veya aynı isimde şube varsa hata fırlatır.
        OlusturulanSube = _kurum.SubeEkle(_adKutusu.Text, _adresKutusu.Text);
        return true;
    }
}
