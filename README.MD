# Hackatons Vibe Coders

A web application project created during the **AI Ruse Hackathon** as part of a 48-hour competition challenge.

> Status: Work in progress  
> This project is not finished yet. The repository currently contains multiple branches and active development work. Features, structure, UI, and implementation details may change.

---

## About the Project

**Hackatons Vibe Coders** is an event-focused web application built as a hackathon project.  
The goal of the project is to provide a platform where events, users, and event-related functionality can be managed in one place.

The current version is still under development. Some parts of the project were created quickly during the hackathon period, and the team is now continuing work on improving the structure, UI, functionality, and overall code quality.

---

## Current Development Status

This repository is currently in a mid-development state.

What this means:

- The project is not production-ready.
- Some features may be incomplete.
- Some branches may contain experimental or temporary work.
- The UI and backend structure may still change.
- Documentation will be updated as the project becomes more stable.

Active work is being done through branches, including UI and feature-related updates.

---

## Tech Stack

Current visible project stack:

- **C#**
- **ASP.NET Core**
- **Entity Framework Core**
- **Razor Views / MVC structure**
- **HTML**
- **CSS**
- **PostgreSQL** for local development and hosting

The repository currently includes typical ASP.NET project folders such as:

- `Controllers`
- `Models`
- `Views`
- `ViewModels`
- `Services`
- `Data`
- `Migrations`
- `wwwroot`

---

## Local Development Setup

Use the `postgres-migration` branch when working with the PostgreSQL version:

```powershell
git checkout postgres-migration
dotnet restore
docker compose up -d postgres
dotnet ef database update
dotnet run
```

Local database connection:

```text
Host=localhost
Port=5432
Database=evento_dev
Username=evento
Password=evento_dev_password
```

The local PostgreSQL database runs in Docker and stores its data in the `evento-postgres-data` volume, so the database state is preserved between container restarts.

Local uploads use the local filesystem by default:

```text
MEDIA_STORAGE=Local
```

With `MEDIA_STORAGE=Local`, uploaded event images, post images, profile images and stories are saved under:

```text
wwwroot/uploads
```

This is the correct setup for local development. Do not commit files from `wwwroot/uploads` unless they are intentional demo assets.

---

## Team Workflow Notes

### First local run for teammates

1. Clone the repo and switch to the PostgreSQL branch:

```powershell
git checkout postgres-migration
```

2. Copy `.env.example` to `.env` and fill only local/development values. Never commit `.env`.

3. Start PostgreSQL and apply migrations:

```powershell
dotnet restore
docker compose up -d postgres
dotnet ef database update
dotnet run
```

The app also runs `Database.Migrate()` on startup, but running `dotnet ef database update` makes migration problems easier to see before the web app starts.

### Before pushing

Run:

```powershell
dotnet build --no-restore
dotnet test EventsApp.Tests\EventsApp.Tests.csproj --no-restore
```

Do not commit:

- `.env`
- files from `wwwroot/uploads`
- local database volumes
- IDE/system files

### Current organizer event flow

Organizers use:

```text
Organizer dashboard -> Events
```

That page lists only their own events and gives quick actions for:

- event preview
- edit
- ticket management

If an event is not approved yet, organizer edits are applied directly to the draft.

If an event is already approved/published, organizer edits do not replace the live event immediately. The app creates an `EventChangeRequest` with the proposed changes. The old public version stays visible until an admin approves the pending changes.

Admin review flow:

```text
Admin -> Events -> Pending only -> Changes -> Approve changes
```

When the admin approves pending changes, the snapshot is applied to the event and the change request becomes approved.

### Validator flow

Validators are scoped to a public organizer page, not to individual events.

Organizer flow:

```text
Organizer dashboard -> Validators
```

Rules:

- maximum 3 active validators per organizer
- validators are service user accounts, usually logged in on door/scan phones
- a validator can scan tickets for every event published under the selected public page
- validators cannot manage the organizer dashboard or see private organizer data

### Media storage

Local development uses:

```text
MEDIA_STORAGE=Local
```

Production uses an S3-compatible Railway Storage Bucket:

```text
MEDIA_STORAGE=S3
```

The database stores media URLs/keys only. Image/video binary data is stored either in local `wwwroot/uploads` during development or in the production bucket on Railway.

---

## Production Setup on Railway

The recommended Railway production shape is:

- Railway Web Service for the ASP.NET Core app
- Railway PostgreSQL for the database
- Railway Storage Bucket for images/videos/uploads
- Railway Variables for secrets and production configuration

For production/Railway, set at least these environment variables:

