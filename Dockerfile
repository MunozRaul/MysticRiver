FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore src/MysticRiver.HttpApi/MysticRiver.HttpApi.csproj
RUN dotnet publish src/MysticRiver.HttpApi/MysticRiver.HttpApi.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app .

ENTRYPOINT ["dotnet", "MysticRiver.HttpApi.dll"]
