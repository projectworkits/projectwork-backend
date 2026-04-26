# Report Dettagliato — projectwork-backend

> Questo documento descrive il funzionamento completo del backend, pensato per essere comprensibile anche a chi non ha mai visto il progetto.

---

## 1. Cos'è questo progetto?

È un **backend web** scritto in C# con il framework **ASP.NET Core 10**. Un backend è la parte "invisibile" di un'applicazione: non ha un'interfaccia grafica, ma espone delle **API REST** — indirizzi web a cui il frontend (app, sito) manda richieste per ottenere o modificare dati.

In questo caso il sistema gestisce:
- un **catalogo di fotografie** (con stati: disponibile, prenotata, venduta)
- un **catalogo di prodotti** fisici (con quantità disponibili e vendute)
- la **gestione degli utenti** con ruoli differenziati

---

## 2. Stack tecnologico

| Componente | Tecnologia | Scopo |
|---|---|---|
| Linguaggio | C# | Linguaggio principale |
| Framework | ASP.NET Core 10 | Gestione HTTP, routing, DI |
| Database | PostgreSQL | Persistenza dei dati |
| ORM | Dapper + Npgsql | Esecuzione query SQL |
| Autenticazione | JWT + Cookie | Sessioni sicure |
| Hashing password | Argon2id | Protezione credenziali |
| Documentazione API | Swagger / OpenAPI | Interfaccia di test interattiva |

Il progetto target **.NET 10** (versione più recente al momento della scrittura) e usa lo stile **Minimal API** di ASP.NET, che è più compatto rispetto ai classici Controller MVC.

---

## 3. Struttura del codice

```
projectWork/
├── Program.cs                  ← Punto di avvio, configurazione globale
├── appsettings.json            ← Configurazione (connessione DB, segreto JWT)
├── projectWork.csproj          ← Dipendenze NuGet
│
├── Authentication/
│   ├── Authentication.cs       ← Logica JWT e refresh token
│   ├── AuthEndpoints.cs        ← Route: /api/auth/*
│   └── PasswordServices.cs     ← Hashing Argon2id
│
├── Endpoints/
│   ├── UsersEndpoints.cs       ← Route: /api/users/*
│   ├── ProductsEndpoints.cs    ← Route: /api/products/*
│   └── ImagesEndpoints.cs      ← Route: /api/photos/*
│
├── Services/
│   ├── UsersServices.cs        ← Query SQL sugli utenti
│   ├── ProductsServices.cs     ← Query SQL sui prodotti
│   └── ImagesServices.cs       ← Query SQL sulle foto + gestione file
│
└── Models/
    ├── User.cs                 ← Entità utente
    ├── Product.cs              ← Entità prodotto
    └── Image.cs                ← Entità foto (con enum PhotoState)
```

### Il pattern usato: Endpoints → Services → DB

Ogni richiesta HTTP segue questo flusso:

```
Client HTTP
    ↓
Endpoint (AuthEndpoints / UsersEndpoints / ...)
    ↓ chiama
Service (UsersServices / ProductsServices / ...)
    ↓ esegue SQL tramite Dapper
PostgreSQL
```

Questo separa le responsabilità: gli Endpoint si occupano solo di ricevere la richiesta e rispondere; i Service si occupano solo di interagire col database.

---

## 4. Avvio dell'applicazione — `Program.cs`

`Program.cs` è il file principale che viene eseguito all'avvio. Fa tre cose:

**a) Registra i servizi** nella dependency injection (il sistema che crea le istanze delle classi):
```csharp
builder.Services.AddScoped<Authentication>();
builder.Services.AddScoped<PasswordServices>();
builder.Services.AddScoped<UsersServices>();
builder.Services.AddScoped<ProductsServices>();
builder.Services.AddScoped<ImagesServices>();
```
`AddScoped` significa che ogni richiesta HTTP riceve la propria istanza del servizio.

