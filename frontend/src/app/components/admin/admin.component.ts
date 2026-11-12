import { Component, OnInit } from '@angular/core';
import { ProductService } from '../../services/product.service';
import { InventoryService } from '../../services/inventory.service';
import { AuthService } from '../../services/auth.service';
import { OrderService } from '../../services/order.service';
import { ProductImageService } from '../../services/product-image.service';
import { Product } from '../../models/product';

@Component({
  selector: 'app-admin',
  template: `
  <div class="container" style="max-width: 1140px; padding: 32px 20px 80px;">
    <!-- Top Header -->
    <div style="display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 30px;">
      <div>
        <div style="display: inline-flex; align-items: center; gap: 6px; background: #f5f3ff; color: #7c3aed; padding: 4px 12px; border-radius: 9999px; font-size: 0.75rem; font-weight: 700; text-transform: uppercase; margin-bottom: 8px;">
          <i class="fa-solid fa-shield-halved"></i>
          <span>System Administration</span>
        </div>
        <h1 style="font-size: 2.2rem; font-weight: 800; color: #0f172a; margin: 0;">Admin Command Center</h1>
        <p style="color: #64748b; font-size: 0.95rem; margin-top: 4px;">Manage catalog inventory, approve customer orders, and perform bulk Excel imports.</p>
      </div>
      <div style="display: flex; gap: 10px;">
        <button (click)="load(); loadAllOrders()" class="btn btn-secondary">
          <i class="fa-solid fa-rotate"></i>
          <span>Refresh All</span>
        </button>
      </div>
    </div>

    <!-- Access Denied Guard -->
    <div *ngIf="!auth.isAdmin()" class="alert alert-danger" style="padding: 24px; text-align: center;">
      <i class="fa-solid fa-ban" style="font-size: 2rem; margin-bottom: 12px; display: block;"></i>
      <h3 style="margin-bottom: 6px;">Access Restricted</h3>
      <p>You must be authenticated with the <strong>Administrator</strong> role to access this console.</p>
      <div style="margin-top: 16px;">
        <a routerLink="/login" class="btn btn-primary">Switch to Admin Account</a>
      </div>
    </div>

    <div *ngIf="auth.isAdmin()">
      <!-- Metric Counters -->
      <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 20px; margin-bottom: 32px;">
        <div class="card" style="padding: 20px; display: flex; align-items: center; gap: 16px;">
          <div style="width: 48px; height: 48px; border-radius: 12px; background: #e0e7ff; color: #4338ca; display: flex; align-items: center; justify-content: center; font-size: 1.25rem;">
            <i class="fa-solid fa-boxes-stacked"></i>
          </div>
          <div>
            <span style="font-size: 0.75rem; font-weight: 700; color: #64748b; text-transform: uppercase;">Total Products</span>
            <div style="font-size: 1.6rem; font-weight: 800; color: #0f172a;">{{ products.length }}</div>
          </div>
        </div>

        <div class="card" style="padding: 20px; display: flex; align-items: center; gap: 16px;">
          <div style="width: 48px; height: 48px; border-radius: 12px; background: #e0f2fe; color: #0284c7; display: flex; align-items: center; justify-content: center; font-size: 1.25rem;">
            <i class="fa-solid fa-receipt"></i>
          </div>
          <div>
            <span style="font-size: 0.75rem; font-weight: 700; color: #64748b; text-transform: uppercase;">Total Orders</span>
            <div style="font-size: 1.6rem; font-weight: 800; color: #0f172a;">{{ allOrders.length }}</div>
          </div>
        </div>

        <div class="card" style="padding: 20px; display: flex; align-items: center; gap: 16px;">
          <div style="width: 48px; height: 48px; border-radius: 12px; background: #fef3c7; color: #b45309; display: flex; align-items: center; justify-content: center; font-size: 1.25rem;">
            <i class="fa-solid fa-clock"></i>
          </div>
          <div>
            <span style="font-size: 0.75rem; font-weight: 700; color: #64748b; text-transform: uppercase;">Pending Approval</span>
            <div style="font-size: 1.6rem; font-weight: 800; color: #b45309;">{{ getPendingOrdersCount() }}</div>
          </div>
        </div>
      </div>

      <!-- Navigation Tabs -->
      <div style="display: flex; gap: 8px; border-bottom: 2px solid var(--border-color); margin-bottom: 28px;">
        <button (click)="activeTab = 'products'"
                [style.borderBottomColor]="activeTab === 'products' ? 'var(--primary)' : 'transparent'"
                [style.color]="activeTab === 'products' ? 'var(--primary)' : '#64748b'"
                [style.fontWeight]="activeTab === 'products' ? '700' : '600'"
                style="padding: 12px 20px; background: transparent; border: none; border-bottom: 3px solid transparent; cursor: pointer; font-size: 0.95rem; display: flex; align-items: center; gap: 8px; transition: all 0.15s ease;">
          <i class="fa-solid fa-table-list"></i>
          <span>Catalog Inventory</span>
        </button>
        <button (click)="activeTab = 'orders'"
                [style.borderBottomColor]="activeTab === 'orders' ? 'var(--primary)' : 'transparent'"
                [style.color]="activeTab === 'orders' ? 'var(--primary)' : '#64748b'"
                [style.fontWeight]="activeTab === 'orders' ? '700' : '600'"
                style="padding: 12px 20px; background: transparent; border: none; border-bottom: 3px solid transparent; cursor: pointer; font-size: 0.95rem; display: flex; align-items: center; gap: 8px; transition: all 0.15s ease;">
          <i class="fa-solid fa-clipboard-check"></i>
          <span>Order Approvals</span>
          <span *ngIf="getPendingOrdersCount() > 0" class="badge badge-warning" style="font-size: 0.7rem; padding: 2px 6px;">{{ getPendingOrdersCount() }}</span>
        </button>
        <button (click)="activeTab = 'import'"
                [style.borderBottomColor]="activeTab === 'import' ? 'var(--primary)' : 'transparent'"
                [style.color]="activeTab === 'import' ? 'var(--primary)' : '#64748b'"
                [style.fontWeight]="activeTab === 'import' ? '700' : '600'"
                style="padding: 12px 20px; background: transparent; border: none; border-bottom: 3px solid transparent; cursor: pointer; font-size: 0.95rem; display: flex; align-items: center; gap: 8px; transition: all 0.15s ease;">
          <i class="fa-solid fa-file-excel"></i>
          <span>Excel / CSV Import</span>
        </button>
      </div>

      <!-- TAB 1: Products Management -->
      <div *ngIf="activeTab === 'products'">
        <!-- Add / Edit Form Card -->
        <div class="card" style="padding: 28px; margin-bottom: 28px; background: #ffffff;">
          <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px;">
            <h3 style="font-size: 1.25rem; font-weight: 800; color: #0f172a; margin: 0;">
              {{ editing ? 'Edit Product #' + editing.id : 'Add New Product to Store' }}
            </h3>
            <button (click)="cancel()" *ngIf="editing" class="btn btn-secondary btn-sm">Cancel Edit</button>
          </div>

          <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(240px, 1fr)); gap: 16px;">
            <div class="form-group">
              <label class="form-label">Product Name</label>
              <input [(ngModel)]="form.name" class="form-control" placeholder="e.g. Wireless Ergonomic Mouse" />
            </div>
            <div class="form-group">
              <label class="form-label">Category</label>
              <input [(ngModel)]="form.category" class="form-control" placeholder="e.g. Electronics, Apparel" />
            </div>
            <div class="form-group">
              <label class="form-label">Price (USD)</label>
              <input type="number" step="0.01" [(ngModel)]="form.price" class="form-control" placeholder="0.00" />
            </div>
            <div class="form-group">
              <label class="form-label">Inventory Quantity</label>
              <input type="number" [(ngModel)]="form.quantity" class="form-control" placeholder="10" />
            </div>
            <div class="form-group" style="grid-column: 1 / -1;">
              <label class="form-label">Description</label>
              <input [(ngModel)]="form.description" class="form-control" placeholder="Full technical specs and item details..." />
            </div>
          </div>

          <div style="margin-top: 12px; display: flex; gap: 10px;">
            <button (click)="save()" class="btn btn-primary">
              <i class="fa-solid fa-floppy-disk"></i>
              <span>{{ editing ? 'Save Changes' : 'Create Product' }}</span>
            </button>
            <button (click)="cancel()" *ngIf="editing" class="btn btn-secondary">Cancel</button>
          </div>
        </div>

        <!-- Products Table -->
        <div class="card" style="padding: 24px; background: #ffffff;">
          <h3 style="font-size: 1.15rem; font-weight: 800; margin-bottom: 16px;">Catalog Inventory List ({{ products.length }} items)</h3>
          <div style="overflow-x: auto;">
            <table style="width: 100%; border-collapse: collapse; text-align: left;">
              <thead>
                <tr style="background: #f8fafc; border-bottom: 2px solid var(--border-color);">
                  <th style="padding: 12px 14px; font-size: 0.75rem; font-weight: 700; color: #64748b; text-transform: uppercase;">Image</th>
                  <th style="padding: 12px 14px; font-size: 0.75rem; font-weight: 700; color: #64748b; text-transform: uppercase;">Name</th>
                  <th style="padding: 12px 14px; font-size: 0.75rem; font-weight: 700; color: #64748b; text-transform: uppercase;">Category</th>
                  <th style="padding: 12px 14px; font-size: 0.75rem; font-weight: 700; color: #64748b; text-transform: uppercase;">Price</th>
                  <th style="padding: 12px 14px; font-size: 0.75rem; font-weight: 700; color: #64748b; text-transform: uppercase;">Stock</th>
                  <th style="padding: 12px 14px; font-size: 0.75rem; font-weight: 700; color: #64748b; text-transform: uppercase; text-align: right;">Actions</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let p of products" style="border-bottom: 1px solid var(--border-light);">
                  <td style="padding: 12px 14px;">
                    <img [src]="imgSvc.getImage(p)" [alt]="p.name" style="width: 44px; height: 44px; border-radius: 8px; object-fit: cover; border: 1px solid var(--border-color);" />
                  </td>
                  <td style="padding: 12px 14px;">
                    <strong style="color: #0f172a; font-size: 0.95rem;">{{ p.name }}</strong>
                    <div style="color: #94a3b8; font-size: 0.75rem;">ID: #{{ p.id }}</div>
                  </td>
                  <td style="padding: 12px 14px;">
                    <span class="badge badge-primary">{{ p.category }}</span>
                  </td>
                  <td style="padding: 12px 14px; font-weight: 700; color: #0f172a;">
                    {{ p.price | currency }}
                  </td>
                  <td style="padding: 12px 14px;">
                    <span [class]="'badge ' + ((p.quantity ?? 0) > 0 ? 'badge-success' : 'badge-danger')">
                      {{ (p.quantity ?? 0) > 0 ? p.quantity + ' in stock' : 'Out of stock' }}
                    </span>
                  </td>
                  <td style="padding: 12px 14px; text-align: right;">
                    <div style="display: inline-flex; gap: 8px;">
                      <button (click)="startEdit(p)" class="btn btn-secondary btn-sm" title="Edit Product">
                        <i class="fa-solid fa-pen-to-square"></i>
                      </button>
                      <button (click)="remove(p.id)" class="btn btn-danger btn-sm" title="Delete Product">
                        <i class="fa-solid fa-trash"></i>
                      </button>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <!-- TAB 2: Order Approvals -->
      <div *ngIf="activeTab === 'orders'">
        <!-- Action Alert -->
        <div *ngIf="orderActionMessage" class="alert alert-success">
          <i class="fa-solid fa-circle-check"></i>
          <span>{{ orderActionMessage }}</span>
        </div>

        <div class="card" style="padding: 24px; background: #ffffff; margin-bottom: 24px;">
          <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px;">
            <h3 style="font-size: 1.15rem; font-weight: 800; margin: 0;">Order Processing Queue</h3>
            <button (click)="loadAllOrders()" class="btn btn-secondary btn-sm">
              <i class="fa-solid fa-rotate"></i>
              <span>Refresh Orders</span>
            </button>
          </div>

          <div *ngIf="allOrders.length === 0" style="padding: 30px; text-align: center; color: #64748b;">
            No orders found in the system.
          </div>

          <div *ngIf="allOrders.length > 0" style="overflow-x: auto;">
            <table style="width: 100%; border-collapse: collapse; text-align: left;">
              <thead>
                <tr style="background: #f8fafc; border-bottom: 2px solid var(--border-color);">
                  <th style="padding: 12px 14px; font-size: 0.75rem; font-weight: 700; color: #64748b; text-transform: uppercase;">Order</th>
                  <th style="padding: 12px 14px; font-size: 0.75rem; font-weight: 700; color: #64748b; text-transform: uppercase;">Customer</th>
                  <th style="padding: 12px 14px; font-size: 0.75rem; font-weight: 700; color: #64748b; text-transform: uppercase;">Total Billed</th>
                  <th style="padding: 12px 14px; font-size: 0.75rem; font-weight: 700; color: #64748b; text-transform: uppercase;">Status</th>
                  <th style="padding: 12px 14px; font-size: 0.75rem; font-weight: 700; color: #64748b; text-transform: uppercase; text-align: right;">Action</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let ord of allOrders" style="border-bottom: 1px solid var(--border-light);">
                  <td style="padding: 14px; font-weight: 700; color: #0f172a;">#ORD-{{ ord.id }}</td>
                  <td style="padding: 14px; color: #475569;">User Account #{{ ord.userId }}</td>
                  <td style="padding: 14px; font-weight: 700; color: var(--primary);">{{ ord.totalAmount | currency }}</td>
                  <td style="padding: 14px;">
                    <span [class]="'badge ' + (ord.status === 'Paid' ? 'badge-success' : (ord.status === 'Pending' ? 'badge-warning' : 'badge-primary'))">
                      {{ ord.status }}
                    </span>
                  </td>
                  <td style="padding: 14px; text-align: right;">
                    <button
                      *ngIf="ord.status === 'Pending'"
                      (click)="approveOrder(ord)"
                      [disabled]="approvingOrderId === ord.id"
                      class="btn btn-success btn-sm"
                    >
                      <i class="fa-solid fa-check"></i>
                      <span>{{ approvingOrderId === ord.id ? 'Approving...' : 'Approve' }}</span>
                    </button>
                    <span *ngIf="ord.status !== 'Pending'" style="color: #94a3b8; font-size: 0.85rem;">Completed</span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- Order Lookup Tool -->
        <div class="card" style="padding: 24px; background: #ffffff;">
          <h4 style="font-size: 1rem; font-weight: 700; margin-bottom: 12px;">Direct Order Lookup</h4>
          <div style="display: flex; gap: 10px; max-width: 400px;">
            <input type="number" [(ngModel)]="orderId" placeholder="Enter Order ID" class="form-control" />
            <button (click)="findOrder()" class="btn btn-primary">Find</button>
          </div>

          <div *ngIf="orderResult" style="margin-top: 16px; padding: 16px; background: #f8fafc; border-radius: 8px; border: 1px solid var(--border-color);">
            <div *ngIf="orderResult.id" style="display: flex; justify-content: space-between; align-items: center;">
              <div>
                <strong>Order #{{ orderResult.id }}</strong> — Status: <span class="badge badge-primary">{{ orderResult.status }}</span> — Total: <strong>{{ orderResult.totalAmount | currency }}</strong>
              </div>
              <button
                *ngIf="orderResult.status === 'Pending'"
                (click)="approveOrder(orderResult)"
                [disabled]="approvingOrderId === orderResult.id"
                class="btn btn-success btn-sm"
              >
                Approve
              </button>
            </div>
            <div *ngIf="!orderResult.id" style="color: #ef4444;">{{ orderResult.error || 'Order not found' }}</div>
          </div>
        </div>
      </div>

      <!-- TAB 3: Excel / CSV Bulk Import -->
      <div *ngIf="activeTab === 'import'">
        <div class="card" style="padding: 32px; background: #ffffff; max-width: 680px;">
          <div style="display: flex; align-items: center; gap: 14px; margin-bottom: 18px;">
            <div style="width: 48px; height: 48px; border-radius: 12px; background: #dcfce7; color: #15803d; display: flex; align-items: center; justify-content: center; font-size: 1.5rem;">
              <i class="fa-solid fa-file-excel"></i>
            </div>
            <div>
              <h3 style="font-size: 1.3rem; font-weight: 800; margin: 0;">ClosedXML Bulk Product Ingestion</h3>
              <p style="color: #64748b; font-size: 0.85rem; margin: 2px 0 0;">Upload an Excel (.xlsx) or CSV spreadsheet to populate the product database in bulk.</p>
            </div>
          </div>

          <div style="border: 2px dashed var(--border-strong); border-radius: 12px; padding: 32px; text-align: center; background: #f8fafc; margin-bottom: 20px;">
            <i class="fa-solid fa-cloud-arrow-up" style="font-size: 2.5rem; color: #94a3b8; margin-bottom: 12px; display: block;"></i>
            <p style="font-weight: 600; color: #334155; margin-bottom: 8px;">Select spreadsheet file (.xlsx, .xls, .csv)</p>
            <input type="file" (change)="onFile($event)" accept=".xlsx,.xls,.csv" style="display: block; margin: 0 auto; font-size: 0.9rem;" />
          </div>

          <button (click)="upload()" [disabled]="!importFile" class="btn btn-primary btn-lg" style="width: 100%;">
            <i class="fa-solid fa-upload"></i>
            <span>Upload and Ingest File</span>
          </button>

          <div *ngIf="importResult" style="margin-top: 20px; padding: 16px; background: #f8fafc; border-radius: 8px; border: 1px solid var(--border-color);">
            <h5 style="margin-bottom: 8px; font-weight: 700;">Import Server Response:</h5>
            <pre style="font-size: 0.85rem; color: #334155; white-space: pre-wrap;">{{ importResult | json }}</pre>
          </div>
        </div>
      </div>
    </div>
  </div>
  `
})
export class AdminComponent implements OnInit {
  products: Product[] = [];
  allOrders: any[] = [];
  loadingOrders = false;
  activeTab: 'products' | 'orders' | 'import' = 'products';
  editing: Product | null = null;
  form: any = { name: '', description: '', price: 0, category: '', quantity: 0 };
  orderId: number | null = null;
  orderResult: any = null;
  orderActionMessage = '';
  approvingOrderId: number | null = null;
  importFile: File | null = null;
  importResult: any = null;

