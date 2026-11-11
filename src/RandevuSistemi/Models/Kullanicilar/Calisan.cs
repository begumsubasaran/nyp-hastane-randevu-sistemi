namespace RandevuSistemi.Models.Kullanicilar;

/// <summary>
/// Kurumda çalışan kişilerin (Doktor, Sağlık Personeli) ortak SOYUT sınıfı.
/// Çok seviyeli kalıtım örneği: Kullanici → Calisan → Doktor
/// </summary>
public abstract class Calisan : Kullanici
{
    protected Calisan(string ad, string soyad, string tcKimlikNo, string kullaniciAdi, string sifre)
        : base(ad, soyad, tcKimlikNo, kullaniciAdi, sifre)
    {
    }

    /// <summary>İşten ayrılan çalışan silinmez, pasife alınır (geçmiş randevular korunur).</summary>
    public bool AktifMi { get; private set; } = true;

    public void PasifeAl() => AktifMi = false;
    public void AktifEt() => AktifMi = true;

    /// <summary>
    /// Her çalışan türü unvanını kendisi belirler.
    /// Doktor: "Uzm. Dr. (Kardiyoloji)", Sağlık Personeli: "Hemşire" gibi.
    /// </summary>
    public abstract string Unvan { get; }

    public override string BilgiOzeti() =>
        $"{base.BilgiOzeti()} - {Unvan}{(AktifMi ? "" : " (pasif)")}";
}
