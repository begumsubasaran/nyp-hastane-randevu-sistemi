# Hastane / Klinik Randevu Sistemi

Nesneye Dayalı Programlama dersi dönem projesi (Seçenek A).

C# (.NET 9) ve **Windows Forms** ile yazılmış, masaüstü bir **hastane/klinik randevu sistemi**.
Tek bir kliniği ve çok şubeli sağlık kuruluşlarını destekler. Veriler JSON dosyalarında saklanır.

## Özellikler (hedef)

- **Kurum tanımları:** hastane/klinik, şubeler, bölümler, çalışma saatleri
- **Sağlık hizmetleri:** her hizmetin süresi ve ücreti (genel muayene, radyoloji, laboratuvar vb.)
- **Çalışan yönetimi:** doktor ve sağlık personeli kaydı, uzmanlık alanı, sunulan hizmetler
- **Uygunluk zaman dilimleri:** hastalar sadece doktorun uygun olduğu saatlere randevu alabilir
- **Randevu sistemi:** çakışma kontrolü, randevu detaylarının kaydı, onay mekanizması
- **Kullanıcı rolleri:** Hasta, Doktor / Sağlık Personeli, Hastane / Klinik Yöneticisi

## Kullanılan OOP ilkeleri

| İlke | Projede nerede? |
|---|---|
| Kapsülleme (Encapsulation) | Alanlar `private`, erişim `property` ve doğrulamalı metotlarla |
| Kalıtım (Inheritance) | `Kullanici` soyut sınıfından `Hasta`, `Doktor`, `SaglikPersoneli`, `Yonetici` türer |
| Çok biçimlilik (Polymorphism) | Her rol kendi menüsünü ve yetkilerini `override` ile tanımlar |
| Soyutlama (Abstraction) | `abstract` sınıflar (`Kullanici`, `Calisan`) ve `IVeriDeposu` arayüzü |

## Çalıştırma

