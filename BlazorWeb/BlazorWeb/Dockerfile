# Сборка
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["BlazorWeb/BlazorWeb/BlazorWeb.csproj", "BlazorWeb/BlazorWeb/"]
COPY ["BlazorWeb/BlazorWeb.Client/BlazorWeb.Client.csproj", "BlazorWeb/BlazorWeb.Client/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Core/Core.csproj", "Core/"]
RUN dotnet restore "BlazorWeb/BlazorWeb/BlazorWeb.csproj"

COPY . .
WORKDIR /src/BlazorWeb/BlazorWeb
RUN dotnet publish "BlazorWeb.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "BlazorWeb.dll"]