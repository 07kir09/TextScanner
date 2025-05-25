FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["AntiPlagiarism.FileAnalysisService/AntiPlagiarism.FileAnalysisService.csproj", "AntiPlagiarism.FileAnalysisService/"]
COPY ["AntiPlagiarism.Common/AntiPlagiarism.Common.csproj", "AntiPlagiarism.Common/"]
RUN dotnet restore "AntiPlagiarism.FileAnalysisService/AntiPlagiarism.FileAnalysisService.csproj"
COPY . .
WORKDIR "/src/AntiPlagiarism.FileAnalysisService"
RUN dotnet build "AntiPlagiarism.FileAnalysisService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AntiPlagiarism.FileAnalysisService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Создаем каталог для хранения облаков слов
RUN mkdir -p /app/WordCloudStorage
VOLUME /app/WordCloudStorage
ENTRYPOINT ["dotnet", "AntiPlagiarism.FileAnalysisService.dll"]