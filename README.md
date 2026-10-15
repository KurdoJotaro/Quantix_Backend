# Quantix

**Personal Finance Tracker** — Gelir ve giderlerini tek bir yerden takip et.

Quantix, .NET 9 ile geliştirilmiş bir kişisel finans takip API'sidir. JWT tabanlı kimlik doğrulama, rol bazlı yetkilendirme, e-posta doğrulama ve Docker desteği ile gelir.

---

## Tech Stack

| Katman | Teknoloji |
|--------|-----------|
| Runtime | .NET 9 / ASP.NET Core |
| ORM | Entity Framework Core |
| Veritabanı | MySQL 8.0 |
| Kimlik | ASP.NET Core Identity |
| Token | JWT (Access Token) |
| Loglama | Serilog |
| Mapping | AutoMapper |
| Container | Docker & Docker Compose |
| Docs | Swagger / OpenAPI |

---

## Proje Yapısı

```
Quantix/
├── src/
│   ├── Quantix.API          # Web API, Controller'lar, Middleware
│   ├── Quantix.Core         # Entity'ler, DTO'lar, Interface'ler, Enum'lar
│   ├── Quantix.Data         # DbContext, Migration'lar, Repository, Seed
│   └── Quantix.Service      # İş mantığı (Auth, Token, Email, Transaction, Category)
├── Dockerfile
├── docker-compose.yml
├── .env.example
└── Quantix.slnx
```

---

## API Endpoints

### Auth (`/api/auth`)

| Method | Endpoint | Açıklama |
|--------|----------|----------|
| `POST` | `/register` | Yeni kullanıcı kaydı (e-posta doğrulama gerekir) |
| `POST` | `/login` | Giriş yap, JWT token al |
| `GET` | `/verify-email` | E-posta doğrulama linki |

### Transactions (`/api/transactions`) — `[Authorize]`

| Method | Endpoint | Açıklama |
|--------|----------|----------|
| `GET` | `/` | Kullanıcının tüm işlemlerini listele |
| `GET` | `/{id}` | Belirli bir işlemi getir |
| `POST` | `/` | Yeni işlem oluştur |
| `PUT` | `/{id}` | İşlemi güncelle |
| `DELETE` | `/{id}` | İşlemi sil |

> Her kullanıcı sadece kendi işlemlerini görebilir ve düzenleyebilir.

### Categories (`/api/categories`) — `[Authorize]`

| Method | Endpoint | Açıklama | Yetki |
|--------|----------|----------|-------|
| `GET` | `/` | Kendi kategorilerini ve eski ortak kategorileri listele | Giriş yapan kullanıcı |
| `POST` | `/` | Kullanıcıya özel kategori oluştur | Giriş yapan kullanıcı |
| `DELETE` | `/{id}` | Kendi kategorisini sil; eski ortak kategorileri yalnız admin silebilir | Kategori sahibi / ortak kategori için admin |

Yeni kategorinin sahibi token'daki kullanıcı kimliğinden atanır; istemci sahip kimliği göndermez. Adminin oluşturduğu kategori de kendisine özeldir. Başka bir kullanıcının özel kategorileri admin dahil diğer kullanıcılara listelenmez ve onların işlemlerinde kullanılamaz. Kategori yanıtındaki `canManage`, kullanıcının o kategori üzerinde silme yetkisi olduğunu; `isShared` ise önceki sürümden kalan ortak kategori olduğunu belirtir. İşlem geçmişinde kullanılmış kategoriler, silinmiş işlemler dahil, silinemez.

### Users (`/api/users`) — `[Authorize(Roles = "Admin")]`

| Method | Endpoint | Açıklama |
|--------|----------|----------|
| `GET` | `/` | Tüm kullanıcıları listele |
| `PUT` | `/{id}/ban` | Kullanıcıyı engelle / engeli kaldır |

---

## Kurulum

