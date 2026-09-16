FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["ArtemisBank.WebApp/ArtemisBank.WebApp.csproj", "ArtemisBank.WebApp/"]
COPY ["ArtemisBank.Core.Application/ArtemisBank.Core.Application.csproj", "ArtemisBank.Core.Application/"]
COPY ["ArtemisBank.Core.Domain/ArtemisBank.Core.Domain.csproj", "ArtemisBank.Core.Domain/"]
COPY ["ArtemisBank.Infrastructure.Identity/ArtemisBank.Infrastructure.Identity.csproj", "ArtemisBank.Infrastructure.Identity/"]
COPY ["ArtemisBank.Infrastructure.Persistence/ArtemisBank.Infrastructure.Persistence.csproj", "ArtemisBank.Infrastructure.Persistence/"]
COPY ["ArtemisBank.Infrastructure.Shared/ArtemisBank.Infrastructure.Shared.csproj", "ArtemisBank.Infrastructure.Shared/"]

RUN dotnet restore "ArtemisBank.WebApp/ArtemisBank.WebApp.csproj"

COPY . .

WORKDIR "/src/ArtemisBank.WebApp"
RUN dotnet build "ArtemisBank.WebApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ArtemisBank.WebApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ArtemisBank.WebApp.dll"]