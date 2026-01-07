# Protokoll — Media Ratings Platform (MRP)

## 0) Metadaten
- **Projekt:** Media Ratings Platform (MRP)
- **Student:** Dominik Eifinger
- **Identifier:** if25b207
- **Git Repository:** https://github.com/Dominik1905/MediaRatingsPlatform

---

## 1) Ziel & Scope (laut Specification)
Ziel ist eine **standalone REST API** (C#) mit **pure HTTP** (z.B. HttpListener) ohne Webframework, die von beliebigen Frontends verwendet werden kann.  
Persistenz erfolgt in **PostgreSQL**.

Funktionen (high level):
- Registrierung / Login mit Token
- Media CRUD
- Ratings (1–5) + optionaler Kommentar
- Kommentar-Moderation: Kommentar wird erst nach Bestätigung öffentlich
- Likes auf Ratings (1 Like pro User pro Rating)
- Favoriten markieren / entfernen
- Profil + Statistiken, Rating-History, Favoriten-Liste
- Leaderboard
- Recommendations (Genre/Typ/Age Similarity + Ratingverhalten)

---

## 2) Technische Architektur & Entscheidungen

### 2.1 Architektur-Überblick
**Schichten / Verantwortungen:**
- `MRP_Server`
  - `HttpServer` + `Router`: minimaler HTTP Server + Routing
  - `Controllers`: Request-Parsing, Auth-Checks, Response-Codes, DTO/JSON
  - `AuthService/AuthHelper`: Token-Handling, Login/Register
  - `RecommendationEngine`: reine Business-Logik (testbar, unabhängig von HTTP/DB)
- `MRP_Database`
  - `Models` (User/Media/Rating)
  - `DatabaseService`: Datenzugriff via **parameterized SQL** (SQL-Injection safe)
- `MRP_Server.Tests`
  - xUnit Tests für Core-Logik (Parser + RecommendationEngine)

**Warum so?**
- Controller bleiben „thin“ (nur HTTP/Validierung), Business-Logik testbar in Services.
- `DatabaseService` kapselt DB komplett → Änderungen am DB-Layer wirken nicht auf Routing/Controller.
- Empfehlungssystem als eigener Service → einfach unit-testbar ohne Postgres.

### 2.2 HTTP Stack Entscheidung
- **HttpListener** statt ASP.NET: entspricht „pure HTTP“-Vorgabe der Spec.
- JSON Serialisierung via `System.Text.Json` (zulässig laut Spec).

### 2.3 SQL-Injection Schutz
- Durchgehend **NpgsqlCommand + Parameters** (`@Param`) statt String-Concatenation.

---

## 3) Datenbank

### 3.1 Setup (Docker)
`docker-compose.yml` startet PostgreSQL (Port `15432`).

Start:
```bash
docker compose -v up
```

### 3.2 Schema (Kurzüberblick)
- `Users(Id, Username, PasswordHash)`
- `Media(Id, Title, Description, Type, ReleaseYear, Genre, AgeRestriction, CreatedByUserId)`
- `Ratings(Id, MediaId, UserId, Stars, Comment, Confirmed, Timestamp)`
- `UserLikedMedia(UserId, MediaId)` = Favoriten
- `UserLikedRatings(UserId, RatingId)` = Likes

**Begründung:**
- `UNIQUE(MediaId,UserId)` erzwingt „1 Rating pro User pro Media“ → Upsert möglich.
- Like/Favorite sind many-to-many Tabellen mit Composite PK.

---

## 4) REST API — Endpunkte & Regeln

### 4.1 Public Endpoints
- `POST /api/users/register`
  - Body: `{ "Username": "...", "Password": "..." }`
  - 201 created / 409 conflict (Username existiert) / 400 invalid

- `POST /api/users/login`
  - Body: `{ "Username": "...", "Password": "..." }`
  - 200 + `{ "Token": "..." }` / 401 invalid / 400 invalid

### 4.2 Auth (Token)
Alle Endpunkte außer Register/Login verlangen Bearer Token.  
Header akzeptiert:
- `Authorization: Bearer <token>`
- (tolerant) `Authentication: Bearer <token>` *(Spec-Beispiel nutzt „Authentication“)*

### 4.3 Media
- `GET /api/media`  
  Filter + Sort (Spec Use-Cases):
  - `title` (partial matching)
  - `genre`
  - `type` (`movie|series|game` oder `0..2`)
  - `year` oder `yearFrom/yearTo`
  - `ageRestriction` oder `age`
  - `minRating`
  - `sortBy=title|year|score`
  - `sortOrder=asc|desc`
  - optional `limit`, `offset`

- `GET /api/media/search?title=...`  
  Alias für „search by title (partial matching)“.

- `POST /api/media`  
  Erstellt Media, `CreatedByUserId` wird aus Token gesetzt.  
  201 + JSON des Media

- `GET /api/media/{id}`  
  Liefert:
  - Media Daten
  - `AvgScore`
  - Ratings (mit Moderation-Regel für Kommentare)

- `PUT /api/media/{id}`  
  **nur Creator** (sonst 403), 204 bei Erfolg.

- `DELETE /api/media/{id}`  
  **nur Creator** (sonst 403), 204 bei Erfolg.

### 4.4 Ratings
- `GET /api/media/{id}/ratings`  
  Liefert Ratings zu Media.  
  **Moderation-Regel laut Spec:**
  - Rating ist nicht sichtbar bis `Confirmed=true`
  - Ausnahme: Autor sieht eigenen Kommentar immer.

- `POST /api/media/{id}/ratings`  
  Upsert: 1 Rating pro User pro Media (UNIQUE Constraint).  
  Setzt `Confirmed=false` bei Update.  
  201 + Rating JSON

- `POST /api/ratings/{id}/confirm`  
  Nur Autor darf bestätigen. 204.

- `DELETE /api/ratings/{id}`  
  Nur Autor darf löschen. 204.

- `POST /api/ratings/{id}/like`  
  1 Like pro User pro Rating.  
  204 bei Erfolg / 409 wenn schon geliked.  
  (Optional-Regel im Code: own rating nicht likebar → 400)

### 4.5 Favorites / Profile / History
- `POST /api/media/{id}/favorite` → 204
- `DELETE /api/media/{id}/favorite` → 204

- `GET /api/users/{username}/favorites`  
  Nur eigenes Profil (sonst 403).

- `GET /api/users/{username}/ratings`  
  Nur eigenes Profil (sonst 403).

- `GET /api/users/{username}/profile`  
  Nur eigenes Profil (sonst 403).  
  Statistiken:
  - total ratings
  - avg score
  - favorite genre

- `PUT /api/users/{username}/profile`  
  Minimaler „Edit Profile“-Use-Case: Passwort ändern.

### 4.6 Leaderboard / Recommendations
- `GET /api/users/leaderboard`  
  Sortiert nach Anzahl Ratings pro User.

- `GET /api/users/{username}/recommendations?limit=10`  
  Empfehlungen basierend auf:
  - Genre similarity (stark gewichtet)
  - Type similarity
  - Age restriction similarity
  - global avg score als Qualitätssignal
  - Ausschluss: bereits geratet oder favorisiert

---

## 5) Unit Tests — Strategie & Abdeckung

**Ziel:** mind. 20 sinnvolle Tests (Checklist Must-Have).  
Unit-Tests fokussieren **Business-Logik**, nicht DB/HTTP:

### 5.1 Getestete Komponenten
1) **MediaQueryOptionsParser**
- parsing + validation von Query-Params
- Sort whitelist
- limit/offset validation
- type parsing (numeric + string)

