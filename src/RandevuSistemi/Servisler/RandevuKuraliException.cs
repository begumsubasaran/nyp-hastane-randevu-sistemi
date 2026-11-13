namespace RandevuSistemi.Servisler;

/// <summary>
/// Randevu kurallarından biri ihlal edildiğinde fırlatılan ÖZEL hata türü
/// (çakışma, çalışanın uygun olmadığı saat, geçmiş tarih vb.).
///
/// .NET'in InvalidOperationException sınıfından TÜRER; böylece onu yakalayan her kod
/// bu hatayı da yakalar, ama istenirse sadece randevu hataları ayrıca yakalanabilir.
/// </summary>
public class RandevuKuraliException : InvalidOperationException
{
    public RandevuKuraliException(string mesaj) : base(mesaj)
    {
    }
}