  constructor(
    public auth: AuthService,
    private productSvc: ProductService,
    private inventorySvc: InventoryService,
    private orderSvc: OrderService,
    public imgSvc: ProductImageService
  ) {}

  ngOnInit(): void {
    this.load();
    if (this.auth.isAdmin()) {
      this.loadAllOrders();
    }
  }

  getPendingOrdersCount(): number {
    return this.allOrders.filter(o => o.status === 'Pending').length;
  }

  load(): void {
    this.productSvc.getAll().subscribe((res: any) => {
      this.products = (res || []).map((r: any) => ({
        id: r.id,
        name: r.name,
        description: r.description,
        price: r.price,
        category: r.category,
        quantity: r.quantity
      }));
    });
  }

  loadAllOrders(): void {
    this.loadingOrders = true;
    this.orderSvc.getAll().subscribe({
      next: (orders) => {
        this.allOrders = orders || [];
        this.loadingOrders = false;
      },
      error: () => {
        this.loadingOrders = false;
      }
    });
  }

  approveOrder(order: any): void {
    this.approvingOrderId = order.id;
    this.orderActionMessage = '';
    this.orderSvc.approve(order.id).subscribe({
      next: () => {
        order.status = 'Paid';
        if (this.orderResult && this.orderResult.id === order.id) {
          this.orderResult.status = 'Paid';
        }
        this.approvingOrderId = null;
        this.orderActionMessage = `Order #${order.id} has been approved successfully!`;
      },
      error: (err) => {
        this.approvingOrderId = null;
        alert('Failed to approve order: ' + (err?.error?.error || err?.message || 'Error'));
      }
    });
  }

