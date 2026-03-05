FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY DragonEnvelopes.sln ./
COPY src/DragonEnvelopes.Domain/DragonEnvelopes.Domain.csproj src/DragonEnvelopes.Domain/
COPY src/DragonEnvelopes.Application/DragonEnvelopes.Application.csproj src/DragonEnvelopes.Application/
COPY src/DragonEnvelopes.Infrastructure/DragonEnvelopes.Infrastructure.csproj src/DragonEnvelopes.Infrastructure/
COPY src/DragonEnvelopes.Contracts/DragonEnvelopes.Contracts.csproj src/DragonEnvelopes.Contracts/
COPY src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj src/DragonEnvelopes.Api/

RUN dotnet restore src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj

COPY . .
RUN dotnet publish src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "DragonEnvelopes.Api.dll"]


