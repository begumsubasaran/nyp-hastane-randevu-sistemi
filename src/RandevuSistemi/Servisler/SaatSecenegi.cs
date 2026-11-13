namespace RandevuSistemi.Servisler;

/// <summary>
/// Randevu ekranında listelenen bir saat aralığı (ör. 09:20 - 09:40).
/// Musait = false ise neden uygun olmadığı "Neden" alanında yazar (ör. çakışma).
/// </summary>
public sealed record SaatSecenegi(DateTime Baslangic, DateTime Bitis, string? Neden)
{
    public bool Musait => Neden is null;

    public override string ToString() =>
        $"{Baslangic:HH:mm} - {Bitis:HH:mm}" + (Musait ? "" : "   (uygun değil)");
}
