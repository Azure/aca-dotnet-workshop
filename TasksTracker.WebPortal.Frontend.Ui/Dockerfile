#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TasksTracker.WebPortal.Frontend.Ui/TasksTracker.WebPortal.Frontend.Ui.csproj", "TasksTracker.WebPortal.Frontend.Ui/"]
RUN dotnet restore "TasksTracker.WebPortal.Frontend.Ui/TasksTracker.WebPortal.Frontend.Ui.csproj"
COPY . .
WORKDIR "/src/TasksTracker.WebPortal.Frontend.Ui"
RUN dotnet build "TasksTracker.WebPortal.Frontend.Ui.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TasksTracker.WebPortal.Frontend.Ui.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TasksTracker.WebPortal.Frontend.Ui.dll"]