**b) Configura l'autenticazione JWT** per leggere il token dal cookie `AccessToken` invece che dall'header `Authorization`:
```csharp
OnMessageReceived = ctx => {
    ctx.Token = ctx.Request.Cookies["AccessToken"];
    return Task.CompletedTask;
}
```
Questa scelta è più sicura contro attacchi XSS perché i cookie `HttpOnly` non sono accessibili da JavaScript.

**c) Registra le route** chiamando i metodi di estensione di ogni modulo:
```csharp
app.AddAuthenticationEndpoints();
app.AddUsersEndpoints();
app.AddImagesEndpoints();
app.AddProductsEndpoints();
```

---

## 5. Autenticazione e sicurezza

### 5.1 Flusso di login

```
1. Client invia POST /api/auth/login con { username, password }
2. Il server cerca l'utente nel DB per username
3. Ricalcola l'hash Argon2id della password con il salt salvato
4. Confronta in tempo costante (FixedTimeEquals) per prevenire timing attacks
5. Se ok: genera AccessToken JWT (15 min) e RefreshToken (GUID, 7 giorni)
6. RefreshToken viene hashato SHA-256 e salvato nel DB (tabella sessions)
7. Entrambi i token vengono impostati come cookie HttpOnly, Secure, SameSite=Strict
8. Il client non vede mai i token — sono gestiti automaticamente dal browser
```

### 5.2 AccessToken (JWT)

Un **JWT (JSON Web Token)** è un token firmato che contiene informazioni sull'utente. In questo progetto contiene solo il `userId`:
```json
{ "userId": "42" }
```
La firma usa **HMAC-SHA256** con una chiave segreta configurabile in `appsettings.json` (`jwtSecret`). Il token scade dopo **15 minuti** e il `ClockSkew` è impostato a zero, quindi scade esattamente all'ora prevista senza tolleranze.

### 5.3 RefreshToken

Il RefreshToken serve per ottenere un nuovo AccessToken senza dover fare login di nuovo:

```
Client → GET /api/auth/refresh
Server legge il cookie RefreshToken
     → hash SHA-256 del token
     → cerca nel DB se esiste e non è scaduto/revocato
     → se ok, genera nuovo AccessToken e lo imposta come cookie
```

Il token viene **hashato** prima di essere salvato nel DB: così, anche in caso di data breach, i token nel database sono inutilizzabili senza la stringa originale.

### 5.4 Hashing delle password — Argon2id

Argon2id è lo standard consigliato per l'hashing di password. I parametri usati:
- `DegreeOfParallelism = 2` — usa 2 thread in parallelo
- `MemorySize = 16384` — usa 16 MB di RAM (rende difficile attacchi con GPU)
- `Iterations = 4` — 4 passaggi dell'algoritmo
- Output: 32 byte → convertiti in Base64

Ogni password ha un **salt casuale** (16 byte) generato con `RandomNumberGenerator` (crittograficamente sicuro). Salt e hash vengono salvati separatamente nel DB.

### 5.5 Ruoli

Non esiste un sistema di ruoli complesso: i ruoli sono due campi booleani nella tabella `users`:
- `Admin` — accesso totale (non modificabile via API, solo da DB)
- `Collaborator` — può essere assegnato/rimosso dall'admin

Il check viene fatto in ogni endpoint protetto:
```csharp
if (!(await usersServices.IsAdmin(userId) || await usersServices.IsCollaborator(userId)))
    return TypedResults.Forbid();
```

---

## 6. Modelli dati

### User
```
UserId       int       — chiave primaria
Username     string    — nome utente
Email        string    — email
PasswordSalt string    — salt Argon2id (Base64)
PasswordHash string    — hash Argon2id (Base64)
Admin        bool      — ruolo admin (default: false)
Collaborator bool      — ruolo collaboratore (default: false)
```

### Product
```
ProductId    int       — chiave primaria
Name         string    — nome prodotto
Description  string    — descrizione
Price        decimal   — prezzo
Available    int       — unità disponibili
Sold         int       — unità vendute (default: 0)
```

