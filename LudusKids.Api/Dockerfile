FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /app
EXPOSE 8080

RUN dotnet tool install --global dotnet-ef --version 8.0.29
ENV PATH="$PATH:/root/.dotnet/tools"

# Restore separately so Docker caches this layer unless csproj changes
COPY *.csproj ./
RUN dotnet restore

COPY . .

# dotnet watch gives you hot reload while editing in VS Code on the host
CMD ["dotnet", "watch", "run", "--urls", "http://0.0.0.0:8080"]
