# Step 1: Base Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Step 2: Build Image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["HR_system.csproj", "./"]
RUN dotnet restore "HR_system.csproj"
COPY . .
RUN dotnet build "HR_system.csproj" -c Release -o /app/build

# Step 3: Publish App
FROM build AS publish
RUN dotnet publish "HR_system.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Step 4: Final Stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "HR_system.dll"]