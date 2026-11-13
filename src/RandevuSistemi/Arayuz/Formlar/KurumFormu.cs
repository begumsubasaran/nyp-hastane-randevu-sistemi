using RandevuSistemi.Models.Kurumlar;
using RandevuSistemi.Veri;

namespace RandevuSistemi.Arayuz.Formlar;

/// <summary>
/// Yeni hastane / klinik tanımlama penceresi (sadece Sistem Yöneticisi açabilir).
/// Her kurumun en az bir şubesi olacağı için ilk şube de burada birlikte oluşturulur.
/// </summary>
public class KurumFormu : DialogFormTemeli
{
    private readonly UygulamaVerisi _veri;
    private readonly TextBox _adKutusu = new();
    private readonly ComboBox _turKutusu = SecimKutusu(["Hastane", "Klinik", "Tıp Merkezi"]); // KurumTuru sırası
    private readonly TextBox _subeAdiKutusu = new() { Text = "Merkez" };
    private readonly TextBox _adresKutusu = new();

    public KurumFormu(UygulamaVerisi veri) : base("Yeni Hastane / Klinik")
    {
        _veri = veri;
        SatirEkle("Kurum adı", _adKutusu);
        SatirEkle("Kurum türü", _turKutusu);
        SatirEkle("İlk şube adı", _subeAdiKutusu);
        SatirEkle("Şube adresi", _adresKutusu);
    }

    public Kurum? OlusturulanKurum { get; private set; }

    protected override bool Kaydet()
    {
        var kurum = new Kurum(_adKutusu.Text, (KurumTuru)_turKutusu.SelectedIndex);
        kurum.SubeEkle(_subeAdiKutusu.Text, _adresKutusu.Text);
        _veri.KurumEkle(kurum); // aynı isimde kurum varsa hata verir

        OlusturulanKurum = kurum;
        return true;
    }
}
