FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ASNParser/ASNParser.csproj", "ASNParser/"]
RUN dotnet restore "ASNParser/ASNParser.csproj"
COPY . .
WORKDIR "/src/ASNParser"
RUN dotnet build "ASNParser.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ASNParser.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ASNParser.dll"]