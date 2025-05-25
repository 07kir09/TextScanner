FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["AntiPlagiarism.FileStoringService/AntiPlagiarism.FileStoringService.csproj", "AntiPlagiarism.FileStoringService/"]
COPY ["AntiPlagiarism.Common/AntiPlagiarism.Common.csproj", "AntiPlagiarism.Common/"]
RUN dotnet restore "AntiPlagiarism.FileStoringService/AntiPlagiarism.FileStoringService.csproj"
COPY . .
WORKDIR "/src/AntiPlagiarism.FileStoringService"
RUN dotnet build "AntiPlagiarism.FileStoringService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AntiPlagiarism.FileStoringService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Создаем каталог для хранения файлов
RUN mkdir -p /app/FileStorage
VOLUME /app/FileStorage
ENTRYPOINT ["dotnet", "AntiPlagiarism.FileStoringService.dll"]