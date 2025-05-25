#!/bin/bash
set -e

# Colors for pretty output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print status messages
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

# Function to check if a command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Display banner
echo -e "${GREEN}"
echo "==================================================================="
echo "  RealEstate API - Automated VPS Setup and Deployment Script"
echo "==================================================================="
echo -e "${NC}"

print_status "Starting setup process..."

# Check if running as root
if [ "$(id -u)" != "0" ]; then
   print_error "This script must be run as root. Please use sudo."
   exit 1
fi

# Update system packages
print_status "Updating system packages..."
apt-get update
apt-get upgrade -y
print_success "System packages updated successfully."

# Install prerequisites
print_status "Installing prerequisites..."
apt-get install -y \
    apt-transport-https \
    ca-certificates \
    curl \
    gnupg \
    lsb-release \
    software-properties-common
print_success "Prerequisites installed successfully."

# Install Docker if not installed
if ! command_exists docker; then
    print_status "Installing Docker..."
    curl -fsSL https://get.docker.com -o get-docker.sh
    sh get-docker.sh
    print_success "Docker installed successfully."
else
    print_warning "Docker is already installed, skipping."
fi

# Install Docker Compose if not installed
if ! command_exists docker-compose; then
    print_status "Installing Docker Compose..."
    curl -L "https://github.com/docker/compose/releases/download/v2.12.2/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
    chmod +x /usr/local/bin/docker-compose
    print_success "Docker Compose installed successfully."
else
    print_warning "Docker Compose is already installed, skipping."
fi

# Start Docker service if not running
if ! systemctl is-active --quiet docker; then
    print_status "Starting Docker service..."
    systemctl start docker
    systemctl enable docker
    print_success "Docker service started."
else
    print_warning "Docker service is already running, skipping."
fi

# Create project directory
PROJECT_DIR="/opt/realestate"
print_status "Creating project directory at ${PROJECT_DIR}..."
mkdir -p "${PROJECT_DIR}"
cd "${PROJECT_DIR}"
print_success "Project directory created."

# Create necessary files for deployment

print_status "Creating Dockerfile..."
mkdir -p src/RealEstate.API
cat > src/RealEstate.API/Dockerfile << 'EOF'
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy csproj files and restore dependencies
COPY ["src/RealEstate.API/RealEstate.API.csproj", "src/RealEstate.API/"]
COPY ["src/RealEstate.Core/RealEstate.Core.csproj", "src/RealEstate.Core/"]
COPY ["src/RealEstate.Infrastructure/RealEstate.Infrastructure.csproj", "src/RealEstate.Infrastructure/"]
RUN dotnet restore "src/RealEstate.API/RealEstate.API.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "src/RealEstate.API/RealEstate.API.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "src/RealEstate.API/RealEstate.API.csproj" -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Install PostgreSQL client for migrations
RUN apt-get update && apt-get install -y postgresql-client

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5268
ENV ASPNETCORE_ENVIRONMENT=Production

# Copy init script
COPY src/RealEstate.API/init-db.sh .
RUN chmod +x init-db.sh

EXPOSE 5268
ENTRYPOINT ["dotnet", "RealEstate.API.dll"]
EOF
print_success "Dockerfile created."

print_status "Creating docker-compose.yml..."
cat > docker-compose.yml << 'EOF'
version: '3.8'

services:
  # PostgreSQL Database
  postgres:
    image: postgres:15
    container_name: realestate-postgres
    environment:
      POSTGRES_DB: RealEstateDb
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: 123
    ports:
      - "5432:5432"
    volumes:
      - postgres-data:/var/lib/postgresql/data
    restart: unless-stopped
    networks:
      - realestate-network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5

  # .NET API Application
  api:
    build:
      context: .
      dockerfile: src/RealEstate.API/Dockerfile
    container_name: realestate-api
    ports:
      - "5268:5268"
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=RealEstateDb;Username=postgres;Password=123
      - ASPNETCORE_ENVIRONMENT=Production
    depends_on:
      postgres:
        condition: service_healthy
    restart: unless-stopped
    networks:
      - realestate-network
    command: bash -c "./init-db.sh && dotnet RealEstate.API.dll"

# Shared volumes
volumes:
  postgres-data:
    name: realestate-postgres-data

# Shared networks
networks:
  realestate-network:
    name: realestate-network
EOF
print_success "docker-compose.yml created."

print_status "Creating database initialization script..."
cat > src/RealEstate.API/init-db.sh << 'EOF'
#!/bin/bash
set -e

