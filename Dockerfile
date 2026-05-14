FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/WeeklyPlanning.API/WeeklyPlanning.API.csproj", "WeeklyPlanning.API/"]
COPY ["src/WeeklyPlanning.Application/WeeklyPlanning.Application.csproj", "WeeklyPlanning.Application/"]
COPY ["src/WeeklyPlanning.Domain/WeeklyPlanning.Domain.csproj", "WeeklyPlanning.Domain/"]
COPY ["src/WeeklyPlanning.Infrastructure/WeeklyPlanning.Infrastructure.csproj", "WeeklyPlanning.Infrastructure/"]

RUN dotnet restore "WeeklyPlanning.API/WeeklyPlanning.API.csproj"

COPY ["src/WeeklyPlanning.API/", "WeeklyPlanning.API/"]
COPY ["src/WeeklyPlanning.Application/", "WeeklyPlanning.Application/"]
COPY ["src/WeeklyPlanning.Domain/", "WeeklyPlanning.Domain/"]
COPY ["src/WeeklyPlanning.Infrastructure/", "WeeklyPlanning.Infrastructure/"]

RUN dotnet publish "WeeklyPlanning.API/WeeklyPlanning.API.csproj" -c Release -o /app/out

FROM base AS final
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "WeeklyPlanning.API.dll"]
