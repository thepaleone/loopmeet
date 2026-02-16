# Research: Auth & Groups MVP

**Date**: 2026-02-16

## Decision 1: App + API stack

- **Decision**: Use .NET 10 with .NET MAUI for iOS/Android and ASP.NET Core Web API for backend services.
- **Rationale**: Matches requested stack, supports shared C# models, and reduces implementation overhead for a small MVP.
- **Alternatives considered**: React Native + Node API; native iOS/Android + separate backend.

## Decision 2: Auth provider and token validation

- **Decision**: Use Supabase Auth for email and Google sign-in; API validates Supabase-issued JWTs for authorized endpoints.
- **Rationale**: Keeps auth consistent with production persistence and reduces custom auth implementation.
- **Alternatives considered**: Custom ASP.NET Identity; third-party auth service unrelated to Supabase.

## Decision 3: Data persistence strategy

- **Decision**: Use EF Core with Npgsql to target local Postgres for testing/dev and Supabase Postgres for production.
- **Rationale**: Aligns with requested Postgres testing environment and production Supabase persistence.
- **Alternatives considered**: Supabase REST API only; direct SQL access without ORM.

## Decision 4: NuGet packages

- **Decision**: Use CommunityToolkit.Mvvm and CommunityToolkit.Maui for MAUI patterns; Refit + Polly for API access and resilience; Serilog for API logging; FluentValidation for API input validation; FluentAssertions for tests.
- **Rationale**: These packages are widely used and reduce boilerplate without introducing heavy abstractions.
- **Alternatives considered**: Hand-rolled MVVM and HTTP clients; built-in logging only.

## Decision 5: Splash screen approach

- **Decision**: Configure MAUI splash screen with a text-only branding asset that reads "LoopMeet" for both iOS and Android.
- **Rationale**: Meets the immediate branding requirement while keeping assets minimal for the MVP.
- **Alternatives considered**: Full logo and animated splash; platform-specific native splash implementations.
