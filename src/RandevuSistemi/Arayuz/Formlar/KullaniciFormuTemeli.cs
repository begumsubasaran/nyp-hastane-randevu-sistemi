using RandevuSistemi.Models.Kullanicilar;
using RandevuSistemi.Veri;

namespace RandevuSistemi.Arayuz.Formlar;

/// <summary>
/// Yeni kullanıcı (hasta veya çalışan) kayıt pencerelerinin ORTAK SOYUT temel sınıfı.
/// Tüm kullanıcılarda ortak olan alanları (ad, soyad, TC, kullanıcı adı, şifre, telefon, e-posta)
/// burada oluşturur. Hangi tür kullanıcının oluşturulacağına alt sınıf karar verir:
/// abstract KullaniciOlustur() metodu (HastaKayitFormu → Hasta, CalisanFormu → Doktor / Sağlık Personeli).
///
/// Model tarafındaki "Kullanici → Hasta / Calisan" kalıtımının arayüzdeki karşılığıdır.
/// </summary>
public abstract class KullaniciFormuTemeli : DialogFormTemeli
{
    private readonly UygulamaVerisi _veri;

    // Alt sınıfların KullaniciOlustur() içinde kullanabilmesi için "protected"
    protected readonly TextBox AdKutusu = new();
    protected readonly TextBox SoyadKutusu = new();
    protected readonly TextBox TcKutusu = new() { MaxLength = 11 };
    protected readonly TextBox KullaniciAdiKutusu = new();
    protected readonly TextBox SifreKutusu = new() { UseSystemPasswordChar = true };
    private readonly TextBox _sifreTekrarKutusu = new() { UseSystemPasswordChar = true };
    private readonly TextBox _telefonKutusu = new();
    private readonly TextBox _epostaKutusu = new();

    protected KullaniciFormuTemeli(string baslik, UygulamaVerisi veri) : base(baslik)
    {
        _veri = veri;

        SatirEkle("Ad", AdKutusu);
        SatirEkle("Soyad", SoyadKutusu);
        SatirEkle("TC Kimlik No", TcKutusu);
        SatirEkle("Telefon", _telefonKutusu);
        SatirEkle("E-posta", _epostaKutusu);
        SatirEkle("Kullanıcı adı", KullaniciAdiKutusu);
        SatirEkle("Şifre (en az 6)", SifreKutusu);
        SatirEkle("Şifre (tekrar)", _sifreTekrarKutusu);
        // Alt sınıflar kendi constructor'larında bunların altına kendi alanlarını ekler.
    }

    /// <summary>Kaydedilen yeni kullanıcı (form OK ile kapandıysa dolu olur).</summary>
    public Kullanici? OlusturulanKullanici { get; private set; }

    /// <summary>Alt sınıf, formdaki bilgilerle doğru türde kullanıcı nesnesini oluşturur.</summary>
    protected abstract Kullanici KullaniciOlustur();

    protected override bool Kaydet()
    {
        if (SifreKutusu.Text != _sifreTekrarKutusu.Text)
            throw new ArgumentException("Girilen şifreler birbiriyle aynı değil.");

        // Doğrulamalar (TC 11 hane, şifre en az 6 karakter vb.) model sınıflarının içinde yapılır.
        Kullanici kullanici = KullaniciOlustur();
        kullanici.Telefon = _telefonKutusu.Text;
        kullanici.Eposta = _epostaKutusu.Text;

        // Kullanıcı adı ve TC'nin benzersiz olduğu burada kontrol edilir.
        _veri.KullaniciEkle(kullanici);
        OlusturulanKullanici = kullanici;
        return true;
    }
}
