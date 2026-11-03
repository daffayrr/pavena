---
name: devops-deployment
description: Deploy and operate Pavena SaaS using containerized services, secure configuration, monitoring, backups, and repeatable releases.
---

# DevOps & Deployment Skill

## Initial deployment

Docker Compose is appropriate for early environments. Keep services in separate containers:
- ASP.NET Core web/API.
- Background worker.
- Collaboration server.
- MySQL.
- S3-compatible object storage such as MinIO.
- Nginx reverse proxy.

Do not assume all services must run on one machine in production. Start simply but preserve separation and backup boundaries.

## Environment separation

Maintain separate:
- Local development.
- Automated test.
- Staging.
- Production.

Use environment variables or secret management for:
- Database credentials.
- Identity/email provider credentials.
- Object storage keys.
- Signing keys.
- Collaboration service secrets.
- External API keys.

Never commit `.env` files containing real secrets.

## Networking

- Expose only required public ports.
- Keep database and object storage private.
- Use HTTPS.
- Configure trusted proxy headers correctly.
- Restrict collaboration service origins and authentication.
- Do not expose internal admin endpoints publicly without authentication and network controls.

## Observability

Implement:
- Structured application logs.
- Correlation/request IDs.
- Health/readiness checks.
- Error tracking.
- Metrics for API latency, error rate, DB connections, background job failures, collaboration rooms, active sockets, update persistence latency, and storage usage.

Do not log passwords, tokens, private document contents, or sensitive personal data.

## Backup and recovery

- Back up relational database.
- Back up object storage.
- Test restore procedures regularly.
- Define recovery point and recovery time objectives.
- Ensure database backups and object storage backups are consistent enough for the application's data model.
- Keep backups protected from production account compromise.

## Release process

1. Build immutable artifacts.
2. Run tests and security checks.
3. Apply reviewed migrations.
4. Deploy to staging.
5. Smoke-test key flows.
6. Deploy production using a rollback plan.
7. Monitor errors and latency.
8. Document release and migration status.

## Scaling triggers

Scale components only when metrics justify it:
- Web/API horizontal scaling for request load.
- Collaboration service scaling for concurrent rooms/sessions.
- Worker scaling for export/compile queues.
- Database tuning/read replicas when appropriate.
- Dedicated search service only when DB search is insufficient.

Do not introduce Kubernetes or distributed infrastructure in MVP without an operational need.