Gerekli: Windows ve [.NET 9 SDK](https://dotnet.microsoft.com/download)

```bash
dotnet run --project src/RandevuSistemi
```

Visual Studio 2022 ile: `src/RandevuSistemi/RandevuSistemi.csproj` dosyasını açıp **F5** ile çalıştırın.

## Giriş ve roller

Program giriş ekranıyla açılır. Örnek hesaplar (hepsinin şifresi `sifre123`):

| Kullanıcı adı | Rol | Görebildiği ekranlar |
|---|---|---|
| `ayse`, `can` | Hasta | Randevularım (kendi randevuları, yeni randevu alma, iptal) |
| `drmehmet`, `drelif`, `drhakan` | Doktor | Randevularım (kendisine alınanlar, onay/ret/tamamlama), Çalışma Bilgilerim (uygunluk saatleri) |
| `zeynep`, `burak` | Sağlık Personeli | Doktor ile aynı |
| `admin` | Yönetici | Tüm ekranlar ve tüm randevular |

"Çıkış Yap" ile giriş ekranına dönülür.

## Ekleme pencereleri (arayüzde kalıtım)

```
Form (.NET)
└── DialogFormTemeli (abstract)      → ortak düzen, Kaydet/İptal, hata gösterimi ("Template Method")
    ├── KurumFormu                   → yeni hastane/klinik + ilk şubesi (sadece Sistem Yöneticisi)
    ├── SubeFormu                    → kuruma yeni şube (sadece Sistem Yöneticisi)
    └── KullaniciFormuTemeli (abstract) → ad, soyad, TC, kullanıcı adı, şifre, telefon, e-posta
        ├── HastaKayitFormu          → + doğum tarihi (giriş ekranından "kayıt ol")
        └── CalisanFormu             → + tür, uzmanlık/unvan veya görev, şube (yönetici)
```

## Veri dosyası

Veriler program kapatılırken ve ekran değiştirilirken otomatik olarak JSON dosyasına kaydedilir:

```
src/RandevuSistemi/bin/Debug/net9.0-windows/veri/randevu-sistemi.json
```

İlk açılışta (dosya yokken) örnek verilerle başlanır. Örnek verilere dönmek için bu dosyayı silmek yeterlidir.

## Proje klasör yapısı

```
nesne_programlama/
├── RandevuSistemi.slnx          # Çözüm dosyası
├── README.md
└── src/
    └── RandevuSistemi/
        ├── RandevuSistemi.csproj
        ├── Program.cs           # Giriş noktası, ana pencereyi açar
        ├── Arayuz/              # Windows Forms arayüzü
        │   ├── AnaForm.cs       # Ana pencere: yan menü + içerik alanı
        │   ├── Tema.cs          # Renkler, yazı tipleri, ortak görünüm ayarları
        │   ├── Ekranlar/        # Menüden açılan ekranlar (UserControl)
        │   └── Formlar/         # Pencereler: giriş, randevu, ekleme formları (DialogFormTemeli'nden türeyen)
        ├── Servisler/
        │   ├── RandevuServisi.cs     # Randevu kuralları: uygunluk, çakışma, şube saatleri
        │   ├── RandevuKuraliException.cs # Kural ihlalinde fırlatılan özel hata türü
        │   ├── OturumServisi.cs      # Kullanıcı adı / şifre ile giriş
        │   └── SaatSecenegi.cs       # Randevu ekranındaki saat seçenekleri
        ├── Veri/
        │   ├── UygulamaVerisi.cs   # Tüm verileri (kurumlar, kullanıcılar, randevular) bir arada tutar
        │   ├── IVeriDeposu.cs      # Kaydetme/yükleme arayüzü (soyutlama)
        │   ├── JsonVeriDeposu.cs   # IVeriDeposu'nun JSON dosyası ile uygulaması
        │   ├── VeriDonusturucu.cs  # Model ↔ kayıt (DTO) dönüşümü
        │   ├── Kayitlar/           # JSON'a yazılan sade kayıt sınıfları (DTO)
        │   └── OrnekVeriler.cs     # İlk açılış için örnek veriler
        └── Models/              # İş mantığı sınıfları (arayüzden bağımsız)
            ├── Dogrulama.cs
            ├── Kullanicilar/    # Kullanıcı sınıf hiyerarşisi
            ├── Kurumlar/        # Kurum, şube, bölüm, çalışma saatleri
            ├── Hizmetler/       # Sağlık hizmetleri (süre, ücret)
            └── Randevular/      # Uygunluk dilimleri, randevu ve randevu durumları
```

Model sınıfları arayüzden tamamen bağımsızdır: aynı sınıflar konsol, masaüstü
veya web arayüzüyle değiştirilmeden kullanılabilir.

## Kullanıcı sınıf hiyerarşisi

```
Kullanici (abstract)
├── Hasta
├── Calisan (abstract)
│   ├── Doktor
│   └── SaglikPersoneli
└── Yonetici
```

## Kurum yapısı

```
Kurum (Hastane / Klinik / Tıp Merkezi)
└── Sube (1 veya daha fazla)
    ├── CalismaSaati (gün bazında açılış-kapanış)
    ├── Bolum (Kardiyoloji, Radyoloji, Laboratuvar...)
    └── SaglikHizmeti (ad, kategori, süre, ücret) — her şubenin kendi hizmetleri ve fiyatları

Calisan (Doktor / SaglikPersoneli)
├── SubeId            → çalıştığı şube
├── HizmetIdleri      → sunduğu hizmetler
└── UygunlukDilimleri → haftalık randevu verebildiği saatler (ör. Pazartesi 09:00-12:00)
```

Tek şubeli bir klinik, 1 şubesi olan kurum olarak; çok şubeli bir hastane grubu ise
birden fazla şubesi olan kurum olarak tanımlanır.

## Randevu

```
Randevu
├── HastaId, CalisanId, SubeId, HizmetId   → ilişkili kayıtlar
├── HizmetAdi, SureDakika, Ucret           → randevu anında kopyalanır (fiyat sonradan değişse de korunur)
├── Baslangic, Bitis (= Baslangic + süre)
└── Durum: Beklemede → Onaylandi → Tamamlandi
                    ↘ Reddedildi / IptalEdildi
```

## Randevu kuralları (`RandevuServisi`)

Randevu oluşturulurken sırasıyla kontrol edilir; biri sağlanmazsa `RandevuKuraliException` fırlatılır:

1. Geçmiş bir zamana randevu alınamaz
2. Çalışan aktif olmalı, bir şubeye atanmış olmalı ve seçilen hizmeti sunmalı
3. Randevu şubenin çalışma saatleri içinde olmalı
4. Randevu çalışanın **uygunluk zaman dilimlerinden** birinin içinde olmalı
5. **Çakışma:** çalışanın aynı saatte başka randevusu olmamalı
6. **Çakışma:** hastanın aynı saatte başka randevusu olmamalı

Reddedilen ve iptal edilen randevular saati boşaltır, çakışma kontrolünde sayılmaz.

## Onay mekanizması

```
Beklemede ──Onayla──► Onaylandi ──Tamamla──► Tamamlandi
    │                     │
    ├──Reddet──► Reddedildi (sebep zorunlu)
    └──IptalEt──► IptalEdildi ◄──IptalEt──┘
```

Durum sadece `Randevu` sınıfının `Onayla`, `Reddet`, `IptalEt`, `Tamamla` metotlarıyla değişir.
Her metot önce **yetkiyi**, sonra **durum geçişinin** kurallara uygunluğunu kontrol eder.

Yetki kuralları her rolün kendi sınıfında `override` edilir (çok biçimlilik):

| Rol | Onaylama / Reddetme / Tamamlama | İptal |
|---|---|---|
| `Kullanici` (varsayılan) | Hayır | Hayır |
| `Hasta` | Hayır | Sadece kendi randevusu |
| `Calisan` (Doktor, Sağlık Personeli) | Sadece kendisine alınan randevular | Sadece kendisine alınan randevular |
| `Yonetici` | Tüm randevular | Tüm randevular |

Aynı şekilde `RandevuyuGorebilirMi` ve `RandevuOlusturabilirMi` de her rolde `override` edilir;
ekranlar kullanıcının rolünü kontrol etmek yerine bu metotlara sorar.

## Geliştirme yol haritası

Her adım ayrı bir commit olarak gönderilir.

| # | Adım | Durum |
|---|---|---|
| 1 | Proje iskeleti, README, .gitignore | ✅ |
| 2 | Kullanıcı sınıf hiyerarşisi (`Kullanici`, `Hasta`, `Doktor`, `SaglikPersoneli`, `Yonetici`) | ✅ |
| 3 | Kurum yapısı (`Kurum`, `Sube`, `Bolum`, `CalismaSaati`) | ✅ |
| 3b | Windows Forms arayüzü: ana pencere, kurumlar ve kullanıcılar ekranları | ✅ |
| 4 | Sağlık hizmetleri (`SaglikHizmeti`: süre, ücret), çalışanların şubesi ve sunduğu hizmetler | ✅ |
| 5 | Doktor uygunluk zaman dilimleri (`UygunlukDilimi`) | ✅ |
| 6 | Randevu modeli ve `RandevuDurumu`, randevu listesi ekranı | ✅ |
| 7 | Veri katmanı: `IVeriDeposu` arayüzü ve JSON ile kalıcı kayıt | ✅ |
| 8 | Randevu servisi, çakışma kontrolü ve randevu oluşturma ekranı | ✅ |
| 9 | Randevu onay mekanizması (onayla, reddet, iptal, tamamla) ve rol bazlı yetkiler | ✅ |
| 10 | Giriş sistemi ve rol bazlı menüler | ✅ |
| 11 | Yönetim ekranları: kurum/şube ekleme, çalışma saati ve bölüm düzenleme, çalışan kaydı ve pasife alma, hasta kaydı | ✅ |
| 12 | Örnek veriler, UML sınıf diyagramı, son düzenlemeler | ⏳ |
