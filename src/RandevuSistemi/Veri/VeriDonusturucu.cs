using RandevuSistemi.Models.Kullanicilar;
using RandevuSistemi.Models.Kurumlar;
using RandevuSistemi.Models.Randevular;
using RandevuSistemi.Veri.Kayitlar;

namespace RandevuSistemi.Veri;

/// <summary>
/// Model nesneleri ↔ kayıt (DTO) nesneleri arasında dönüşüm yapar.
///
/// - KaydaDonustur: UygulamaVerisi → VeriKaydi (diske yazmadan önce)
/// - VeriyeDonustur: VeriKaydi → UygulamaVerisi (diskten okuduktan sonra)
///
/// Geri dönüşümde nesneler modelin kendi metotlarıyla (SubeEkle, HizmetEkle, UygunlukEkle...)
/// kurulur; yani dosyadan gelen veri de aynı kurallardan geçer.
/// </summary>
public static class VeriDonusturucu
{
    // =========================================================================
    // Model → Kayıt
    // =========================================================================

    public static VeriKaydi KaydaDonustur(UygulamaVerisi veri) => new()
    {
        KayitZamani = DateTime.Now,
        Kurumlar = veri.Kurumlar.Select(KurumKaydiOlustur).ToList(),
        Kullanicilar = veri.Kullanicilar.Select(KullaniciKaydiOlustur).ToList(),
        Randevular = veri.Randevular.Select(RandevuKaydiOlustur).ToList()
    };

    private static KurumKaydi KurumKaydiOlustur(Kurum kurum) => new()
    {
        Id = kurum.Id,
        Ad = kurum.Ad,
        Tur = kurum.Tur,
        Subeler = kurum.Subeler.Select(sube => new SubeKaydi
        {
            Id = sube.Id,
            Ad = sube.Ad,
            Adres = sube.Adres,
            CalismaSaatleri = sube.CalismaSaatleri
                .Select(s => new CalismaSaatiKaydi { Gun = s.Gun, Acilis = s.Acilis, Kapanis = s.Kapanis })
                .ToList(),
            Bolumler = sube.Bolumler
                .Select(b => new BolumKaydi { Id = b.Id, Ad = b.Ad, Aciklama = b.Aciklama })
                .ToList(),
            Hizmetler = sube.Hizmetler
                .Select(h => new HizmetKaydi
                {
                    Id = h.Id,
                    Ad = h.Ad,
                    Kategori = h.Kategori,
                    SureDakika = h.SureDakika,
                    Ucret = h.Ucret,
                    Aciklama = h.Aciklama
                })
                .ToList()
        }).ToList()
    };

    private static KullaniciKaydi KullaniciKaydiOlustur(Kullanici kullanici)
    {
        var kayit = new KullaniciKaydi
        {
            Rol = kullanici.Rol,
            Id = kullanici.Id,
            Ad = kullanici.Ad,
            Soyad = kullanici.Soyad,
            TcKimlikNo = kullanici.TcKimlikNo,
            KullaniciAdi = kullanici.KullaniciAdi,
            SifreOzeti = kullanici.SifreOzeti,
            Telefon = kullanici.Telefon,
            Eposta = kullanici.Eposta
        };

        // Nesnenin gerçek türüne göre türe özel alanları dolduruyoruz (tür kontrolü + pattern matching).
        switch (kullanici)
        {
            case Hasta hasta:
                kayit.DogumTarihi = hasta.DogumTarihi;
                break;
            case Doktor doktor:
                CalisanAlanlariniDoldur(kayit, doktor);
                kayit.UzmanlikAlani = doktor.UzmanlikAlani;
                kayit.AkademikUnvan = doktor.AkademikUnvan;
                break;
            case SaglikPersoneli personel:
                CalisanAlanlariniDoldur(kayit, personel);
                kayit.Gorev = personel.Gorev;
                break;
            case Yonetici yonetici:
                kayit.Yetki = yonetici.Yetki;
                break;
        }

        return kayit;
    }

    private static void CalisanAlanlariniDoldur(KullaniciKaydi kayit, Calisan calisan)
    {
        kayit.AktifMi = calisan.AktifMi;
        kayit.SubeId = calisan.SubeId;
        kayit.HizmetIdleri = calisan.HizmetIdleri.ToList();
        kayit.UygunlukDilimleri = calisan.UygunlukDilimleri
            .Select(d => new UygunlukKaydi { Gun = d.Gun, Baslangic = d.Baslangic, Bitis = d.Bitis })
            .ToList();
    }

    private static RandevuKaydi RandevuKaydiOlustur(Randevu r) => new()
    {
        Id = r.Id,
        HastaId = r.HastaId,
        CalisanId = r.CalisanId,
        SubeId = r.SubeId,
        HizmetId = r.HizmetId,
        HastaAdi = r.HastaAdi,
        CalisanAdi = r.CalisanAdi,
        HizmetAdi = r.HizmetAdi,
        SureDakika = r.SureDakika,
        Ucret = r.Ucret,
        Baslangic = r.Baslangic,
        OlusturmaZamani = r.OlusturmaZamani,
        Durum = r.Durum,
        Not = r.Not
    };

    // =========================================================================
    // Kayıt → Model
    // =========================================================================

