# 🚀 Apex E-Commerce — Quick Runbook

---

## ▶️ How to Start (Choose Option 1 or 2)

### Option 1: The 1-Click Automatic Way (Recommended)
Open PowerShell in this folder and run:
```powershell
cd "C:\final push draft"
.\start-demo.ps1
```
*This automatically starts the VM, starts Kubernetes, launches port forwarders, and opens the website in your browser.*

---

### Option 2: The Manual 3-Step Way
If you prefer to run each command yourself:

**1. Start the Container Machine & Kubernetes:**
```powershell
podman machine start
podman start ecommerce-cluster-control-plane
```

**2. Start Port Forwarding:**
```powershell
.\ECommerceAPI\scripts\start-port-forwards.ps1
```
*(Leave the popup PowerShell windows open while using the app).*

**3. Open Your Browser:**
👉 **[http://localhost:4200](http://localhost:4200)**

---

## 🛑 How to Stop Everything Cleanly

When you are done and want to free up all computer RAM and CPU:

```powershell
.\stop-demo.ps1
```
*This cleanly closes all port-forwards, stops the Kubernetes cluster, and shuts down the Podman VM with zero orphaned processes.*

---

## 🔑 Demo Sign-In Accounts
- **Admin**: Click **Admin (`admin@local`)** → Click **Sign In to Account** *(Password: `Admin123!`)*
- **Customer**: Click **Customer (`demo@local`)** → Click **Sign In to Account** *(Password: `Password123`)*

---

## 🗄️ Database Access (SSMS / GUI)
- **Server Name**: `127.0.0.1,1433`
- **Login**: `sa`
- **Password**: `Password123!`
- **Trust Server Certificate**: ☑ Checked