2) **RecommendationEngine**
- Ausschlusslogik (rated/favorites)
- fallback ohne high-rated
- scoring Präferenzen (genre/type/age)
- limit, score gesetzt

### 5.2 Warum diese Logik?
- Parser + Recommendation sind zentrale „Core Business Logic“ und müssen stabil sein.
- DB und HttpListener wären in Unit-Tests unnötig schwergewichtig → dafür Integration Tests.

---

## 6) Integration Tests (curl)
Die Spec verlangt Postman Collection oder Curl Script.  
Es wird ein `curl_tests.sh` bereitgestellt, der:
- register/login
- create/list media
- rate + confirm
- like (mit zweitem User)
- favorite
- favorites/history
- leaderboard
- recommendations
- ownership check (403)

Run:
```bash
powershell -ExecutionPolicy Bypass -File .\curl_tests.ps1
```

---

## 7) SOLID Prinzipien (mit Projektbezug)

### 7.1 Single Responsibility Principle (SRP)
- `UserController`, `MediaController`, `RatingController` machen nur:
  - Routing/Parsing/Validation/HTTP Codes
- `DatabaseService` macht nur:
  - SQL + DB Access
- `RecommendationEngine` macht nur:
  - Recommendation Logik

→ klare Verantwortungen, leicht testbar und wartbar.

### 7.2 Open/Closed Principle (OCP)
- Erweiterungen an Filterparametern (`MediaQueryOptionsParser`) oder Sorting können ergänzt werden,
  ohne bestehende Call-Sites zu ändern (Optionen-Objekt bleibt stabil).
- Neue Endpunkte können via Router/Controller-Struktur ergänzt werden, ohne Server-Kern umzubauen.


---

## 8) Probleme & Lösungen (Lessons learned)

1) **Postgres “relation users does not exist”**
- Ursache: Quoted Identifiers und Case-Sensitivity (`"Users"` vs `users`).
- Lösung: konsistente Tabellennamen ohne Quotes und passende Queries.

2) **TargetFramework Konflikt (net8/net9) in Tests**
- Ursache: Test-Projekt auf anderem TargetFramework als Server/Database.
- Lösung: TargetFramework aller Projekte vereinheitlichen (Restore/Build stabil).

3) **Auth Header Name**
- Spec Beispiel nutzt `Authentication`, Standard ist `Authorization`.
- Lösung: Server akzeptiert beide Headernamen tolerant.

4) **UI / Demo**
- Frontend ist laut Spec nicht Teil des Projekts.
- Optional wurde eine Demo-UI gebaut, um API ohne Postman zu demonstrieren.
- Einfach im Browser `http://localhost:8080/` aufrufen.
  

---

## 9) Zeit-Tracking (Schätzung, mit Pausen etc.)
| Bereich | Aufwand (h) |
|--------|-------------|
| Projektsetup, Solution Struktur, Docker Postgres, init.sql | 4 |
| HttpListener Server + Router/Controller-Layout | 6 |
| Auth (Register/Login, Token Handling, Passwort Hashing) | 4 |
| Media CRUD inkl. Ownership Rules | 5 |
| Filtering/Sorting/Search (Query Parser + SQL) | 5 |
| Ratings (Upsert, Delete, Confirm Moderation) | 5 |
| Likes + Favorites + History Endpoints | 4 |
| Profile Stats + Leaderboard | 3 |
| Recommendation Engine | 4 |
| Unit Tests (20+), Refactoring für Testbarkeit | 5 |
| Integration Tests (curl script), End-to-End Checks | 2 |
| Dokumentation (README + protocol.md) | 2 |
| **Summe** | **49 h** |

---


