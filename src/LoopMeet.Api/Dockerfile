FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY LoopMeet.slnx ./
COPY src/LoopMeet.Core/LoopMeet.Core.csproj src/LoopMeet.Core/
COPY src/LoopMeet.Infrastructure/LoopMeet.Infrastructure.csproj src/LoopMeet.Infrastructure/
COPY src/LoopMeet.Api/LoopMeet.Api.csproj src/LoopMeet.Api/
RUN dotnet restore src/LoopMeet.Api/LoopMeet.Api.csproj

COPY src/LoopMeet.Core src/LoopMeet.Core
COPY src/LoopMeet.Infrastructure src/LoopMeet.Infrastructure
COPY src/LoopMeet.Api src/LoopMeet.Api
RUN dotnet publish src/LoopMeet.Api/LoopMeet.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:5001
EXPOSE 5001
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "LoopMeet.Api.dll"]
