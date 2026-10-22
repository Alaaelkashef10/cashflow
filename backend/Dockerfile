FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/CashFlow.Core/CashFlow.Core.csproj src/CashFlow.Core/
COPY src/CashFlow.Infrastructure/CashFlow.Infrastructure.csproj src/CashFlow.Infrastructure/
COPY src/CashFlow.Api/CashFlow.Api.csproj src/CashFlow.Api/
RUN dotnet restore src/CashFlow.Api/CashFlow.Api.csproj
COPY src/ src/
RUN dotnet publish src/CashFlow.Api/CashFlow.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "CashFlow.Api.dll"]