import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CartService } from '../../services/cart.service';
import { ProductImageService } from '../../services/product-image.service';

@Component({
  selector: 'app-cart',
  templateUrl: './cart.component.html',
  styleUrls: ['./cart.component.css']
})
export class CartComponent implements OnInit {
  items: any[] = [];
  total = 0;

  constructor(
    private cart: CartService,
    private router: Router,
    public imgSvc: ProductImageService
  ) {}

  ngOnInit(): void {
    this.items = this.cart.getItems();
    this.recalc();
  }

  remove(i: number): void {
    this.items.splice(i, 1);
    this.cart.save(this.items);
    this.recalc();
  }

  increase(i: number): void {
    this.items[i].quantity += 1;
    this.cart.save(this.items);
    this.recalc();
  }

  decrease(i: number): void {
    if (this.items[i].quantity > 1) {
      this.items[i].quantity -= 1;
    } else {
      this.remove(i);
      return;
    }
    this.cart.save(this.items);
    this.recalc();
  }

  clearCart(): void {
    this.items = [];
    this.cart.clear();
    this.recalc();
  }

  recalc(): void {
    this.total = this.items.reduce((s, it) => s + (it.product.price * it.quantity), 0);
  }

  checkout(): void {
    this.router.navigate(['/checkout']);
  }
}
