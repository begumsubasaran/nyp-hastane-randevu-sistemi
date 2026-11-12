using System.Security.Cryptography;
using System.Text;

namespace RandevuSistemi.Models.Kullanicilar;

/// <summary>
/// Tüm kullanıcıların ortak özelliklerini tutan SOYUT (abstract) temel sınıf.
///
/// - SOYUTLAMA: "new Kullanici(...)" yazılamaz; sadece Hasta, Doktor gibi
///   somut alt sınıflardan nesne üretilebilir.
/// - KAPSÜLLEME: Alanlar private tutulur, dışarıya doğrulama yapan
///   property'ler ve metotlar açılır. Şifre hiçbir zaman düz metin saklanmaz.
/// - KALITIM: Hasta, Calisan (Doktor, SaglikPersoneli) ve Yonetici bu sınıftan türer.
/// </summary>
public abstract class Kullanici
{
    // ---- Private alanlar (dışarıdan doğrudan erişilemez) ----
    private string _ad = string.Empty;
    private string _soyad = string.Empty;
    private string _tcKimlikNo = string.Empty;
    private string _kullaniciAdi = string.Empty;
    private string _sifreHash = string.Empty;
    private string? _telefon;
    private string? _eposta;

    protected Kullanici(string ad, string soyad, string tcKimlikNo, string kullaniciAdi, string sifre)
    {
        // Atamalar property'ler üzerinden yapılır, böylece doğrulama kuralları
        // nesne ilk oluşturulurken de çalışır.
        Ad = ad;
        Soyad = soyad;
        TcKimlikNo = tcKimlikNo;
        KullaniciAdi = kullaniciAdi;
        SifreAta(sifre);
    }

    /// <summary>
    /// Her kullanıcıya otomatik verilen benzersiz kimlik. "internal set": sadece bu proje içinden
    /// (JSON'dan yüklerken aynı kimliği geri vermek için) atanabilir.
    /// </summary>
    public Guid Id { get; internal set; } = Guid.NewGuid();

    public string Ad
    {
        get => _ad;
        set => _ad = BosOlamaz(value, "Ad");
    }

    public string Soyad
    {
        get => _soyad;
        set => _soyad = BosOlamaz(value, "Soyad");
    }

    public string TcKimlikNo
    {
        get => _tcKimlikNo;
        set
        {
            string tc = BosOlamaz(value, "TC Kimlik No");
            if (tc.Length != 11 || !tc.All(char.IsDigit) || tc[0] == '0')
                throw new ArgumentException("TC Kimlik No 11 haneli olmalı ve 0 ile başlamamalı.");
            _tcKimlikNo = tc;
        }
    }

    public string KullaniciAdi
    {
        get => _kullaniciAdi;
        set
        {
            string ad = BosOlamaz(value, "Kullanıcı adı");
            if (ad.Length < 3 || ad.Contains(' '))
                throw new ArgumentException("Kullanıcı adı en az 3 karakter olmalı ve boşluk içermemeli.");
            _kullaniciAdi = ad.ToLowerInvariant();
        }
    }

    /// <summary>İsteğe bağlı. Boşluk, tire gibi karakterler temizlenip sadece rakamlar saklanır.</summary>
    public string? Telefon
    {
        get => _telefon;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _telefon = null;
                return;
            }
            string rakamlar = new string(value.Where(char.IsDigit).ToArray());
            if (rakamlar.Length is not (10 or 11))
                throw new ArgumentException("Telefon numarası 10 veya 11 haneli olmalı.");
            _telefon = rakamlar;
        }
    }

    /// <summary>İsteğe bağlı. Basit bir biçim kontrolü yapılır.</summary>
    public string? Eposta
    {
        get => _eposta;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _eposta = null;
                return;
            }
            string eposta = value.Trim();
            int atIndex = eposta.IndexOf('@');
            if (atIndex < 1 || !eposta[atIndex..].Contains('.'))
                throw new ArgumentException("Geçerli bir e-posta adresi girin.");
            _eposta = eposta;
        }
    }

    /// <summary>Ad ve soyadı birleştiren, sadece okunabilir (hesaplanan) property.</summary>
    public string TamAd => $"{Ad} {Soyad}";

    /// <summary>
    /// Her alt sınıf kendi rolünü bildirmek ZORUNDADIR (abstract).
    /// </summary>
    public abstract Rol Rol { get; }

    // ---- Şifre işlemleri ----

    /// <summary>Girilen şifrenin doğru olup olmadığını kontrol eder.</summary>
    public bool SifreDogruMu(string sifre) => _sifreHash == Hashle(sifre);

    /// <summary>Şifre sadece mevcut şifre bilinerek değiştirilebilir.</summary>
    public void SifreDegistir(string eskiSifre, string yeniSifre)
    {
        if (!SifreDogruMu(eskiSifre))
            throw new InvalidOperationException("Mevcut şifre hatalı.");
        SifreAta(yeniSifre);
    }

    /// <summary>
    /// Şifrenin özeti (hash). Sadece veri dosyasına kaydetmek ve geri yüklemek için kullanılır;
    /// "internal" olduğu için proje dışından erişilemez. Şifrenin kendisi hiçbir yerde saklanmaz.
    /// </summary>
    internal string SifreOzeti
    {
        get => _sifreHash;
        set => _sifreHash = value;
    }

    private void SifreAta(string sifre)
    {
        if (string.IsNullOrEmpty(sifre) || sifre.Length < 6)
            throw new ArgumentException("Şifre en az 6 karakter olmalı.");
        _sifreHash = Hashle(sifre);
    }

    /// <summary>
    /// Şifreyi SHA-256 ile özetler (hash). Not: Gerçek sistemlerde "salt" eklenmiş
    /// daha yavaş algoritmalar (PBKDF2, bcrypt) tercih edilir; ders projesi için yeterli.
    /// </summary>
    private static string Hashle(string metin) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(metin)));

    // ---- Çok biçimlilik (polymorphism) ----

    /// <summary>
    /// Kullanıcının kısa tanıtımı. "virtual" olduğu için alt sınıflar
    /// override ederek kendi bilgilerini ekleyebilir.
    /// </summary>
    public virtual string BilgiOzeti() => $"[{this.Rol.GorunenAd()}] {TamAd} (kullanıcı adı: {KullaniciAdi})";

    public override string ToString() => BilgiOzeti();

    // ---- Alt sınıfların da kullanabileceği yardımcı metot ----

    protected static string BosOlamaz(string? deger, string alanAdi) => Dogrulama.BosOlamaz(deger, alanAdi);
}
