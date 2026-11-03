# Database Backup and Disaster Recovery Guide

## Purpose
This document defines disaster recovery, cold backup, streaming snapshot, and restoration procedures for Pavena transactional databases (PostgreSQL and MySQL) and private object storage (MinIO/S3).

---

## 1. PostgreSQL (Production Target Engine)

### 1.1 Automated Daily Logical Backup
Logical backups export schema, constraints, data, and sequence states:
```bash
# Set secure password in environment
export PGPASSWORD="${DB_PASSWORD}"

# Execute pg_dump with custom compressed archive format
pg_dump -h "${DB_HOST}" -p "${DB_PORT:-5432}" -U "${DB_USER}" -d "${DB_NAME}" \
  -F c -b -v -f "/backups/postgres/pavena_db_$(date +%Y%m%d_%H%M%S).dump"
```

### 1.2 Point-in-Time Recovery (PITR) with WAL Archiving
In `postgresql.conf`:
```ini
wal_level = replica
archive_mode = on
archive_command = 'test ! -f /backups/wal/%f && cp %p /backups/wal/%f'
archive_timeout = 900 # Forces archive every 15 minutes
```

### 1.3 Database Restoration Procedure
1. Create a clean target database:
   ```bash
   dropdb -h "${DB_HOST}" -U "${DB_USER}" "${TARGET_DB}"
   createdb -h "${DB_HOST}" -U "${DB_USER}" "${TARGET_DB}"
   ```
2. Restore from compressed dump:
   ```bash
   pg_restore -h "${DB_HOST}" -U "${DB_USER}" -d "${TARGET_DB}" -v \
     --no-owner --role="${DB_USER}" "/backups/postgres/target_backup.dump"
   ```
3. Verify integrity:
   ```bash
   psql -h "${DB_HOST}" -U "${DB_USER}" -d "${TARGET_DB}" -c "SELECT COUNT(*) FROM posts;"
   ```

---

## 2. MySQL (Development / Pilot Engine)

### 2.1 Logical Backup with mysqldump
```bash
mysqldump --host="${DB_HOST:-localhost}" \
          --port="${DB_PORT:-3306}" \
          --user="${DB_USER:-root}" \
          --password="${DB_PASSWORD}" \
          --single-transaction \
          --quick \
          --routines \
          --triggers \
          --default-character-set=utf8mb4 \
          pavena_db | gzip > "/backups/mysql/pavena_mysql_$(date +%Y%m%d_%H%M%S).sql.gz"
```

### 2.2 Restoration Procedure
```bash
gunzip < "/backups/mysql/target_backup.sql.gz" | mysql -h "${DB_HOST}" -u "${DB_USER}" -p"${DB_PASSWORD}" pavena_db
```

---

## 3. Object Storage Backup (MinIO / S3)

Because files are stored in object storage rather than database tables (per **ADR-0009**), object storage must be backed up concurrently:
1. **MinIO Client (`mc`) Mirroring:**
   ```bash
   mc mirror --overwrite --remove localminio/pavena-research-private backup-target/pavena-research-private-backups/
   ```
2. **S3 Bucket Versioning:**
   - Enable bucket versioning and Cross-Region Replication (CRR) on `pavena-research-private`.
   - Set lifecycle rule: retain non-current versions for 90 days.

---

## 4. Disaster Recovery RPO / RTO Targets
- **Recovery Point Objective (RPO):** < 15 minutes (using WAL archiving / transaction log backups).
- **Recovery Time Objective (RTO):** < 30 minutes for complete service restoration.
