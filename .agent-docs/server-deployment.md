# Server Deployment

> Agent-first documentation. Server configuration, deployment, and operations reference.

## Server Overview

The Megabonk Together server provides:
- **Matchmaking**: WebSocket-based lobby management
- **NAT Punchthrough**: UDP rendezvous for P2P connections
- **Relay Service**: Fallback for failed P2P connections

### Ports

| Port | Protocol | Purpose |
|------|----------|---------|
| 5432 | HTTP | WebSocket matchmaking endpoint |
| 5678 | UDP | NAT punchthrough and relay |

## Docker Deployment

### Build Image

```bash
docker build -t megabonk-server .
```

### Run Container

```bash
docker run -d \
  --name megabonk-server \
  -p 5432:5432 \
  -p 5678:5678/udp \
  megabonk-server
```

### Docker Compose

```yaml
version: '3.8'
services:
  megabonk-server:
    build: .
    ports:
      - "5432:5432"
      - "5678:5678/udp"
    restart: unless-stopped
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
```

## Direct Deployment

### Prerequisites

- .NET 6.0 Runtime
- Linux or Windows Server

### Publish

```bash
cd src/server
dotnet publish -c Release -o ./publish
```

### Run

```bash
cd publish
./MegabonkTogether.Server
```

### Systemd Service (Linux)

```ini
# /etc/systemd/system/megabonk-server.service
[Unit]
Description=Megabonk Together Matchmaking Server
After=network.target

[Service]
Type=simple
User=megabonk
WorkingDirectory=/opt/megabonk-server
ExecStart=/opt/megabonk-server/MegabonkTogether.Server
Restart=on-failure
RestartSec=10

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl daemon-reload
sudo systemctl enable megabonk-server
sudo systemctl start megabonk-server
```

## Configuration

### appsettings.json

**File:** `src/server/appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft": "Error",
      "Microsoft.Hosting.Lifetime": "Information",
      "MegabonkTogether.Server.Services.WebSocketHandler": "Information",
      "MegabonkTogether.Server.Services.RendezVousServer": "Information"
    }
  }
}
```

### appsettings.Production.json

**File:** `src/server/appsettings.Production.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Error",
      "Microsoft.Hosting.Lifetime": "Warning"
    }
  }
}
```

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Production | Environment name |
| `ASPNETCORE_URLS` | http://+:5432 | HTTP endpoint |

## Architecture

### WebSocket Handler

**File:** `src/server/Services/WebSocketHandler.cs`

Handles:
- `/ws?random` - Quickplay matchmaking
- `/ws?friendlies` - Private lobbies with room codes

### RendezVous Server

**File:** `src/server/Services/RendezVousServer.cs`

Handles:
- NAT introduction requests
- Relay session management
- Stale entry cleanup

### Connection Pools

| Pool | Purpose |
|------|---------|
| `ConnectionIdPool` | Allocate unique connection IDs |
| `RoomCodePool` | Generate 4-character room codes |

## Matchmaking Flow

### Quickplay (`/ws?random`)

```
1. Client connects
2. Server adds to pending queue
3. When 2-6 players available or timeout:
   - Assign one as host
   - Send MatchInfo with host endpoint
   - Begin NAT punchthrough
4. All players connect via UDP
5. Game starts
```

### Friendlies (`/ws?friendlies`)

```
1. Host connects, receives room code
2. Host shares code with friends
3. Friends connect with room code
4. When all ready, host starts game
5. NAT punchthrough / relay setup
```

## Relay System

### When Relay is Used

- IPv6 addresses detected
- Symmetric NAT
- P2P connection timeout
- `force_relay` flag in token

### Relay Session Management

```csharp
public class RelaySession
{
    public uint HostConnectionId;
    public RelayPeer Host;
    public ConcurrentDictionary<uint, RelayPeer> Clients;
    public ConcurrentQueue<PendingRelayMessage> PendingToHost;
}
```

### Relay Flow

