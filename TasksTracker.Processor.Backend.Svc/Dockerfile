#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TasksTracker.Processor.Backend.Svc/TasksTracker.Processor.Backend.Svc.csproj", "TasksTracker.Processor.Backend.Svc/"]
RUN dotnet restore "TasksTracker.Processor.Backend.Svc/TasksTracker.Processor.Backend.Svc.csproj"
COPY . .
WORKDIR "/src/TasksTracker.Processor.Backend.Svc"
RUN dotnet build "TasksTracker.Processor.Backend.Svc.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TasksTracker.Processor.Backend.Svc.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TasksTracker.Processor.Backend.Svc.dll"]