echo "Waiting for PostgreSQL to be ready..."
# Wait for PostgreSQL to be ready
until PGPASSWORD=123 psql -h postgres -U postgres -d postgres -c '\q'; do
  >&2 echo "PostgreSQL is unavailable - sleeping"
  sleep 1
done

echo "PostgreSQL is up - executing migrations"

# Create the database if it doesn't exist
PGPASSWORD=123 psql -h postgres -U postgres -tc "SELECT 1 FROM pg_database WHERE datname = 'RealEstateDb'" | grep -q 1 || PGPASSWORD=123 psql -h postgres -U postgres -c "CREATE DATABASE \"RealEstateDb\""

# Update all existing users to have PhoneNumberConfirmed = true
PGPASSWORD=123 psql -h postgres -U postgres -d RealEstateDb -c "
UPDATE \"AspNetUsers\" SET \"PhoneNumberConfirmed\" = true WHERE \"PhoneNumberConfirmed\" = false;
"

echo "Database initialization completed successfully"
EOF
chmod +x src/RealEstate.API/init-db.sh
print_success "Database initialization script created."

print_status "Creating .dockerignore file..."
cat > .dockerignore << 'EOF'
# Git
.git
.gitignore
.gitattributes

# Docker
Dockerfile
docker-compose.yml
.dockerignore

# Build and temporary files
**/bin/
**/obj/
**/out/
**/TestResults/
**/.vs/
**/.vscode/
**/.idea/
**/node_modules/
**/wwwroot/lib/

# Others
LICENSE
README.md
*.md
*.yml
*.ps1
*.cmd
*.sh
!init-db.sh
EOF
print_success ".dockerignore file created."

# Clone the actual project from Git
print_status "Would you like to clone the RealEstate project from Git? (y/n)"
read -r response
if [[ "$response" =~ ^([yY][eE][sS]|[yY])$ ]]; then
    print_status "Please enter the Git repository URL:"
    read -r repo_url
    
    print_status "Cloning project from Git repository..."
    # Save the Dockerfile and init-db.sh before cloning
    mv src/RealEstate.API/Dockerfile Dockerfile.tmp
    mv src/RealEstate.API/init-db.sh init-db.sh.tmp
    
    # Clone the repository
    git clone "$repo_url" .
    
    # Restore the Dockerfile and init-db.sh
    mkdir -p src/RealEstate.API
    mv Dockerfile.tmp src/RealEstate.API/Dockerfile
    mv init-db.sh.tmp src/RealEstate.API/init-db.sh
    
    print_success "Project cloned successfully."
else
    print_warning "Skipping Git clone. You'll need to copy your project files manually."
fi

# Configure firewall to allow traffic on port 5268
print_status "Configuring firewall to allow traffic on port 5268..."
if command_exists ufw; then
    ufw allow 5268/tcp
    print_success "Firewall configured to allow traffic on port 5268."
else
    print_warning "UFW not installed. Please ensure port 5268 is open in your firewall."
fi

# Deploy the application with Docker Compose
print_status "Starting the application with Docker Compose..."
docker-compose up -d --build

# Wait for the application to start
print_status "Waiting for the application to start (this may take a minute)..."
sleep 30

# Get server IP
SERVER_IP=$(hostname -I | awk '{print $1}')

# Check if the application is running
if docker ps | grep -q "realestate-api"; then
    print_success "RealEstate API is now running!"
    echo -e "${GREEN}==================================================================="
    echo "  RealEstate API is available at: http://$SERVER_IP:5268"
    echo "  Swagger UI is available at: http://$SERVER_IP:5268/swagger"
    echo "==================================================================="
    echo -e "${NC}"
    
    # Display container logs
    print_status "Application container logs:"
    docker logs realestate-api | tail -n 20
else
    print_error "There was an issue starting the RealEstate API. Check the logs for more information:"
    docker-compose logs
fi

# Instructions for managing the application
echo -e "${YELLOW}"
echo "==================================================================="
echo "  USEFUL COMMANDS FOR MANAGING YOUR APPLICATION"
echo "==================================================================="
echo "  View logs:                docker-compose logs -f"
echo "  Stop application:         docker-compose down"
echo "  Start application:        docker-compose up -d"
echo "  Restart application:      docker-compose restart"
echo "  Rebuild and restart:      docker-compose up -d --build"
echo "  Remove all data:          docker-compose down -v"
echo "==================================================================="
echo -e "${NC}"

print_success "Setup completed successfully!" 