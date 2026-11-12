# Production Kubernetes Architecture & Deployment Guide

This document describes the Kubernetes production topology and orchestration setup for the distributed E-Commerce platform built with **.NET 10** microservices and **Angular 16**.

---

## 1. Kubernetes Cluster Topology

The platform deploys into the isolated `ecommerce` namespace. All inter-service communication stays internal via Kubernetes DNS (`ClusterIP` services), and traffic enters the cluster through the Kubernetes **Ingress Controller** or **API Gateway**.

```mermaid
graph TD
    Client["Client Web Browser / Mobile App"]
    Ingress["Kubernetes Ingress (nginx) :80/:443"]
    
    subgraph "Namespace: ecommerce"
        FrontendSvc["ecommerce-frontend:80 (ClusterIP)"]
        GatewaySvc["ecommerce-api-gateway:5000 (ClusterIP)"]
        
        AuthSvc["ecommerce-auth-service:5001 (ClusterIP)"]
        ProductSvc["ecommerce-product-service:5002 (ClusterIP)"]
        OrderSvc["ecommerce-order-service:5003 (ClusterIP)"]
        AISvc["ecommerce-ai-service:5004 (ClusterIP)"]
        
        SQLServerSvc["ecommerce-sqlserver:1433 (ClusterIP)"]
        PVC[("PersistentVolumeClaim\necommerce-sqlserver-pvc (5Gi)")]
        
        ConfigMap["ConfigMap: ecommerce-config"]
        Secrets["Secret: ecommerce-secrets"]
    end

    Client -->|HTTP / HTTPS| Ingress
    Ingress -->|"/"| FrontendSvc
    Ingress -->|"/api/*", "/orderHub/*"| GatewaySvc

    GatewaySvc -->|"/api/auth/*", "/api/users/*"| AuthSvc
    GatewaySvc -->|"/api/products/*", "/api/inventory/*"| ProductSvc
    GatewaySvc -->|"/api/orders/*", "/orderHub"| OrderSvc
    GatewaySvc -->|"/api/ai/*", "/api/chat/*"| AISvc

    OrderSvc -.->|"Internal Stock Reservation"| ProductSvc
    AISvc -.->|"Internal Catalog & Orders Query"| ProductSvc
    AISvc -.->|"Internal Catalog & Orders Query"| OrderSvc

    AuthSvc --> SQLServerSvc
    ProductSvc --> SQLServerSvc
    OrderSvc --> SQLServerSvc
    SQLServerSvc --- PVC
```

---

## 2. Directory Structure (`k8s/`)

All Kubernetes manifests are managed under `k8s/` and bundled via **Kustomize**:

```
k8s/
├── kustomization.yaml     # Master Kustomize bundle with common labels and namespace
├── namespace.yaml         # Isolated 'ecommerce' namespace definition
├── configmap.yaml         # Non-sensitive endpoints, environments, and URLs
├── secrets.yaml           # Database SA password, JWT secrets, and Azure AI API key
├── sqlserver.yaml         # MSSQL 2022 Deployment, PVC (5Gi), and ClusterIP Service
├── auth-service.yaml      # Auth microservice Deployment (2 replicas) + Service
├── product-service.yaml   # Product catalog microservice Deployment (2 replicas) + Service
├── order-service.yaml     # Order & checkout microservice Deployment (2 replicas) + Service
├── ai-service.yaml        # Azure AI multi-agent service Deployment + Service
├── api-gateway.yaml       # YARP API Gateway Deployment (2 replicas) + Service
├── frontend.yaml          # Angular SPA Nginx Deployment (2 replicas) + Service
└── ingress.yaml           # Nginx Ingress routing (/api, /orderHub, /)
```

---

## 3. Microservice Specifications & Port Mappings

| Service Name | K8s Service | Target Port | Replicas | Liveness / Readiness Check |
| :--- | :--- | :--- | :--- | :--- |
| **SQL Server** | `ecommerce-sqlserver` | `1433` | 1 | TCP Socket `:1433` |
| **Auth Service** | `ecommerce-auth-service` | `5001` | 2 | HTTP GET `/swagger/v1/swagger.json` |
| **Product Service** | `ecommerce-product-service` | `5002` | 2 | HTTP GET `/swagger/v1/swagger.json` |
| **Order Service** | `ecommerce-order-service` | `5003` | 2 | HTTP GET `/swagger/v1/swagger.json` |
| **AI Service** | `ecommerce-ai-service` | `5004` | 1 | HTTP GET `/swagger/v1/swagger.json` |
| **API Gateway** | `ecommerce-api-gateway` | `5000` | 2 | HTTP GET `/` (Status: Healthy) |
| **Frontend UI** | `ecommerce-frontend` | `80` | 2 | HTTP GET `/` |

---

## 4. Deployment Instructions

### Option A: Direct Deployment via `kubectl` + `kustomize` (Recommended)

1. Ensure your Kubernetes cluster context is active (`kubectl config current-context`).
2. Run single-command apply:
   ```powershell
   kubectl apply -k k8s/
   ```
3. Verify all pods are running:
   ```powershell
   kubectl get pods -n ecommerce -o wide
   ```
4. Verify all services are healthy:
   ```powershell
   kubectl get svc -n ecommerce
   ```

---

### Option B: Using the Automated PowerShell Deployment Script

Run the automated deployment script with built-in rollout validation:
```powershell
.\scripts\deploy-k8s.ps1
```

To clean up all deployed resources:
```powershell
.\scripts\deploy-k8s.ps1 -Delete
```

---

### Option C: Running Kubernetes Manifests in Podman (`podman kube play`)

For local development or testing without spinning up a full Kubernetes cluster:
```powershell
.\scripts\podman-kube-play.ps1
```

To tear down all Podman pods:
```powershell
.\scripts\podman-kube-down.ps1
```

---

## 5. Local Access & Port Forwarding

If you do not have an Ingress controller configured in your local cluster (e.g. Minikube / Kind / Docker Desktop), use Kubernetes port forwarding:

* **Access the Frontend (Angular SPA)**:
  ```powershell
  kubectl port-forward svc/ecommerce-frontend 4200:80 -n ecommerce
  # Navigate to http://localhost:4200
  ```

* **Access the API Gateway**:
  ```powershell
  kubectl port-forward svc/ecommerce-api-gateway 5000:5000 -n ecommerce
  # Test with: curl http://localhost:5000/
  ```

* **Access Microservices Swagger Directly**:
  ```powershell
  kubectl port-forward svc/ecommerce-auth-service 5001:5001 -n ecommerce
  kubectl port-forward svc/ecommerce-product-service 5002:5002 -n ecommerce
  kubectl port-forward svc/ecommerce-order-service 5003:5003 -n ecommerce
  kubectl port-forward svc/ecommerce-ai-service 5004:5004 -n ecommerce
  ```

---

## 6. Scaling & Production Readiness

### Horizontal Pod Autoscaling (HPA)
Deploy HPA for stateless services based on CPU/Memory usage:
```powershell
kubectl autoscale deployment ecommerce-api-gateway --cpu-percent=70 --min=2 --max=10 -n ecommerce
kubectl autoscale deployment ecommerce-order-service --cpu-percent=75 --min=2 --max=8 -n ecommerce
kubectl autoscale deployment ecommerce-product-service --cpu-percent=75 --min=2 --max=8 -n ecommerce
```

### CI/CD Integration
The `.github/workflows/ci.yml` pipeline automatically performs linting and dry-run syntax verification using:
```bash
kustomize build k8s/
```
Ensure all PRs pass manifest verification prior to merging.

