FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY Directory.Build.props ./
#COPY LoopMeet.slnx ./
COPY LoopMeet.Core/LoopMeet.Core.csproj LoopMeet.Core/
COPY LoopMeet.Infrastructure/LoopMeet.Infrastructure.csproj LoopMeet.Infrastructure/
COPY LoopMeet.Api/LoopMeet.Api.csproj LoopMeet.Api/
RUN dotnet restore LoopMeet.Api/LoopMeet.Api.csproj

COPY LoopMeet.Core LoopMeet.Core
COPY LoopMeet.Infrastructure LoopMeet.Infrastructure
COPY LoopMeet.Api LoopMeet.Api
RUN dotnet publish LoopMeet.Api/LoopMeet.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:5001
EXPOSE 5001
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "LoopMeet.Api.dll"]
