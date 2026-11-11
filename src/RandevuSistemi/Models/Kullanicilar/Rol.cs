namespace RandevuSistemi.Models.Kullanicilar;

/// <summary>
/// Sistemdeki kullanıcı rolleri.
/// Her kullanıcı sınıfı kendi rolünü (override ederek) bildirir.
/// </summary>
public enum Rol
{
    Hasta,
    Doktor,
    SaglikPersoneli,
    Yonetici
}

/// <summary>
/// Rol değerlerini ekranda okunabilir şekilde göstermek için yardımcı metotlar.
/// "this Rol rol" yazımı bir extension method'dur: rol.GorunenAd() şeklinde çağrılır.
/// </summary>
public static class RolUzantilari
{
    public static string GorunenAd(this Rol rol) => rol switch
    {
        Rol.Hasta => "Hasta",
        Rol.Doktor => "Doktor",
        Rol.SaglikPersoneli => "Sağlık Personeli",
        Rol.Yonetici => "Yönetici",
        _ => rol.ToString()
    };
}
