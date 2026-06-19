# منصة التعارف للزواج · Marriage Matchmaking Platform

A friendly, **Arabic-first (RTL)** marriage matchmaking web application for the Muslim community in
Egypt. Applicants (عريس / عروسة) register detailed profiles; an **admin** reviews system-generated
**Top-5 compatibility matches** and makes the final decision. Users only ever see their own status —
never the scores or other profiles.

Built with **ASP.NET Core 8 MVC + Razor**, **Entity Framework Core**, **SQL Server**, and
**ASP.NET Core Identity** (role-based: `User` / `Admin`).

## Features

- **Gender-branched onboarding** — landing page routes to the عريس or عروسة registration form.
- **Rich profiles** — personal, family, religious commitment, and "requirements in the other party"
  data, with **multi-select preferences** (education, commitment, dress code, marital status,
  residence cities) stored as `[Flags]` bitmasks.
- **Matching engine** — weighted, range-based (with linear falloff), set-membership scoring, **soft
  hard-constraints** (heavy penalty instead of exclusion), and **mutual** two-way scoring. Returns the
  Top-5 with a per-criterion breakdown. **Admin-only.**
- **Secure photos** — encrypted at rest (ASP.NET Data Protection), never served as static files.
  Admin can view + download; a matched groom can *view* the bride's photos only **after an admin
  approves that match**. Every access is audited.
- **Notifications** — pluggable channels (in-app + email out of the box; WhatsApp/SMS via Twilio when
  configured).
- **Warm, fun, fully responsive RTL UI** — "وِصال" blush-and-gold theme, mobile-optimized.

## Solution structure

```
MarriageApp.sln
├─ src/MarriageApp.Core            domain entities, enums, matching contracts/DTOs
├─ src/MarriageApp.Infrastructure  EF Core DbContext, MatchingService, notifications, photo encryption
├─ src/MarriageApp.Web             controllers, RTL Razor views, Identity wiring
└─ tests/MarriageApp.Tests         xUnit matching-algorithm tests
```

## Getting started

**Prerequisites:** .NET 8 SDK, SQL Server (LocalDB is fine on Windows).

```bash
# from the repo root
dotnet build
dotnet run --project src/MarriageApp.Web
```

On first run the app applies EF migrations and seeds the roles + a default admin. In `Development`
it also seeds sample grooms/brides so the match screen has data.

- App: `http://localhost:5279`
- **Default admin:** `admin@marriageapp.local` / `Admin#12345`
- **Sample test users** (Development only): e.g. `fatma.bride@test.local` — password `Test#12345`

> Change the seeded credentials in [`appsettings.json`](src/MarriageApp.Web/appsettings.json)
> (`Seed:Admin`) before any real deployment, and supply a production SQL Server connection string.

## Configuration

`src/MarriageApp.Web/appsettings.json`:

- `ConnectionStrings:DefaultConnection` — SQL Server connection.
- `Matching` — tunable scoring weights, falloff, and the hard-constraint penalty factor.
- `Notifications:Email` / `Notifications:Twilio` — enable + configure external channels.
- `PhotoStorage:StorageRoot` — where encrypted photo blobs are stored.

## Tests

```bash
dotnet test
```

## License

Private project.
