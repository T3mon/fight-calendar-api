FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY WhoFights.sln .
COPY src/WhoFights.Web/WhoFights.Web.csproj src/WhoFights.Web/
COPY src/WhoFights.Data/WhoFights.Data.csproj src/WhoFights.Data/
RUN dotnet restore src/WhoFights.Web/WhoFights.Web.csproj

COPY src/ src/
RUN dotnet publish src/WhoFights.Web/WhoFights.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Npgsql probes for GSSAPI/Kerberos support on every connection; the slim
# runtime image doesn't ship it, which prints a harmless but noisy error to
# the logs on every connection attempt otherwise.
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "WhoFights.Web.dll"]
