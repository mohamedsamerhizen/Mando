FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Mando.Api/Mando.Api.csproj", "Mando.Api/"]
RUN dotnet restore "Mando.Api/Mando.Api.csproj"

COPY . .
WORKDIR /src/Mando.Api
RUN dotnet publish "Mando.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Mando.Api.dll"]
