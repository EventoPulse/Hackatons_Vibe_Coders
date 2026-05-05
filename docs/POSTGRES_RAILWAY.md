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

## Railway

1. Create a Railway project.
2. Add a PostgreSQL service.
3. Add the web app from GitHub and point it at this repository/branch.
4. Attach the PostgreSQL service to the web app so Railway exposes `DATABASE_URL`.
5. Add the app secrets as environment variables: `OPENAI_API_KEY`, `GOOGLE_MAPS_KEY`, and later email/payment keys.
6. Deploy with the included `Dockerfile`.

The app reads `DATABASE_URL` first. If it is missing, it falls back to `ConnectionStrings:DefaultConnection`.
