# Hackatons Vibe Coders

Event and social platform built during the AI Ruse Hackathon and currently developed on the `social-design` branch.

The app is an ASP.NET Core MVC project with authentication, events, tickets, organizer tools, social content, messaging, and AI-assisted features.

## Current Stack

- .NET 8
- ASP.NET Core MVC + Razor Views
- Entity Framework Core
- ASP.NET Core Identity
- SQL Server / LocalDB
- Optional OpenAI integration
- Optional Google Maps client key

## What Works Right Now

The current codebase includes flows for:

- registration, login, roles, and seeded admin account
- event browsing and event details
- organizer profiles and organizer management screens
- posts, stories, comments, likes, and social feed UI
- direct messages and shared cards inside conversations
- ticket creation, ticket management, attendee data, and user tickets
- venue layout editing, including AI-assisted layout generation
- AI-assisted search when an API key is configured
- Bulgarian and English localization

## Local Run

### Prerequisites

- .NET 8 SDK
- SQL Server LocalDB on Windows

If you are not using LocalDB, replace `ConnectionStrings:DefaultConnection` with a normal SQL Server connection string in `appsettings.json`, `appsettings.Development.json`, environment variables, or user-secrets.

### Optional `.env`

The app loads a local `.env` file automatically on startup. Useful keys:

```env
OPENAI_API_KEY=your_openai_key
GOOGLE_MAPS_API_KEY=your_google_maps_key
```

`AI_API_KEY` also works as an alias for `OPENAI_API_KEY`.

### Start

```bash
dotnet restore
dotnet build
dotnet run
```

Default local URLs from the launch profile:

- `https://localhost:7180`
- `http://localhost:5180`

On startup the app automatically:

- applies pending EF Core migrations
- seeds roles
- seeds the admin account
- seeds demo users, organizers, events, posts, tickets, and activity data

## Demo Accounts

Admin:

- email: `admin@grooveon.com`
- password: `admin`

Demo users and demo organizers:

- password: `Demo123`
- example emails: `ivan.dimitrov@demo.bg`, `sofia.sound@demo.bg`, `beat.rooms@demo.bg`

The admin password is hardcoded for development convenience and should be changed before any real deployment.

## Build Status

Verified locally on May 7, 2026:

- `dotnet build` succeeds
- app startup reaches database migration and seeding successfully

## Important Note For Railway

The project is currently wired to SQL Server in code:

- `Program.cs` uses `UseSqlServer(...)`
- `Data/ApplicationDbContextFactory.cs` uses `UseSqlServer(...)`
- default config points to LocalDB

That means a Railway Postgres service will not work with the current code as-is.

If you want to deploy to Railway next, you have two realistic options:

1. Keep the current code and point it to a SQL Server instance instead of Postgres.
2. Migrate the app from `Microsoft.EntityFrameworkCore.SqlServer` to PostgreSQL/Npgsql.

## Project Structure

```text
Hackatons_Vibe_Coders/
|-- Areas/
|-- Common/
|-- Configuration/
|-- Controllers/
|-- Data/
|-- Infrastructure/
|-- Migrations/
|-- Models/
|-- Services/
|-- ViewModels/
|-- Views/
|-- wwwroot/
|-- Program.cs
|-- EventsApp.csproj
|-- EventsApp.sln
|-- README.MD
```
