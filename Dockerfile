FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["backend/RentalSaaS.Api/RentalSaaS.Api.csproj", "backend/RentalSaaS.Api/"]
RUN dotnet restore "backend/RentalSaaS.Api/RentalSaaS.Api.csproj"

COPY . .
WORKDIR /src/backend/RentalSaaS.Api
RUN dotnet publish "RentalSaaS.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "RentalSaaS.Api.dll"]