### Image (Photo)
```
PhotoId       int        — chiave primaria
Title         string     — titolo lavorativo
OriginalTitle string     — titolo originale dell'opera
Date          string     — data dello scatto
Place         string     — luogo
Path          string     — percorso file sul server (es: /photos/uuid.jpg)
Description   string?    — descrizione opzionale
State         PhotoState — enum: available | booked | sold
Price         decimal    — prezzo (default: 0)
BookedBy      int?       — userId di chi ha prenotato (null se libera)
```

---

## 7. API Reference completa

La base URL in sviluppo è `http://localhost:<porta>`. Con Swagger attivo in development, è disponibile un'interfaccia grafica interattiva su `/swagger`.

---

### 7.1 Auth — `/api/auth`

#### `POST /api/auth/login`
Effettua il login e imposta i cookie di sessione.

**Body (JSON):**
```json
{
  "username": "mario",
  "password": "segretissima"
}
```

**Risposte:**
- `200 OK` — login riuscito, cookie impostati
- `401 Unauthorized` — credenziali errate o campi vuoti

**Note:** Questo endpoint disabilita l'antiforgery (necessario per chiamate API da client non-browser).

---

#### `GET /api/auth/logout`
Cancella i cookie `AccessToken` e `RefreshToken`.

**Risposte:**
- `200 OK` — sempre (anche se non si era loggati)

---

#### `GET /api/auth/refresh`
Rinnova l'AccessToken usando il RefreshToken nel cookie.

**Risposte:**
- `200 OK` — nuovo AccessToken impostato nel cookie
- `401 Unauthorized` — RefreshToken mancante, scaduto o revocato

---

### 7.2 Utenti — `/api/users`

#### `POST /api/users/register`
Registra un nuovo utente. Non richiede autenticazione.

**Body (JSON):**
```json
{
  "username": "mario",
  "password": "segretissima",
  "email": "mario@example.com"
}
```

**Risposte:**
- `201 Created` — registrazione riuscita
- `400 Bad Request` — uno o più campi vuoti

---

#### `GET /api/users/user`
Restituisce i dati dell'utente attualmente autenticato.

**Auth:** Richiede AccessToken valido nel cookie.

**Risposta `200 OK`:**
```json
{
  "userId": 42,
  "username": "mario",
  "email": "mario@example.com",
  "admin": false,
  "collaborator": false
}
```

---

#### `GET /api/users/`
Restituisce la lista di tutti gli utenti.

**Auth:** Admin o Collaboratore.

**Risposta `200 OK`:** array di oggetti User.

---

#### `GET /api/users/{id}`
Restituisce un utente per ID.

**Auth:** Admin o Collaboratore.

**Risposte:**
- `200 OK` — utente trovato
- `404 Not Found` — ID inesistente

---

#### `PUT /api/users/`
Aggiorna i dati di un utente (username, email, password hash/salt, collaborator).

**Auth:** Admin o Collaboratore.

**Body (JSON):** oggetto `User` completo.

**Risposte:**
- `204 No Content` — aggiornamento riuscito
- `404 Not Found` — utente non trovato

**Nota:** il campo `admin` non viene aggiornato tramite questa API.

---

#### `DELETE /api/users/{id}`
Elimina un utente e revoca le sue sessioni.

**Auth:** Admin o Collaboratore.

**Risposte:**
- `204 No Content` — eliminazione riuscita
- `404 Not Found` — utente non trovato
- `200 OK` con `flag{bel tentativo, ma eliminare l'admin non è così facile}` — tentativo di eliminare l'admin (easter egg 🥚)

---

#### `PUT /api/users/opCollaborator/{id}`
Promuove l'utente con quell'ID a Collaboratore.

**Auth:** Solo Admin.

**Risposte:** `204 No Content` | `404 Not Found` | `403 Forbidden`

---

#### `PUT /api/users/deopCollaborator/{id}`
Rimuove il ruolo Collaboratore dall'utente.

**Auth:** Solo Admin.

**Risposte:** `204 No Content` | `404 Not Found` | `403 Forbidden`

---

### 7.3 Prodotti — `/api/products`