    /// <summary>
    /// Kayıttan uygulama verisini kurar. Sıra önemlidir:
    /// önce kurumlar (şubeler ve hizmetler), sonra kullanıcılar (çalışanlar şubelere bağlanır),
    /// en son randevular.
    /// </summary>
    public static UygulamaVerisi VeriyeDonustur(VeriKaydi kayit)
    {
        var veri = new UygulamaVerisi();

        foreach (KurumKaydi kurumKaydi in kayit.Kurumlar)
            veri.Kurumlar.Add(KurumOlustur(kurumKaydi));

        foreach (KullaniciKaydi kullaniciKaydi in kayit.Kullanicilar)
            veri.Kullanicilar.Add(KullaniciOlustur(kullaniciKaydi, veri));

        foreach (RandevuKaydi r in kayit.Randevular)
        {
            veri.Randevular.Add(Randevu.KayittanYukle(
                r.Id, r.HastaId, r.CalisanId, r.SubeId, r.HizmetId,
                r.HastaAdi, r.CalisanAdi, r.HizmetAdi, r.SureDakika, r.Ucret,
                r.Baslangic, r.OlusturmaZamani, r.Durum, r.Not));
        }

        return veri;
    }

    private static Kurum KurumOlustur(KurumKaydi kayit)
    {
        // Id, şubeler eklenmeden ÖNCE atanmalı; çünkü şube oluşturulurken kurumun Id'sini alır.
        var kurum = new Kurum(kayit.Ad, kayit.Tur) { Id = kayit.Id };

        foreach (SubeKaydi subeKaydi in kayit.Subeler)
        {
            Sube sube = kurum.SubeEkle(subeKaydi.Ad, subeKaydi.Adres);
            sube.Id = subeKaydi.Id;

            foreach (CalismaSaatiKaydi saat in subeKaydi.CalismaSaatleri)
                sube.CalismaSaatiBelirle(saat.Gun, saat.Acilis, saat.Kapanis);

            foreach (BolumKaydi bolum in subeKaydi.Bolumler)
                sube.BolumEkle(bolum.Ad, bolum.Aciklama).Id = bolum.Id;

            foreach (HizmetKaydi h in subeKaydi.Hizmetler)
                sube.HizmetEkle(h.Ad, h.Kategori, h.SureDakika, h.Ucret, h.Aciklama).Id = h.Id;
        }

        return kurum;
    }

    /// <summary>
    /// Kullanıcı constructor'ları şifre ister; ama dosyada şifrenin kendisi değil, sadece özeti var.
    /// Bu yüzden geçici bir şifreyle oluşturup hemen ardından kayıttaki özeti yerine koyuyoruz.
    /// </summary>
    private const string GeciciSifre = "gecici-sifre";

    private static Kullanici KullaniciOlustur(KullaniciKaydi k, UygulamaVerisi veri)
    {
        Kullanici kullanici = k.Rol switch
        {
            Rol.Hasta => new Hasta(k.Ad, k.Soyad, k.TcKimlikNo, k.KullaniciAdi, GeciciSifre,
                k.DogumTarihi ?? throw new InvalidDataException($"{k.Ad} {k.Soyad} için doğum tarihi eksik.")),

            Rol.Doktor => new Doktor(k.Ad, k.Soyad, k.TcKimlikNo, k.KullaniciAdi, GeciciSifre,
                k.UzmanlikAlani ?? throw new InvalidDataException($"{k.Ad} {k.Soyad} için uzmanlık alanı eksik."),
                k.AkademikUnvan ?? DoktorUnvani.Dr),

            Rol.SaglikPersoneli => new SaglikPersoneli(k.Ad, k.Soyad, k.TcKimlikNo, k.KullaniciAdi, GeciciSifre,
                k.Gorev ?? PersonelGorevi.Hemsire),

            Rol.Yonetici => new Yonetici(k.Ad, k.Soyad, k.TcKimlikNo, k.KullaniciAdi, GeciciSifre,
                k.Yetki ?? YoneticiYetkisi.KurumYoneticisi),

            _ => throw new InvalidDataException($"Bilinmeyen kullanıcı rolü: {k.Rol}")
        };

        kullanici.Id = k.Id;
        kullanici.SifreOzeti = k.SifreOzeti;
        kullanici.Telefon = k.Telefon;
        kullanici.Eposta = k.Eposta;

        if (kullanici is Calisan calisan)
            CalisanBilgileriniYukle(calisan, k, veri);

        return kullanici;
    }

    private static void CalisanBilgileriniYukle(Calisan calisan, KullaniciKaydi k, UygulamaVerisi veri)
    {
        if (!k.AktifMi)
            calisan.PasifeAl();

        Sube? sube = veri.SubeBul(k.SubeId);
        if (sube is null)
            return; // şubeye atanmamış çalışan

        calisan.SubeyeAta(sube);

        foreach (Guid hizmetId in k.HizmetIdleri)
        {
            var hizmet = sube.HizmetBul(hizmetId);
            if (hizmet is not null)
                calisan.HizmetEkle(hizmet);
        }

        foreach (UygunlukKaydi d in k.UygunlukDilimleri)
        {
            try
            {
                calisan.UygunlukEkle(sube, d.Gun, d.Baslangic, d.Bitis);
            }
            catch (Exception hata) when (hata is ArgumentException or InvalidOperationException)
            {
                // Kurallara artık uymayan bir dilim (ör. şube saatleri değişmiş) varsa atlanır.
            }
        }
    }
}
