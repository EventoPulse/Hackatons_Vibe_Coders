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

For production/Railway, set at least these environment variables:

```text
DATABASE_URL=<provided by Railway Postgres>
ADMIN_EMAIL=<your admin email>
ADMIN_PASSWORD=<strong admin password>
OPENAI_API_KEY=<optional, for AI features>
GOOGLE_MAPS_KEY=<optional, for maps>
```

`ADMIN_PASSWORD` must satisfy the production password rules: at least 10 characters, uppercase, lowercase, number, and symbol.

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