#### `GET /api/products/`
Restituisce tutti i prodotti. Nessuna auth richiesta.

**Risposta `200 OK`:**
```json
[
  {
    "productId": 1,
    "name": "Stampa A3",
    "description": "Stampa fotografica su carta Fine Art",
    "price": 45.00,
    "available": 10,
    "sold": 3
  }
]
```

---

#### `GET /api/products/{id}`
Restituisce un prodotto per ID.

**Risposte:** `200 OK` | `404 Not Found`

---

#### `POST /api/products/`
Crea un nuovo prodotto.

**Auth:** Admin o Collaboratore.

**Body (JSON):**
```json
{
  "name": "Stampa A3",
  "description": "Stampa fotografica su carta Fine Art",
  "price": 45.00,
  "available": 10
}
```

**Risposte:** `201 Created` | `400 Bad Request` | `401 Unauthorized` | `403 Forbidden`

---

#### `PUT /api/products/`
Aggiorna un prodotto esistente.

**Auth:** Admin o Collaboratore.

**Body (JSON):** oggetto `Product` completo (con `productId`).

**Risposte:** `204 No Content` | `404 Not Found`

---

#### `DELETE /api/products/{id}`
Elimina un prodotto.

**Auth:** Admin o Collaboratore.

**Risposte:** `204 No Content` | `404 Not Found`

---

#### `PUT /api/products/sell/{productId}/{quantity}`
Registra la vendita di `quantity` unità: decrementa `available` e incrementa `sold`.

**Auth:** Admin o Collaboratore.

**Esempio:** `PUT /api/products/sell/1/3` → vende 3 unità del prodotto 1.

**Risposte:** `204 No Content` | `401 Unauthorized` | `403 Forbidden`

---

#### `PUT /api/products/addAvailable/{productId}/{quantity}`
Aggiunge `quantity` unità disponibili al prodotto (riassortimento).

**Auth:** Admin o Collaboratore.

**Risposte:** `204 No Content` | `401 Unauthorized` | `403 Forbidden`

---

### 7.4 Foto — `/api/photos`

#### `GET /api/photos/`
Restituisce tutte le foto. Nessuna auth richiesta.

**Risposta `200 OK`:**
```json
[
  {
    "photoId": 1,
    "title": "Alba sul Po",
    "originalTitle": "Po Sunrise",
    "date": "2024-03-15",
    "place": "Torino",
    "path": "/photos/3f2504e0-4f89-11d3-9a0c-0305e82c3301.jpg",
    "description": "Alba sul fiume Po",
    "state": "available",
    "price": 120.00,
    "bookedBy": null
  }
]
```

---

#### `GET /api/photos/{id}`
Restituisce una foto per ID.

**Risposte:** `200 OK` | `404 Not Found`

---

#### `GET /api/photos/filter/{state}`
Filtra le foto per stato. Valori validi: `available`, `booked`, `sold`.

**Esempio:** `GET /api/photos/filter/available`

**Risposte:** `200 OK` con lista filtrata

---

#### `POST /api/photos/upload`
Carica una nuova foto con i suoi metadati.

**Auth:** Admin o Collaboratore.

**Content-Type:** `multipart/form-data`

**Campi form:**
| Campo | Tipo | Obbligatorio | Descrizione |
|---|---|---|---|
| `photo` | file | Sì | File immagine |
| `title` | string | Sì | Titolo lavorativo |
| `originalTitle` | string | Sì | Titolo originale |
| `date` | string | Sì | Data scatto |
| `place` | string | Sì | Luogo |
| `description` | string | No | Descrizione |
| `state` | string | Sì | `available`/`booked`/`sold` |
| `price` | decimal | Sì | Prezzo |

Il file viene salvato fisicamente in `/frontend/photos/` con un nome UUID generato automaticamente. Il percorso viene salvato nel DB.

**Risposte:** `201 Created` | `400 Bad Request` | `401 Unauthorized` | `403 Forbidden`

---

#### `PUT /api/photos/`
Aggiorna i metadati di una foto.

