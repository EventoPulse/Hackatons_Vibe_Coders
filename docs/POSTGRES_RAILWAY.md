# Postgres and Railway

## Local database

Start the local Postgres database:

```powershell
docker compose up -d postgres
```

Apply migrations:

```powershell
dotnet ef database update
```

Run the app:

```powershell
dotnet run
```

The local database uses the Docker volume `evento-postgres-data`, so data stays available across container restarts.

In development, `SeedDemoData` is enabled by default. The app always seeds roles and the admin account; demo events/users are added only when `SeedDemoData=true` and the environment is `Development`.

## Railway

1. Create a Railway project.
2. Add a PostgreSQL service.
3. Add the web app from GitHub and point it at this repository/branch.
4. Attach the PostgreSQL service to the web app so Railway exposes `DATABASE_URL`.
5. Add the app secrets as environment variables: `ADMIN_EMAIL`, `ADMIN_PASSWORD`, `OPENAI_API_KEY`, `GOOGLE_MAPS_KEY`, and later email/payment keys.
6. Deploy with the included `Dockerfile`.

The app reads `DATABASE_URL` first. If it is missing, it falls back to `ConnectionStrings:DefaultConnection`.

Production starts clean by default: roles and the admin account are seeded, but demo data is disabled unless you explicitly set `ASPNETCORE_ENVIRONMENT=Development` and `SeedDemoData=true`.

`ADMIN_PASSWORD` is required outside Development and must satisfy the production password rules: at least 10 characters, uppercase, lowercase, number, and symbol.
