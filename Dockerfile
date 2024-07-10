FROM mcr.microsoft.com/dotnet/sdk:7.0 AS builder
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

COPY ["AlertApi/WebApi.csproj", "AlertApi/"]
COPY ["Common/Common.csproj", "Common/"]
COPY ["ConsoleApp/ConsoleApp.csproj", "ConsoleApp/"]
COPY ["SubscriptionApi/SubApi.csproj", "SubscriptionApi/"]

RUN dotnet restore "AlertApi/WebApi.csproj"
RUN dotnet restore "Common/Common.csproj"
RUN dotnet restore "ConsoleApp/ConsoleApp.csproj"
RUN dotnet restore "SubscriptionApi/SubApi.csproj"

COPY . .
WORKDIR "/src/."
RUN dotnet build "AlertApi/WebApi.csproj" -c Release -o /app/build
RUN dotnet build "Common/Common.csproj" -c Release -o /app/build
RUN dotnet build "ConsoleApp/ConsoleApp.csproj" -c Release -o /app/build
RUN dotnet build "SubscriptionApi/SubApi.csproj" -c Release -o /app/build

# FROM build AS publish
# RUN dotnet publish "AlertApi/WebApi.csproj" -c Release -o /app/publish/AlertApi /p:UseAppHost=false
# RUN dotnet publish "Common/Common.csproj" -c Release -o /app/publish/Common /p:UseAppHost=false
RUN dotnet publish "ConsoleApp/ConsoleApp.csproj" -c Release -o /app/publish/ConsoleApp /p:UseAppHost=false
# RUN dotnet publish "SubscriptionApi/SubApi.csproj" -c Release -o /app/publish/SubscriptionApi /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
WORKDIR /app
# COPY --from=publish /app/publish/AlertApi .
# COPY --from=publish /app/publish/Common .
# COPY --from=publish /app/publish/ConsoleApp .
# COPY --from=publish /app/publish/SubscriptionApi .
ENTRYPOINT ["dotnet", "ConsoleApp.dll"]