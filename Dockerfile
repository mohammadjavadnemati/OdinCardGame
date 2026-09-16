FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY OdinGame/*.csproj OdinGame/
RUN dotnet restore OdinGame/OdinGame.csproj
COPY OdinGame/ OdinGame/
RUN dotnet publish OdinGame/OdinGame.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "OdinGame.dll"]