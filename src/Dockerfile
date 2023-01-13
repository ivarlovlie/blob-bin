FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["BlobBin.csproj", "."]
RUN dotnet restore "BlobBin.csproj"
COPY . .
WORKDIR "/src/BlobBin"
RUN dotnet build "BlobBin.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BlobBin.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BlobBin.dll"]
