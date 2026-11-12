import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { Product } from '../models/product';

@Injectable({ providedIn: 'root' })
export class CartService {
  private key = 'cart_items';
  private countSubject = new BehaviorSubject<number>(this.calculateTotalCount());
  public count$: Observable<number> = this.countSubject.asObservable();

  private calculateTotalCount(): number {
    const raw = localStorage.getItem(this.key);
    if (!raw) return 0;
    try {
      const items: { product: Product, quantity: number }[] = JSON.parse(raw);
      return items.reduce((acc, curr) => acc + (curr.quantity || 1), 0);
    } catch {
      return 0;
    }
  }

  getItems(): { product: Product, quantity: number }[] {
    const raw = localStorage.getItem(this.key);
    return raw ? JSON.parse(raw) : [];
  }

  save(items: { product: Product, quantity: number }[]) {
    localStorage.setItem(this.key, JSON.stringify(items));
    this.countSubject.next(this.calculateTotalCount());
  }

  add(product: Product, qty = 1) {
    const items = this.getItems();
    const existing = items.find(i => i.product.id === product.id);
    if (existing) {
      existing.quantity += qty;
    } else {
      items.push({ product, quantity: qty });
    }
    this.save(items);
  }

  clear() {
    localStorage.removeItem(this.key);
    this.countSubject.next(0);
  }
}
