FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

USER app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["TasksTracker.Processor.Backend.Svc/TasksTracker.Processor.Backend.Svc.csproj", "TasksTracker.Processor.Backend.Svc/"]
RUN dotnet restore "TasksTracker.Processor.Backend.Svc/TasksTracker.Processor.Backend.Svc.csproj"
COPY . .
WORKDIR "/src/TasksTracker.Processor.Backend.Svc"
RUN dotnet build "TasksTracker.Processor.Backend.Svc.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "TasksTracker.Processor.Backend.Svc.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TasksTracker.Processor.Backend.Svc.dll"]
