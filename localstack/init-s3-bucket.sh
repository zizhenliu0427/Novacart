#!/bin/bash
# LocalStack init hook: create the product-images bucket on first startup.
# Runs automatically when LocalStack becomes ready (mounted into /etc/localstack/init/ready.d/).
echo "Creating S3 bucket: novacart-product-images"
awslocal s3 mb s3://novacart-product-images || echo "Bucket may already exist — continuing."
awslocal s3 ls
echo "S3 bucket initialisation complete."
