FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY AryTickets/AryTickets.csproj AryTickets/
RUN dotnet restore AryTickets/AryTickets.csproj
COPY AryTickets/ AryTickets/
WORKDIR /src/AryTickets
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AryTickets.dll"]
