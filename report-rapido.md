# Report Rapido — projectwork-backend

## Stack tecnologico
- **Framework:** ASP.NET Core 10 (Minimal API)
- **Database:** PostgreSQL (via Npgsql + Dapper)
- **Autenticazione:** JWT (cookie HttpOnly) + Refresh Token
- **Hashing password:** Argon2id
- **Documentazione:** Swagger / OpenAPI

## Architettura
Il progetto è organizzato in tre layer:
- **Endpoints** — definiscono le route HTTP (Minimal API, niente Controller classici)
- **Services** — contengono la logica di business e le query SQL tramite Dapper
- **Models** — le entità (`User`, `Product`, `Image`)

## Ruoli utente
| Ruolo | Permessi |
|---|---|
| Utente anonimo | lettura prodotti e foto, registrazione |
| Utente autenticato | prenotazione/cancellazione proprie prenotazioni |
| Collaboratore | CRUD completo su prodotti, foto, utenti |
| Admin | tutto il precedente + gestione collaboratori |

## Endpoint disponibili

### Auth — `/api/auth`
| Metodo | Route | Descrizione |
|---|---|---|
| POST | `/login` | Login, imposta cookie AccessToken + RefreshToken |
| GET | `/logout` | Cancella i cookie di sessione |
| GET | `/refresh` | Rinnova l'AccessToken usando il RefreshToken |

### Utenti — `/api/users`
| Metodo | Route | Auth richiesta | Descrizione |
|---|---|---|---|
| POST | `/register` | No | Registrazione nuovo utente |
| GET | `/user` | Sì | Profilo dell'utente loggato |
| GET | `/` | Admin/Collab | Lista tutti gli utenti |
| GET | `/{id}` | Admin/Collab | Dettaglio utente per ID |
| PUT | `/` | Admin/Collab | Aggiorna un utente |
| DELETE | `/{id}` | Admin/Collab | Elimina un utente (admin non eliminabile) |
| PUT | `/opCollaborator/{id}` | Admin | Promuove a collaboratore |
| PUT | `/deopCollaborator/{id}` | Admin | Rimuove ruolo collaboratore |

### Prodotti — `/api/products`
| Metodo | Route | Auth richiesta | Descrizione |
|---|---|---|---|
| GET | `/` | No | Lista tutti i prodotti |
| GET | `/{id}` | No | Prodotto per ID |
| POST | `/` | Admin/Collab | Crea prodotto |
| PUT | `/` | Admin/Collab | Aggiorna prodotto |
| DELETE | `/{id}` | Admin/Collab | Elimina prodotto |
| PUT | `/sell/{id}/{qty}` | Admin/Collab | Registra vendita (decrementa available, incrementa sold) |
| PUT | `/addAvailable/{id}/{qty}` | Admin/Collab | Aggiunge unità disponibili |

### Foto — `/api/photos`
| Metodo | Route | Auth richiesta | Descrizione |
|---|---|---|---|
| GET | `/` | No | Lista tutte le foto |
| GET | `/{id}` | No | Foto per ID |
| GET | `/filter/{state}` | No | Foto filtrate per stato (`available`, `booked`, `sold`) |
| POST | `/upload` | Admin/Collab | Upload foto (multipart/form-data) |
| PUT | `/` | Admin/Collab | Aggiorna metadati foto |
| DELETE | `/{id}` | Admin/Collab | Elimina foto (rimuove anche il file) |
| PUT | `/book/{imageId}/{userId}` | Sì | Prenota una foto |
| PUT | `/unbook/{imageId}` | Sì | Cancella prenotazione |
| PUT | `/setsold/{imageId}` | Admin/Collab | Segna foto come venduta |

## Sicurezza
- AccessToken JWT in cookie `HttpOnly`, `Secure`, `SameSite=Strict`, durata **15 minuti**
- RefreshToken (GUID) hashato con SHA-256 e salvato in DB (`sessions`), durata **7 giorni**
- Password hashata con **Argon2id** (salt casuale, 32 byte output)
- L'admin non può essere eliminato tramite API
