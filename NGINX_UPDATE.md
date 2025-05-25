# Updated Nginx Configuration for RealEstate

**New API Port: 7269**

Update your nginx config at `/etc/nginx/sites-available/realestate`:

```nginx
server {
    listen 4545;
    server_name 62.171.153.198;
    client_max_body_size 100M;
    
    # API proxy configuration  
    location /api/ {
        proxy_pass http://localhost:7269/;  # Updated port
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }
    
    # Direct API access (without /api prefix)
    location / {
        proxy_pass http://localhost:7269/;  # Updated port
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }
    
    # Static files (images, etc.)
    location /images/ {
        alias /opt/RealEstate/wwwroot/images/;  # Updated path
        expires 30d;
        add_header Cache-Control "public, no-transform";
        try_files $uri $uri/ =404;
    }
    
    # Swagger UI
    location /swagger {
        proxy_pass http://localhost:7269/swagger;  # Updated port
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
    
    # Health check endpoint
    location /health {
        proxy_pass http://localhost:7269/health;  # Updated port
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
    
    error_page 500 502 503 504 /50x.html;
    location = /50x.html {
        root /usr/share/nginx/html;
    }
}
```

## Commands to update:

```bash
# Edit nginx config
sudo nano /etc/nginx/sites-available/realestate

# Test nginx config
sudo nginx -t

# Reload nginx
sudo systemctl reload nginx
```

## Key Changes:
- **API Port**: 5269 → 7269
- **PostgreSQL Port**: 5432 → 7432 (external)
- **Database Name**: RealEstateDb → realestate  
- **Database User**: postgres → realestateuser
- **Static Files**: Updated to Docker volume mount path
