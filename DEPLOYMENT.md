# RealEstate.NET VPS Deployment Guide

This guide explains how to deploy the RealEstate.NET application to a VPS using Docker.

## Prerequisites

- Linux VPS with Docker and Docker Compose installed
- Git installed
- Basic knowledge of Linux commands
- Domain name (optional)

## Quick Deployment

### 1. Clone the repository

```bash
git clone <your-repository-url>
cd RealEstate
```

### 2. Make the deployment script executable

```bash
chmod +x vps-deploy.sh
```

### 3. Deploy the application

```bash
./vps-deploy.sh deploy
```

This will:
- Create necessary directories
- Generate secure passwords (stored in .env file)
- Create database backup (if existing)
- Build and start all containers
- Check service health

## Accessing the Application

After deployment, the application will be available at:

- API: http://your-server-ip:4545
- Swagger UI: http://your-server-ip:4545/swagger

## Available Commands

| Command | Description |
|---------|-------------|
| `./vps-deploy.sh deploy` | Full deployment (backup, build, deploy) |
| `./vps-deploy.sh status` | Show current container status |
| `./vps-deploy.sh logs [service]` | Show recent logs (optional: specify service) |
| `./vps-deploy.sh health` | Check service health |
| `./vps-deploy.sh backup` | Create database backup |
| `./vps-deploy.sh restart` | Restart all services |
| `./vps-deploy.sh stop` | Stop all services |
| `./vps-deploy.sh start` | Start services |

## Directory Structure

```
/
├── docker-compose.prod.yml    # Production Docker Compose
├── vps-deploy.sh              # Deployment script
├── .env                       # Environment variables (auto-generated)
├── nginx/                     # Nginx configuration
│   ├── nginx.conf             # Main Nginx config
│   └── conf.d/                # Virtual host configs
│       └── default.conf       # API proxy config
├── db-init/                   # Database initialization scripts
│   └── 01-init.sql            # Initial DB setup
└── src/                       # Application source code
    └── RealEstate.API/
        └── Dockerfile.prod    # Production Dockerfile
```

## Environment Variables

The deployment script automatically creates a `.env` file with secure random passwords. You can modify this file if needed:

```
# Database settings
DB_PASSWORD=random_password

# JWT settings
JWT_SECRET=random_secret

# Environment
ASPNETCORE_ENVIRONMENT=Production
```

## Database Backups

Backups are automatically created during deployment and stored in `/opt/backups/realestate/`.

To manually create a backup:

```bash
./vps-deploy.sh backup
```

## Using a Domain Name

To use your own domain name:

1. Update the Nginx configuration in `nginx/conf.d/default.conf`:

```nginx
server {
    listen 80;
    server_name your-domain.com;
    ...
}
```

2. Set up SSL with Let's Encrypt:

```bash
apt-get update
apt-get install -y certbot python3-certbot-nginx
certbot --nginx -d your-domain.com
```

## Troubleshooting

### Check container logs

```bash
# View all logs
./vps-deploy.sh logs

# View specific service logs
./vps-deploy.sh logs api
./vps-deploy.sh logs postgres
./vps-deploy.sh logs nginx
```

### Check container status

```bash
./vps-deploy.sh status
```

### Restart services

```bash
./vps-deploy.sh restart
```

### Access the database directly

```bash
docker exec -it realestate-postgres psql -U realestateuser -d realestate
```

## Security Considerations

- The application uses secure random passwords for the database and JWT secret
- Nginx is configured with security headers
- Container services use non-root users where possible
- Database is only accessible from within the Docker network

## Maintenance

### Updating the Application

1. Pull the latest changes:

```bash
git pull
```

2. Redeploy:

```bash
./vps-deploy.sh deploy
```

### Scaling (Future)

For higher traffic loads, consider:

1. Adding a load balancer
2. Implementing database replication
3. Moving static assets to a CDN 