import { Component, OnInit } from '@angular/core';
import { OrderService } from '../../services/order.service';
import { AuthService } from '../../services/auth.service';
import { SignalRService } from '../../services/signalr.service';

@Component({
  selector: 'app-orders',
  templateUrl: './orders.component.html',
  styleUrls: ['./orders.component.css']
})
export class OrdersComponent implements OnInit {
  orders: any[] = [];
  loading = false;
  error = '';
  actionMessage = '';
  approvingId: number | null = null;

  constructor(
    private svc: OrderService,
    private auth: AuthService,
    private signalR: SignalRService
  ) {}

  ngOnInit(): void {
    this.loadOrders();
    this.signalR.startConnection();
    this.signalR.on('OrderStatusChanged', (data: any) => {
      const order = this.orders.find(o => o.id === data?.orderId);
      if (order) {
        order.status = data.newStatus;
        this.actionMessage = `Live Update: Order #${data.orderId} status changed to ${data.newStatus}!`;
        setTimeout(() => this.actionMessage = '', 4000);
      }
    });
  }

  loadOrders(): void {
    const uid = this.auth.getUserId();
    if (!uid) {
      this.error = 'Please login to see your orders';
      return;
    }
    this.loading = true;
    this.svc.getByUser(uid).subscribe({
      next: (o) => {
        this.orders = o;
        this.loading = false;
      },
      error: () => {
        this.error = 'Failed to load orders. Please ensure Order Service is running.';
        this.loading = false;
      }
    });
  }

  approveOrder(order: any): void {
    this.approvingId = order.id;
    this.actionMessage = '';
    this.svc.approve(order.id).subscribe({
      next: () => {
        order.status = 'Paid';
        this.approvingId = null;
        this.actionMessage = `Order #${order.id} has been approved and marked as Paid successfully!`;
      },
      error: (err) => {
        this.approvingId = null;
        alert('Failed to approve order: ' + (err?.error?.error || err?.message || 'Error occurred'));
      }
    });
  }
}