```
1. Server determines relay needed
2. Sends USE_RELAY to both parties
3. Both connect to UDP server
4. Both send RELAY_BIND to register
5. Server routes all traffic
```

## Metrics

### OpenTelemetry

**File:** `src/server/Services/MetricsService.cs`

Prometheus metrics exposed at `/metrics` (if enabled):

| Metric | Type | Description |
|--------|------|-------------|
| Active connections | Gauge | Current WebSocket connections |
| Active lobbies | Gauge | Current active lobbies |
| Relay sessions | Gauge | Active relay sessions |
| Messages processed | Counter | Total messages handled |

## Monitoring

### Log Analysis

```bash
# View all logs
docker logs megabonk-server

# Follow logs
docker logs -f megabonk-server

# Filter for relay
docker logs megabonk-server 2>&1 | grep -i relay

# Filter for NAT
docker logs megabonk-server 2>&1 | grep -i "NAT"
```

### Health Check

```bash
# Check WebSocket endpoint
curl http://localhost:5432/ws

# Check UDP port
nc -zuv localhost 5678
```

## Scaling Considerations

### Single Server Limits

- ~100 concurrent WebSocket connections
- ~50 concurrent relay sessions
- Limited by network bandwidth for relay

### Horizontal Scaling

For higher capacity:

1. **Load Balancer** in front of WebSocket servers
2. **Sticky Sessions** required (WebSocket stateful)
3. **Shared Relay** service or dedicated relay servers
4. **Redis** for cross-instance lobby state

### Recommended Setup

```
                    ┌─────────────┐
                    │ Load        │
                    │ Balancer    │
                    └──────┬──────┘
                           │
            ┌──────────────┼──────────────┐
            ▼              ▼              ▼
     ┌──────────┐   ┌──────────┐   ┌──────────┐
     │ Server 1 │   │ Server 2 │   │ Server 3 │
     │ (WS+Relay)│  │ (WS+Relay)│  │ (WS+Relay)│
     └──────────┘   └──────────┘   └──────────┘
```

## Security

### Recommendations

1. **HTTPS/WSS**: Use reverse proxy with TLS
2. **Rate Limiting**: Limit connections per IP
3. **Input Validation**: Validate all message payloads
4. **Authentication**: Consider adding auth for private lobbies

### Nginx Reverse Proxy

```nginx
server {
    listen 443 ssl;
    server_name megabonk-server.com;

    ssl_certificate /path/to/cert.pem;
    ssl_certificate_key /path/to/key.pem;

    location /ws {
        proxy_pass http://localhost:5432;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_read_timeout 86400;
    }
}
```

## Troubleshooting

### Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| Clients can't connect | Firewall | Open ports 5432, 5678 |
| P2P fails, always relay | NAT type | Expected for some networks |
| High latency in relay | Server location | Use server closer to players |
| Memory growth | Connection leak | Check cleanup logs |

### Debug Logging

Enable verbose logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

### Stale Entry Cleanup

Server automatically cleans up:
- Host registrations: 1 minute
- Pending clients: 45 seconds
- Processed pairs: 45 seconds
- Pending relay: 30 seconds

Check cleanup logs:
```
RendezVous Server Cleanup Report at {timestamp}:
    Cleanup complete: X hosts, Y pending clients...
```

## Cloud Deployment

### AWS

- **EC2**: t3.small minimum
- **Security Groups**: Allow 5432 (TCP), 5678 (UDP)
- **Elastic IP**: Recommended for stable endpoint

### DigitalOcean

- **Droplet**: $6/month minimum
- **Floating IP**: For stable endpoint

### Google Cloud

- **Compute Engine**: e2-small minimum
- **Firewall**: Allow 5432 (TCP), 5678 (UDP)

## Maintenance

### Updates

```bash
# Pull latest code
git pull

# Rebuild and restart
docker-compose build
docker-compose up -d
```

### Backup

No persistent data to backup. Server is stateless.

### Monitoring Alerts

Set up alerts for:
- Server down (health check fails)
- High memory usage
- High CPU usage
- Connection count spike
