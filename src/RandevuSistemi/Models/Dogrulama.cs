namespace RandevuSistemi.Models;

/// <summary>
/// Birden fazla sınıfın kullandığı ortak doğrulama metotları.
/// "static" sınıf: nesnesi oluşturulmaz, metotları Dogrulama.BosOlamaz(...) şeklinde çağrılır.
/// </summary>
public static class Dogrulama
{
    /// <summary>Metin boş ya da sadece boşluksa hata fırlatır, değilse kırpılmış halini döndürür.</summary>
    public static string BosOlamaz(string? deger, string alanAdi)
    {
        if (string.IsNullOrWhiteSpace(deger))
            throw new ArgumentException($"{alanAdi} boş olamaz.");
        return deger.Trim();
    }
}