### Gereksinimler

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (veya daha yeni uyumlu SDK ve .NET 9 runtime)
- [MySQL 8.0](https://dev.mysql.com/downloads/) (veya Docker)
- [Docker](https://www.docker.com/) (opsiyonel)

### 1. Repo'yu klonla

```bash
git clone https://github.com/KurdoJotaro/Quantix_Backend.git
cd Quantix_Backend
```

### 2. Environment değişkenlerini ayarla

`.env.example` dosyasını `.env` olarak kopyala ve kendi değerlerini gir:

```bash
cp .env.example .env
```

```env
# Veritabanı
MYSQL_ROOT_PASSWORD=GUCLU_VERITABANI_SIFREN
ConnectionStrings__DefaultConnection=Server=localhost;Port=3306;Database=quantixdb;User=root;Password=SIFREN;

# JWT
Jwt__Issuer=QuantixAPI
Jwt__Audience=QuantixClient
Jwt__SecurityKey=EN_AZ_32_KARAKTER_UZUNLUGUNDA_GIZLI_ANAHTAR

# E-posta (SMTP)
Email__SmtpHost=smtp.gmail.com
Email__SmtpPort=587
Email__SmtpUser=senin-email@gmail.com
Email__SmtpPass=uygulama-sifresi
Email__FromEmail=noreply@quantix.com
Email__FromName=Quantix

# Uygulama
App__BaseUrl=http://localhost:5000
App__FrontendUrl=http://localhost:3000
Cors__AllowedOrigins__0=http://localhost:3000

# İsteğe bağlı admin hesabı: ikisini birlikte doldur
Seed__AdminEmail=
Seed__AdminPassword=
```

### 3. Çalıştır

#### Lokal

```bash
dotnet run --project src/Quantix.API
```

#### Docker Compose

```bash
docker compose up --build
```

Bu komut MySQL ve API container'larını ayağa kaldırır. API `http://localhost:5000` üzerinden erişilebilir olur.

---

## Swagger

Geliştirme ortamında Swagger UI aktiftir:

```
http://localhost:5000/swagger
```

JWT token ile korunan endpointleri test etmek için Swagger'daki kilit ikonuna tıklayıp token'ınızı girin.

---

## Başlangıç Verileri

İlk çalıştırmada `Admin` ve `User` rolleri oluşturulur. Admin hesabı yalnızca `.env` dosyasındaki `Seed__AdminEmail` ve `Seed__AdminPassword` birlikte doldurulduğunda oluşturulur. Hazır bir admin parolası yoktur. Parola en az 6 karakter ve büyük/küçük harf, rakam ve özel karakter içermelidir.

Kategoriler hazır gelmez; giriş yapan her kullanıcı `/api/categories` üzerinden kendi özel kategorilerini ekleyebilir. `type: 0` gelir, `type: 1` gider anlamındadır. Her işlemde tutar pozitiftir ve en fazla iki ondalık basamak içerir; gelir/gider ayrımı işlem yanıtındaki `categoryType` ile yapılır. Önceki sürümde oluşturulmuş kategoriler sahip kimliği boş bırakılarak ortak tutulur; mevcut kayıtlar ve işlem geçmişi korunur. Sahiplik migration'ı yalnız nullable `AppUserId` sütunu, indeks ve ilişki ekler.

Önceki sürümle oluşturulmuş bir veritabanındaki hesaplar ve parolalar kendiliğinden değişmez. Daha önce varsayılan admin hesabı oluşturulduysa parolasını ayrıca değiştirin.

SMTP bilgileri gerçek bir posta sunucusuna ait olmalıdır. Doğrulama e-postası gönderilemezse kayıt tamamlanmaz; ayarları düzelttikten sonra yeniden kayıt olun. E-postadaki bağlantı `App__FrontendUrl` altındaki `/verify-email` sayfasına gider; frontend bu sayfada API'nin `/api/auth/verify-email` endpointini çağırır.

## Doğrulama

```bash
dotnet restore Quantix.slnx
dotnet build Quantix.slnx --no-restore
dotnet test Quantix.slnx --no-build
```

Testler her test için bellekte geçici SQLite veritabanı oluşturur; MySQL veya SMTP bağlantısı gerektirmez. Kullanıcılar arası veri izolasyonu, kategori geçmişinin korunması, tutar doğrulaması, engellenen kullanıcıların token reddi, e-posta hatasında kayıt geri alma ve hata yanıtlarının bilgi sızdırmaması kontrol edilir. Gerçek MySQL migration ve SMTP teslimatı ayrıca uçtan uca doğrulanmalıdır.

Migration araçlarını kullanmak için proje kökünde `dotnet tool restore` çalıştırın; yerel `dotnet-ef` manifesti `.config/dotnet-tools.json` içindedir ve EF Core 9 ile aynı sürümü kullanır.

---

## Güvenlik

- Şifreler ASP.NET Core Identity tarafından hash'lenir, düz metin olarak saklanmaz
- JWT token'lar `ClockSkew = TimeSpan.Zero` ile kesin süre kontrolüne tabidir
- Parola politikası: en az 6 karakter, büyük/küçük harf, rakam ve özel karakter zorunlu
- Kullanıcılar yalnızca kendi verilerine erişebilir
- Admin paneli rol bazlı yetkilendirme ile korunur
- İstek hataları Serilog ile loglanır; ham exception ayrıntıları istemciye gönderilmez
- E-posta doğrulanmadan giriş yapılamaz
- Hesabı engellenen veya silinen kullanıcıların mevcut JWT token'ları da reddedilir

---

## Lisans

Bu proje şu an bir lisans altında dağıtılmamaktadır.