**Auth:** Admin o Collaboratore.

**Body (JSON):** oggetto `Image` completo (con `photoId`).

**Risposte:** `204 No Content` | `404 Not Found`

---

#### `DELETE /api/photos/{id}`
Elimina una foto dal DB **e** cancella il file fisico dal server.

**Auth:** Admin o Collaboratore.

**Risposte:** `204 No Content` | `404 Not Found`

---

#### `PUT /api/photos/book/{imageId}/{userId}`
Prenota una foto per un utente. Fallisce se già prenotata.

**Auth:** Richiede AccessToken valido.

**Risposte:**
- `204 No Content` — prenotazione riuscita
- `404 Not Found` — foto o utente non trovato
- `403 Forbidden` — foto già prenotata

---

#### `PUT /api/photos/unbook/{imageId}`
Cancella la prenotazione di una foto.

**Auth:** Richiede AccessToken valido.

**Logica di autorizzazione:**
- Se l'utente è **Admin o Collaboratore** → può sempre annullare qualsiasi prenotazione
- Altrimenti → può annullare solo la propria prenotazione (verifica che `bookedBy == userId`)

**Risposte:** `204 No Content` | `404 Not Found` | `403 Forbidden`

---

#### `PUT /api/photos/setsold/{imageId}`
Segna una foto come venduta (cambia `state` → `"sold"`).

**Auth:** Admin o Collaboratore.

**Risposte:** `204 No Content` | `404 Not Found`

---

## 8. Database

Il progetto usa **PostgreSQL** con connessione configurata in `appsettings.json`:
```json
"ConnectionStrings": {
  "db": "Server=127.0.0.1;Port=5434;Database=projectWork;User Id=admin;Password=admin;"
}
```

Le query vengono eseguite manualmente tramite **Dapper** (micro-ORM), senza alcuna migrazione automatica. Le tabelle coinvolte sono:

| Tabella | Contenuto |
|---|---|
| `users` | Utenti con credenziali e ruoli |
| `sessions` | Refresh token hashati con scadenza |
| `products` | Catalogo prodotti |
| `photos` | Catalogo foto con stato e prenotazioni |

La tabella `photos` usa un tipo enum PostgreSQL nativo (`photo_state`) con i valori `available`, `booked`, `sold`. Per questo motivo il cast `::photo_state` è necessario nelle query di insert/update.

**Dapper** è configurato con `MatchNamesWithUnderscores = true`, il che fa sì che le colonne snake_case del DB (es. `user_id`, `photo_id`) vengano mappate automaticamente alle proprietà PascalCase del C# (es. `UserId`, `PhotoId`).

---

## 9. Dipendenze NuGet

| Pacchetto | Versione | Scopo |
|---|---|---|
| `Dapper` | 2.1.72 | Micro-ORM per query SQL |
| `Npgsql` | 10.0.2 | Driver PostgreSQL per .NET |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.5 | Autenticazione JWT |
| `Microsoft.AspNetCore.OpenApi` | 10.0.5 | Generazione OpenAPI/Swagger |
| `Swashbuckle.AspNetCore` | 10.1.7 | UI Swagger interattiva |
| `Konscious.Security.Cryptography.Argon2` | 1.3.1 | Hashing Argon2id |

---

## 10. Note e osservazioni

- **Swagger** è attivo solo in ambiente `Development`. In produzione non è esposto.
- I file immagine vengono scritti direttamente nel filesystem sotto `/frontend/photos/`. Questo presuppone che backend e frontend condividano lo stesso server (o volume).
- La cancellazione di una foto (`DELETE /api/photos/{id}`) usa `RETURNING path` nella query SQL per recuperare il percorso del file da eliminare — un pattern elegante che evita una query extra.
- L'utente Admin non è eliminabile via API (è una protezione hardcoded, più un easter egg con flag CTF incluso).
- Non è presente rate limiting né validazione avanzata degli input: da implementare in produzione.
- La connessione al DB viene aperta e chiusa per ogni operazione (pattern corretto con Dapper e connection pooling di Npgsql).
