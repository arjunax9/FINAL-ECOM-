import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CartService } from '../../services/cart.service';
import { OrderService } from '../../services/order.service';
import { AuthService } from '../../services/auth.service';
import { ProductImageService } from '../../services/product-image.service';

@Component({
  selector: 'app-checkout',
  templateUrl: './checkout.component.html',
  styleUrls: ['./checkout.component.css']
})
export class CheckoutComponent implements OnInit {
  items: any[] = [];
  message = '';
  errorMessage = '';
  total = 0;
  paymentMethod: 'card' | 'paypal' = 'card';
  placing = false;
  successOrderId: number | null = null;

  // Mock shipping form
  shipping = {
    fullName: 'Demo Customer',
    address: '123 Enterprise Blvd, Suite 400',
    city: 'San Francisco',
    postalCode: '94105',
    country: 'United States'
  };

  constructor(
    private cart: CartService,
    private orders: OrderService,
    private auth: AuthService,
    private router: Router,
    public imgSvc: ProductImageService
  ) {}

  ngOnInit(): void {
    this.items = this.cart.getItems();
    this.recalc();
  }

  recalc(): void {
    this.total = this.items.reduce((s, it) => s + (it.product.price * it.quantity), 0);
  }

  place(): void {
    const userId = this.getUserId();
    if (!userId) {
      this.errorMessage = 'Please sign in to complete your checkout.';
      return;
    }

    if (this.items.length === 0) {
      this.errorMessage = 'Your shopping bag is empty.';
      return;
    }

    this.placing = true;
    this.errorMessage = '';
    this.message = '';

    const req = {
      userId: userId,
      items: this.items.map(i => ({ productId: i.product.id, quantity: i.quantity })),
      paymentMethod: this.paymentMethod
    };

    this.orders.create(req).subscribe({
      next: (r: any) => {
        const id = r?.id;
        if (!id) {
          this.errorMessage = 'Order created but no order ID was returned.';
          this.placing = false;
          return;
        }

        // Checkout & Pay pipeline
        this.orders.checkout(id).subscribe({
          next: () => {
            this.orders.pay(id).subscribe({
              next: () => {
                this.placing = false;
                this.successOrderId = id;
                this.message = 'Payment processed and verified successfully!';
                this.cart.clear();
                this.items = [];
                this.recalc();
              },
              error: () => {
                this.errorMessage = 'Payment processing failed. Please verify your payment details.';
                this.placing = false;
              }
            });
          },
          error: () => {
            this.errorMessage = 'Checkout inventory validation failed. One or more items may be out of stock.';
            this.placing = false;
          }
        });
      },
      error: () => {
        this.errorMessage = 'Failed to create order. Please check backend connection.';
        this.placing = false;
      }
    });
  }

  getUserId(): number | null {
    return this.auth.getUserId();
  }
}
