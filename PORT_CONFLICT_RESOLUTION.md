# Resolving Port Conflicts on VPS

When deploying the RealEstate application to your VPS, you might encounter an error like this:

```
ERROR: for realestate-nginx  Cannot start service nginx: driver failed programming external connectivity on endpoint realestate-nginx: Error starting userland proxy: listen tcp4 0.0.0.0:4545: bind: address already in use
```

This indicates that port 4545 is already being used by another process on your VPS. Instead of changing the application port, you can identify and stop the conflicting process.

## Using the Port Conflict Resolution Script

We've provided a script to help you identify and resolve port conflicts:

1. Make the script executable:
   ```bash
   chmod +x fix-port-conflict.sh
   ```

2. Run the script as root:
   ```bash
   sudo ./fix-port-conflict.sh
   ```

3. The script will:
   - Check if port 4545 is in use
   - Identify the process using the port
   - Give you the option to stop the process

4. Once the port is freed, you can run the deployment script:
   ```bash
   ./vps-deploy.sh deploy
   ```

## Manual Resolution

If the script doesn't work, you can manually resolve the port conflict:

1. Find the process using port 4545:
   ```bash
   sudo netstat -tulnp | grep 4545
   ```
   or
   ```bash
   sudo lsof -i :4545
   ```

2. Note the process ID (PID) from the output

3. Stop the process:
   ```bash
   sudo kill <PID>
   ```
   
   If the process doesn't stop, use:
   ```bash
   sudo kill -9 <PID>
   ```

4. Run the deployment script:
   ```bash
   ./vps-deploy.sh deploy
   ```

## Alternative Solutions

If you can't or don't want to stop the process using port 4545:

1. **Temporary port change**: Temporarily change the port in `docker-compose.prod.yml`, deploy the application, then change it back for future deployments.

2. **Configure a reverse proxy**: Set up a reverse proxy on your VPS to forward requests from another port to port 4545 internally.

3. **Identify and disable the service**: If the process using port 4545 is a system service you don't need, consider disabling it permanently:
   ```bash
   sudo systemctl stop service-name
   sudo systemctl disable service-name
   ```

## Need Help?

If you continue to experience issues, check:
- What service is using port 4545 and if it's necessary
- If there are firewall rules affecting port access
- If your hosting provider has any port restrictions 