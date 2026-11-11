# Hastane / Klinik Randevu Sistemi

Nesneye Dayalı Programlama dersi dönem projesi (Seçenek A).

C# (.NET 9) ile yazılmış, konsol tabanlı bir **hastane/klinik randevu sistemi**.
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
| Soyutlama (Abstraction) | `abstract` sınıflar ve `IRepository<T>` gibi arayüzler |

## Çalıştırma

Gerekli: [.NET 9 SDK](https://dotnet.microsoft.com/download)

```bash
dotnet run --project src/RandevuSistemi
```

## Proje klasör yapısı

```
nesne_programlama/
├── RandevuSistemi.slnx          # Çözüm dosyası (Visual Studio ile açılır)
├── README.md
└── src/
    └── RandevuSistemi/
        ├── RandevuSistemi.csproj
        ├── Program.cs           # Giriş noktası ve ana menü
        ├── GeciciTestler.cs     # Geliştirme sırasında sınıfları denemek için (geçici)
        └── Models/
            ├── Dogrulama.cs     # Ortak doğrulama metotları
            ├── Kullanicilar/    # Kullanıcı sınıf hiyerarşisi
            └── Kurumlar/        # Kurum, şube, bölüm, çalışma saatleri
```

## Kurum yapısı

```
Kurum (Hastane / Klinik / Tıp Merkezi)
└── Sube (1 veya daha fazla)
    ├── CalismaSaati (gün bazında açılış-kapanış)
    └── Bolum (Kardiyoloji, Radyoloji, Laboratuvar...)
```

Tek şubeli bir klinik, 1 şubesi olan kurum olarak; çok şubeli bir hastane grubu ise
birden fazla şubesi olan kurum olarak tanımlanır.

## Kullanıcı sınıf hiyerarşisi

```
Kullanici (abstract)
├── Hasta
├── Calisan (abstract)
│   ├── Doktor
│   └── SaglikPersoneli
└── Yonetici
```

## Geliştirme yol haritası

Her adım ayrı bir commit olarak gönderilir.

| # | Adım | Durum |
|---|---|---|
| 1 | Proje iskeleti, README, .gitignore | ✅ |
| 2 | Kullanıcı sınıf hiyerarşisi (`Kullanici`, `Hasta`, `Doktor`, `SaglikPersoneli`, `Yonetici`) | ✅ |
| 3 | Kurum yapısı (`Kurum`, `Sube`, `Bolum`, `CalismaSaati`) | ✅ |
| 4 | Sağlık hizmetleri (`SaglikHizmeti`: süre, ücret) ve doktor uzmanlıkları | ⏳ |
| 5 | Doktor uygunluk zaman dilimleri (`UygunlukDilimi`) | ⏳ |
| 6 | Randevu modeli ve `RandevuDurumu` | ⏳ |
| 7 | Veri katmanı: `IRepository<T>` ve JSON ile kalıcı kayıt | ⏳ |
| 8 | Randevu servisi ve çakışma kontrolü | ⏳ |
| 9 | Randevu onay mekanizması | ⏳ |
| 10 | Giriş sistemi ve rol bazlı menüler | ⏳ |
| 11 | Yönetici ve doktor işlemleri, çok şube desteği | ⏳ |
| 12 | Örnek veriler, UML sınıf diyagramı, son düzenlemeler | ⏳ |
