# RealEstate Docker Deployment Guide

## Quick Start

### 1. Make the deployment script executable
```bash
chmod +x docker-deploy.sh
```

### 2. Deploy the application
```bash
./docker-deploy.sh deploy
```

## Available Commands

| Command | Description |
|---------|-------------|
| `./docker-deploy.sh deploy` | Full deployment (backup, build, deploy) |
| `./docker-deploy.sh status` | Show current container status |
| `./docker-deploy.sh logs` | Show recent application logs |
| `./docker-deploy.sh health` | Check service health |
| `./docker-deploy.sh backup` | Create database backup |
| `./docker-deploy.sh restart` | Restart all services |
| `./docker-deploy.sh stop` | Stop all services |
| `./docker-deploy.sh start` | Start services |

## Manual Docker Commands

### Build and start services
```bash
docker compose -f docker-compose.prod.yml up -d --build
```

### View logs
```bash
docker compose -f docker-compose.prod.yml logs -f
```

### Stop services
```bash
docker compose -f docker-compose.prod.yml down
```

### Check container status
```bash
docker ps --filter "name=realestate"
```

## Service Configuration

- **API**: Runs on port 5269 (mapped to container port 8080)
- **PostgreSQL**: Runs on port 5432
- **Nginx**: Configured to proxy to port 5269 on port 4545

## Health Checks

The API includes health checks that run every 30 seconds. You can manually check:

```bash
curl http://localhost:5269/health
```

## Database Backups

Automatic backups are created during deployment and stored in `/opt/backups/realestate/`

## Troubleshooting

### Check API logs
```bash
docker logs realestate-api --tail 50
```

### Check database logs
```bash
docker logs realestate-postgres --tail 50
```

### Check if services are healthy
```bash
docker ps --filter "name=realestate"
```

### Access database directly
```bash
docker exec -it realestate-postgres psql -U postgres -d RealEstateDb
```

## File Structure

```
/opt/RealEstate/
├── docker-compose.prod.yml    # Production Docker Compose
├── docker-deploy.sh           # Deployment script
├── src/
│   └── RealEstate.API/
│       └── Dockerfile.prod    # Production Dockerfile
└── wwwroot/                   # Static files (mounted)
```

## Current Nginx Configuration

Your nginx is already configured to proxy:
- Port 4545 → localhost:5269 (API)
- Static files served from `/var/www/realestate/publish/wwwroot/images/`

The Docker setup maintains compatibility with your existing nginx configuration.
