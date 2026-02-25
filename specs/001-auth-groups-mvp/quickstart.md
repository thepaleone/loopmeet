# Quickstart: Auth & Groups MVP

**Date**: 2026-02-16

## Prerequisites

- .NET 10 SDK
- Xcode (for iOS builds)
- Android SDK (for Android builds)
- Postgres 15+ for local testing

## Environment Variables

Set these for local development:

- `LOOPMEET_ENV=Development`
- `LOOPMEET_DB_CONNECTION=Host=localhost;Port=5432;Database=loopmeet_dev;Username=postgres;Password=postgres`
- `LOOPMEET_SUPABASE_URL=https://<project>.supabase.co`
- `LOOPMEET_SUPABASE_ANON_KEY=<anon_key>`
- `LOOPMEET_SUPABASE_JWT_ISSUER=https://<project>.supabase.co/auth/v1`

## Run the API

```bash
dotnet restore

dotnet run --project src/LoopMeet.Api
```

## Run the MAUI App

```bash
dotnet restore

dotnet build src/LoopMeet.App
```

Use your IDE or `dotnet build` targets to deploy to iOS/Android simulators.

## Test the API

```bash
dotnet test tests/LoopMeet.Api.Tests
```

## Notes

- Local tests use Postgres. Production connects to Supabase Postgres using the same schema.
- Supabase Auth should be configured with email and Google sign-in before testing login flows.