  startEdit(p: Product): void {
    this.editing = { ...p } as Product;
    this.form = { ...p };
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  cancel(): void {
    this.editing = null;
    this.form = { name: '', description: '', price: 0, category: '', quantity: 0 };
  }

  save(): void {
    const payload: any = {
      name: this.form.name,
      description: this.form.description,
      price: Number(this.form.price) || 0,
      category: this.form.category,
      quantity: this.form.quantity == null ? undefined : Number(this.form.quantity)
    };

    if (this.editing) {
      this.productSvc.update(this.editing.id, payload).subscribe(() => {
        this.cancel();
        this.load();
      });
    } else {
      this.productSvc.create(payload).subscribe(() => {
        this.cancel();
        this.load();
      });
    }
  }

  onFile(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    const f = input?.files?.[0] || null;
    this.importFile = f;
  }

  upload(): void {
    if (!this.importFile) return;
    this.productSvc.importProducts(this.importFile).subscribe({
      next: (res) => {
        this.importResult = res;
        this.load();
      },
      error: (err) => {
        this.importResult = { error: err?.message || err };
      }
    });
  }

  remove(id: number): void {
    if (!confirm('Are you sure you want to delete this product?')) return;
    this.productSvc.delete(id).subscribe(() => this.load());
  }

  findOrder(): void {
    this.orderResult = null;
    if (!this.orderId) return;
    this.orderSvc.getById(this.orderId).subscribe({
      next: (res) => this.orderResult = res,
      error: () => this.orderResult = { error: 'Order not found or access denied.' }
    });
  }
}
