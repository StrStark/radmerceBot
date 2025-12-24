FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY radmerceBot.sln .
COPY src/radmerceBot.Api/radmerceBot.Api.csproj src/radmerceBot.Api/
COPY src/radmerceBot.Core/radmerceBot.Core.csproj src/radmerceBot.Core/
COPY src/radmerceBot.Infrastructure/radmerceBot.Infrastructure.csproj src/radmerceBot.Infrastructure/

RUN dotnet restore

COPY src/ .
RUN dotnet publish src/radmerceBot.Api -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "radmerceBot.Api.dll"]
