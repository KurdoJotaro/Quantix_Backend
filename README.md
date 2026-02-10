# Quantix

**Personal Finance Tracker** — Gelir ve giderlerini tek bir yerden takip et.

Quantix, .NET 8 ile geliştirilmiş, güvenliği merkeze alan bir kişisel finans takip API'sidir. JWT tabanlı kimlik doğrulama, rol bazlı yetkilendirme, e-posta doğrulama ve Docker desteği ile gelir.

---

## Tech Stack

| Katman | Teknoloji |
|--------|-----------|
| Runtime | .NET 8 / ASP.NET Core |
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
| `GET` | `/` | Tüm kategorileri listele | Herkes |
| `POST` | `/` | Yeni kategori oluştur | Admin |
| `DELETE` | `/{id}` | Kategoriyi sil | Admin |

### Users (`/api/users`) — `[Authorize(Roles = "Admin")]`

| Method | Endpoint | Açıklama |
|--------|----------|----------|
| `GET` | `/` | Tüm kullanıcıları listele |
| `PUT` | `/{id}/ban` | Kullanıcıyı engelle / engeli kaldır |

---

## Kurulum

### Gereksinimler

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
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
```

### 3. Çalıştır

#### Lokal

```bash
dotnet run --project src/Quantix.API
```

#### Docker Compose

```bash
docker-compose up --build
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

## Varsayılan Seed Verileri

İlk çalıştırmada otomatik olarak oluşturulur:

| | Değer |
|---|---|
| **Admin E-posta** | `admin@quantix.com` |
| **Admin Şifre** | `Admin123!` |
| **Roller** | `Admin`, `User` |

---

## Güvenlik

- Şifreler ASP.NET Core Identity tarafından hash'lenir, düz metin olarak saklanmaz
- JWT token'lar `ClockSkew = TimeSpan.Zero` ile kesin süre kontrolüne tabidir
- Parola politikası: en az 6 karakter, büyük/küçük harf, rakam ve özel karakter zorunlu
- Kullanıcılar yalnızca kendi verilerine erişebilir
- Admin paneli rol bazlı yetkilendirme ile korunur
- Başarısız giriş denemeleri Serilog ile loglanır
- E-posta doğrulanmadan giriş yapılamaz

---

## Lisans

Bu proje şu an bir lisans altında dağıtılmamaktadır.
