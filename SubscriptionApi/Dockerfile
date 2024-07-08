# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS builder
WORKDIR /app

COPY *.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish --output /app/ --configuration Release --no-restore
RUN sed -n 's:.*<AssemblyName>\(.*\)</AssemblyName>.*:\1:p' *.csproj > __assemblyname
RUN if [ ! -s __assemblyname ]; then filename=$(ls *.csproj); echo ${filename%.*} > __assemblyname; fi

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=builder /app .

ENV PORT 8002
EXPOSE 8002

ENTRYPOINT dotnet $(cat /app/__assemblyname).dll --urls "http://*:8002"
