FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

USER app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["TasksTracker.TasksManager.Backend.Api/TasksTracker.TasksManager.Backend.Api.csproj", "TasksTracker.TasksManager.Backend.Api/"]
RUN dotnet restore "TasksTracker.TasksManager.Backend.Api/TasksTracker.TasksManager.Backend.Api.csproj"
COPY . .
WORKDIR "/src/TasksTracker.TasksManager.Backend.Api"
RUN dotnet build "TasksTracker.TasksManager.Backend.Api.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "TasksTracker.TasksManager.Backend.Api.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TasksTracker.TasksManager.Backend.Api.dll"]
