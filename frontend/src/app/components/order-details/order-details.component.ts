import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { OrderService } from '../../services/order.service';

@Component({
  selector: 'app-order-details',
  template: `
  <div class="container" style="max-width: 900px; padding: 32px 20px 60px;">
    <!-- Top Nav -->
    <div style="display: flex; align-items: center; justify-content: space-between; margin-bottom: 24px;">
      <div>
        <a routerLink="/orders" style="display: inline-flex; align-items: center; gap: 8px; color: var(--primary); font-weight: 600; font-size: 0.9rem; text-decoration: none; margin-bottom: 8px;">
          <i class="fa-solid fa-arrow-left"></i>
          <span>Back to All Orders</span>
        </a>
        <h1 style="font-size: 2rem; font-weight: 800; color: #0f172a; margin: 0;">Order Summary</h1>
      </div>
      <span *ngIf="order" [class]="'badge ' + (order.status === 'Paid' ? 'badge-success' : (order.status === 'Pending' ? 'badge-warning' : 'badge-primary'))" style="font-size: 0.85rem; padding: 6px 16px;">
        {{ order.status }}
      </span>
    </div>

    <!-- Alerts -->
    <div *ngIf="loading" class="alert alert-info">
      <i class="fa-solid fa-spinner fa-spin"></i>
      <span>Loading order details...</span>
    </div>
    <div *ngIf="error" class="alert alert-danger">
      <i class="fa-solid fa-triangle-exclamation"></i>
      <span>{{ error }}</span>
    </div>
    <div *ngIf="actionMessage" class="alert alert-success">
      <i class="fa-solid fa-circle-check"></i>
      <span>{{ actionMessage }}</span>
    </div>

    <!-- Main Order Details Card -->
    <div *ngIf="order && !loading" class="card" style="padding: 32px; border-radius: 16px; background: #ffffff;">
      <div style="display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid var(--border-color); padding-bottom: 20px; margin-bottom: 24px;">
        <div>
          <span style="font-size: 0.8rem; font-weight: 700; color: #94a3b8; text-transform: uppercase;">Tracking Number</span>
          <h2 style="font-size: 1.5rem; font-weight: 800; color: #0f172a; margin: 4px 0 0;">#ORD-{{ order.id }}</h2>
        </div>
        <div style="text-align: right;">
          <span style="font-size: 0.8rem; font-weight: 700; color: #94a3b8; text-transform: uppercase;">Total Billed</span>
          <div style="font-size: 1.6rem; font-weight: 800; color: var(--primary);">{{ order.totalAmount | currency }}</div>
        </div>
      </div>

      <!-- Order Items Table -->
      <h3 style="font-size: 1rem; font-weight: 700; color: #475569; text-transform: uppercase; letter-spacing: 0.05em; margin-bottom: 14px;">Items in this Shipment</h3>
      <div style="overflow-x: auto; margin-bottom: 28px;">
        <table style="width: 100%; border-collapse: collapse; text-align: left;">
          <thead>
            <tr style="background: #f8fafc; border-bottom: 1px solid var(--border-color);">
              <th style="padding: 12px 16px; font-size: 0.8rem; font-weight: 700; color: #64748b; text-transform: uppercase;">Product ID</th>
              <th style="padding: 12px 16px; font-size: 0.8rem; font-weight: 700; color: #64748b; text-transform: uppercase;">Quantity</th>
              <th style="padding: 12px 16px; font-size: 0.8rem; font-weight: 700; color: #64748b; text-transform: uppercase;">Unit Price</th>
              <th style="padding: 12px 16px; font-size: 0.8rem; font-weight: 700; color: #64748b; text-transform: uppercase; text-align: right;">Subtotal</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let it of order.orderItems" style="border-bottom: 1px solid var(--border-light);">
              <td style="padding: 14px 16px; font-weight: 600; color: #1e293b;">Product #{{ it.productId }}</td>
              <td style="padding: 14px 16px; color: #475569;">{{ it.quantity }}</td>
              <td style="padding: 14px 16px; color: #475569;">{{ it.price | currency }}</td>
              <td style="padding: 14px 16px; font-weight: 700; color: #0f172a; text-align: right;">{{ (it.price * it.quantity) | currency }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Action Button if Pending -->
      <div *ngIf="order.status === 'Pending'" style="padding-top: 20px; border-top: 1px solid var(--border-color); display: flex; justify-content: flex-end;">
        <button (click)="approveOrder()" [disabled]="approving" class="btn btn-success btn-lg">
          <i *ngIf="!approving" class="fa-solid fa-check"></i>
          <i *ngIf="approving" class="fa-solid fa-circle-notch fa-spin"></i>
          <span>{{ approving ? 'Approving Order...' : 'Approve & Settle Payment' }}</span>
        </button>
      </div>
    </div>
  </div>
  `
})
export class OrderDetailsComponent implements OnInit {
  order: any = null;
  loading = false;
  error = '';
  actionMessage = '';
  approving = false;

  constructor(private route: ActivatedRoute, private svc: OrderService) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      this.error = 'No order ID was provided.';
      return;
    }
    this.loadOrder(id);
  }

  loadOrder(id: number): void {
    this.loading = true;
    this.svc.getById(id).subscribe({
      next: (o) => {
        this.order = o;
        this.loading = false;
      },
      error: () => {
        this.error = 'Failed to load order details.';
        this.loading = false;
      }
    });
  }

  approveOrder(): void {
    if (!this.order?.id) return;
    this.approving = true;
    this.actionMessage = '';
    this.svc.approve(this.order.id).subscribe({
      next: () => {
        this.order.status = 'Paid';
        this.approving = false;
        this.actionMessage = `Order #${this.order.id} was approved and marked as Paid successfully!`;
      },
      error: (err) => {
        this.approving = false;
        alert('Failed to approve order: ' + (err?.error?.error || err?.message || 'Error occurred'));
      }
    });
  }
}
