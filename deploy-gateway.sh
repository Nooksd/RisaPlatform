echo "Building Gateway Docker image..."
docker build -f docker/Gateway.Dockerfile -t your-registry/gateway-api:latest .

echo "Pushing to registry..."
docker push your-registry/gateway-api:latest

echo "Deploying to Kubernetes..."

kubectl create namespace gateway --dry-run=client -o yaml | kubectl apply -f -

kubectl apply -f k8s/redis/

kubectl apply -f k8s/gateway/

echo "Waiting for Gateway deployment..."
kubectl rollout status deployment/gateway-api -n gateway

echo "Gateway deployment status:"
kubectl get pods -n gateway
kubectl get svc -n gateway

echo "Gateway deployed successfully!"