```text
DATABASE_URL=<provided by Railway Postgres>
ADMIN_EMAIL=<your admin email>
ADMIN_PASSWORD=<strong admin password>
OPENAI_API_KEY=<optional, for AI features>
GOOGLE_MAPS_KEY=<optional, for maps>
EMAIL_ENABLED=false
SMTP_HOST=<optional, for account emails>
SMTP_PORT=587
SMTP_USERNAME=<optional>
SMTP_PASSWORD=<optional>
SMTP_FROM_EMAIL=<optional>
SMTP_FROM_NAME=Evento
SMTP_ENABLE_SSL=true
```

`ADMIN_PASSWORD` must satisfy the production password rules: at least 10 characters, uppercase, lowercase, number, and symbol.

### Account Emails and Password Reset

ASP.NET Identity provides the account email pages, including:

```text
/Identity/Account/ForgotPassword
/Identity/Account/ResetPassword
```

Emails are sent through SMTP when email is enabled. For local development you can keep:

```text
EMAIL_ENABLED=false
```

For Railway or production set:

```text
EMAIL_ENABLED=true
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=<mailbox or SMTP username>
SMTP_PASSWORD=<SMTP app password or provider password>
SMTP_FROM_EMAIL=<from address, optional if same as SMTP_USERNAME>
SMTP_FROM_NAME=Evento
SMTP_ENABLE_SSL=true
```

The app also accepts Railway-style aliases:

```text
SMTP_USER=<same as SMTP_USERNAME>
SMTP_PASS=<same as SMTP_PASSWORD>
```

For a real domain email, use a dedicated mailbox or transactional email provider and set the same SMTP variables in Railway. Do not commit SMTP passwords or app passwords to git.

### Railway Storage Bucket

Use one Railway Storage Bucket for production uploads. Suggested bucket display name:

```text
evento-media
```

Railway will create the real API bucket name automatically, for example `evento-media-xxxxxx`. Use Railway's `BUCKET` variable for the API bucket name, not the display name.

In the app service variables set:

```text
MEDIA_STORAGE=S3
S3_ENDPOINT=<Railway bucket ENDPOINT, usually https://storage.railway.app>
S3_BUCKET=<Railway bucket BUCKET>
S3_ACCESS_KEY_ID=<Railway bucket ACCESS_KEY_ID>
S3_SECRET_ACCESS_KEY=<Railway bucket SECRET_ACCESS_KEY>
S3_REGION=<Railway bucket REGION, usually auto>
S3_FORCE_PATH_STYLE=false
S3_KEY_PREFIX=uploads
S3_SIGNED_URL_MINUTES=30
```

Railway Storage Buckets are private and S3-compatible. The app stores uploaded files in the bucket and saves `/media/...` URLs in the database. When the browser requests `/media/...`, the app redirects to a temporary presigned Railway Bucket URL.

The default CSP already allows `https://storage.railway.app` for images and videos. If Railway changes the bucket endpoint or you move to Cloudflare R2/S3 later, add that public endpoint to `Security:ContentSecurityPolicy` under `img-src` and `media-src`.

Railway's bucket variables are:

```text
BUCKET
ACCESS_KEY_ID
SECRET_ACCESS_KEY
REGION
ENDPOINT
```

The app supports both Railway's native names and the clearer `S3_*` names above. If you use Railway variable references, map the bucket values into the `S3_*` names so it is obvious what each variable does.

If a Railway bucket was created before Railway's virtual-hosted URL style change, the bucket credentials page may say that path-style URLs are required. In that case set:

```text
S3_FORCE_PATH_STYLE=true
```

Railway docs:

- Storage Buckets: https://docs.railway.com/guides/storage-buckets
- Uploading and serving files: https://docs.railway.com/storage-buckets/uploading-serving

### Local vs Production Media

Local development:

```text
MEDIA_STORAGE=Local
```

Production:

```text
MEDIA_STORAGE=S3
```

The database stores only file URLs/keys, not the binary image/video data. This keeps PostgreSQL small and fast. Local uploads and production bucket uploads are intentionally separate.

Useful verification commands:

```powershell
dotnet build --no-restore
dotnet test EventsApp.Tests\EventsApp.Tests.csproj --no-restore
```

---

## Main Features Planned / In Progress

The project may include or work toward:

- User registration and login
- Event creation and management
- Event browsing
- Admin or role-based functionality
- UI improvements
- Database-backed event data
- Better project structure and documentation

More specific feature documentation will be added once the implementation becomes stable.

---

## Repository Structure

```text
Hackatons_Vibe_Coders/
├── Controllers/
├── Models/
├── Views/
├── ViewModels/
├── Services/
├── Data/
├── Migrations/
├── wwwroot/
├── Program.cs
├── EventsApp.csproj
├── EventsApp.sln
└── README.MD
