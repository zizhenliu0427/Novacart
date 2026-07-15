-- Phase 5–6: logical database-per-service on one PostgreSQL instance.
SELECT 'CREATE DATABASE novacart_auth'     WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'novacart_auth')\gexec
SELECT 'CREATE DATABASE novacart_product'  WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'novacart_product')\gexec
SELECT 'CREATE DATABASE novacart_commerce' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'novacart_commerce')\gexec
SELECT 'CREATE DATABASE novacart_cart'     WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'novacart_cart')\gexec
