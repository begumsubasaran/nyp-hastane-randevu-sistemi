namespace RandevuSistemi.Models.Kullanicilar;

/// <summary>Doktorların akademik unvanları.</summary>
public enum DoktorUnvani
{
    Dr,
    UzmDr,
    DocDr,
    ProfDr
}

/// <summary>
/// Doktor: Calisan sınıfından türer.
/// Uzmanlık alanı şimdilik metin olarak tutuluyor; sunduğu hizmetler
/// ve uygunluk saatleri sonraki adımlarda eklenecek.
/// </summary>
public class Doktor : Calisan
{
    private string _uzmanlikAlani = string.Empty;

    public Doktor(string ad, string soyad, string tcKimlikNo, string kullaniciAdi, string sifre,
                  string uzmanlikAlani, DoktorUnvani akademikUnvan)
        : base(ad, soyad, tcKimlikNo, kullaniciAdi, sifre)
    {
        UzmanlikAlani = uzmanlikAlani;
        AkademikUnvan = akademikUnvan;
    }

    public override Rol Rol => Rol.Doktor;

    public string UzmanlikAlani
    {
        get => _uzmanlikAlani;
        set => _uzmanlikAlani = BosOlamaz(value, "Uzmanlık alanı");
    }

    public DoktorUnvani AkademikUnvan { get; set; }

    public override string Unvan
    {
        get
        {
            string unvan = AkademikUnvan switch
            {
                DoktorUnvani.Dr => "Dr.",
                DoktorUnvani.UzmDr => "Uzm. Dr.",
                DoktorUnvani.DocDr => "Doç. Dr.",
                DoktorUnvani.ProfDr => "Prof. Dr.",
                _ => "Dr."
            };
            return $"{unvan} ({UzmanlikAlani})";
        }
    }
}
