# Real Estate Image Upload Fix - Troubleshooting Guide

## Summary of Changes Made

### 🔧 Docker Configuration Fixed
1. **Volume Mapping Issue**: Changed from bind mount to named volume to prevent overwriting container files
2. **Permissions**: Added proper directory permissions and ownership in Dockerfile
3. **Directory Structure**: Ensured image directories are created with correct permissions

### 🖼️ Image Upload Logic Enhanced
1. **Cross-Platform Compatibility**: Updated SaveImageAsync method for Linux containers
2. **Error Handling**: Added comprehensive error handling and logging
3. **File Permissions**: Added logic to set proper file permissions after upload
4. **URL Format**: Fixed image URL format to use forward slashes

### 🌐 Nginx Configuration Updated
1. **CORS Headers**: Added proper CORS headers for image serving
2. **File Upload Settings**: Increased max body size and timeout for large uploads
3. **Logging**: Added access and error logging for debugging

### 📁 Static File Serving
1. **Enhanced Static Files**: Added explicit static file configuration with CORS headers
2. **Cache Headers**: Added proper caching headers for images

## Files Modified

### `docker-compose.prod.yml`
- Changed volume mapping from `./wwwroot:/app/wwwroot` to `realestate-images:/app/wwwroot/images`
- Added named volume for persistent image storage

### `src/RealEstate.API/Dockerfile.prod`
- Added directory creation with proper permissions
- Set www-data ownership for upload directories

### `src/RealEstate.API/Controllers/PropertiesController.cs`
- Enhanced `SaveImageAsync` method with:
  - Better error handling
  - Cross-platform compatibility
  - File permission setting
  - Input sanitization

### `src/RealEstate.API/Program.cs`
- Enhanced static file serving with CORS headers
- Added proper cache control headers

### Nginx Configuration (`/etc/nginx/sites-available/realestate`)
- Updated to handle image uploads better
- Added CORS headers
- Increased upload limits

## Deployment Scripts Created

### `complete-fix-deployment.sh`
- Comprehensive deployment script that:
  - Stops and rebuilds containers
  - Sets up proper permissions
  - Updates nginx configuration
  - Tests connectivity and functionality

### `test-image-upload.sh`
- Testing script to verify:
  - Container status
  - Directory permissions
  - API connectivity
  - Manual image upload capability

## How to Deploy the Fix

1. **Run the deployment script**:
   ```bash
   chmod +x complete-fix-deployment.sh
   ./complete-fix-deployment.sh
   ```

2. **Test the functionality**:
   ```bash
   chmod +x test-image-upload.sh
   ./test-image-upload.sh
   ```

## Common Issues and Solutions

### Issue 1: Images not saving
**Symptoms**: API returns success but no files appear in directory
**Solution**: 
- Check container permissions: `docker exec realestate-api ls -la /app/wwwroot/`
- Verify directory exists: `docker exec realestate-api ls -la /app/wwwroot/images/`
- Test write permissions: `docker exec realestate-api touch /app/wwwroot/images/properties/test.txt`

### Issue 2: Images not accessible via URL
**Symptoms**: Images save but return 404 when accessed
**Solution**:
- Check nginx configuration is applied: `sudo nginx -t`
- Reload nginx: `sudo systemctl reload nginx`
- Verify static file serving is working in .NET app

### Issue 3: Container startup issues
**Symptoms**: Containers fail to start or keep restarting
**Solution**:
- Check logs: `docker logs realestate-api`
- Verify database connection
- Check if ports are available

### Issue 4: Permission denied errors
**Symptoms**: "Permission denied" errors in logs
**Solution**:
- Fix container permissions:
  ```bash
  docker exec realestate-api chmod -R 755 /app/wwwroot/
  docker exec realestate-api chown -R 1000:1000 /app/wwwroot/
  ```

## Testing Image Upload

### Method 1: Using curl (requires authentication token)
```bash
# First, get an authentication token by logging in
curl -X POST "http://62.171.153.198:4545/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"phoneNumber":"YOUR_PHONE","password":"YOUR_PASSWORD"}'

# Use the token to upload an image
curl -X POST "http://62.171.153.198:4545/api/properties/with-images" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -F "MainImage=@/path/to/image.jpg" \
  -F 'PropertyData={"Title":"Test Property","Description":"Test","Price":100000,"Area":100,"Bedrooms":3,"Bathrooms":2,"PropertyType":0,"Location":"Test","Address":"Test","IsAvailable":true,"IsForSale":true,"Features":[]}'
```

### Method 2: Using Postman or similar API tool
1. Set up POST request to: `http://62.171.153.198:4545/api/properties/with-images`
2. Add Authorization header with Bearer token
3. Use form-data with:
   - `MainImage`: File upload
   - `PropertyData`: JSON string with property details

## Monitoring and Debugging

### View logs
```bash
# API logs
docker logs -f realestate-api

# Nginx logs
sudo tail -f /var/log/nginx/realestate_error.log
sudo tail -f /var/log/nginx/realestate_access.log
```

### Check directory contents
```bash
# List uploaded images
docker exec realestate-api ls -la /app/wwwroot/images/properties/

# Check volume contents
docker volume inspect realestate-images
```

### Verify API endpoints
```bash
# Test properties endpoint
curl http://62.171.153.198:4545/api/properties

# Test swagger
curl http://62.171.153.198:4545/swagger
```

## Success Indicators

✅ **Containers running**: `docker ps` shows both API and database containers as "Up"  
✅ **Directory exists**: `/app/wwwroot/images/properties/` exists in container  
✅ **Write permissions**: Can create files in image directory  
✅ **API responding**: Properties endpoint returns HTTP 200 or 401  
✅ **Nginx working**: Proxy passes requests correctly  
✅ **Images accessible**: Uploaded images can be accessed via URL  

The image upload functionality should now work correctly on your VPS! 🎉
