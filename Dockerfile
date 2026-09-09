FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY FightCalendar.sln .
COPY src/FightCalendar.Web/FightCalendar.Web.csproj src/FightCalendar.Web/
RUN dotnet restore src/FightCalendar.Web/FightCalendar.Web.csproj

COPY src/ src/
RUN dotnet publish src/FightCalendar.Web/FightCalendar.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "FightCalendar.Web.dll"]
