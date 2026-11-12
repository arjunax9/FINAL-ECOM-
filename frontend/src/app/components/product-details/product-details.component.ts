import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import { ProductService } from '../../services/product.service';
import { CartService } from '../../services/cart.service';
import { ProductImageService } from '../../services/product-image.service';
import { RecommendationService } from '../../services/recommendation.service';
import { Product } from '../../models/product';

@Component({
  selector: 'app-product-details',
  templateUrl: './product-details.component.html',
  styleUrls: ['./product-details.component.css']
})
export class ProductDetailsComponent implements OnInit, OnDestroy {
  product: Product | null = null;
  loading = false;
  error = '';
  quantity = 1;
  addedMessage = '';

  // Customers also bought recommendations
  customersAlsoBought: Product[] = [];
  loadingAlsoBought = false;

  private routeSub?: Subscription;

  constructor(
    private route: ActivatedRoute,
    private svc: ProductService,
    private cart: CartService,
    public imgSvc: ProductImageService,
    private recommendationService: RecommendationService
  ) {}

  ngOnInit(): void {
    // Subscribe to paramMap so clicking recommended items re-loads the product view reactively
    this.routeSub = this.route.paramMap.subscribe(params => {
      const id = Number(params.get('id'));
      if (id) {
        this.quantity = 1;
        this.load(id);
      }
    });
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
  }

  load(id: number): void {
    this.loading = true;
    this.error = '';
    this.svc.getById(id).subscribe({
      next: p => {
        this.product = p;
        this.loading = false;

        // 1. Track browsing pattern for this product view
        this.recommendationService.trackProductView(p);

        // 2. Fetch "Customers also bought" recommendations
        this.loadAlsoBought(p.id, p.category);
      },
      error: () => {
        this.error = 'Failed to load product details.';
        this.loading = false;
      }
    });
  }

  loadAlsoBought(productId: number, category: string): void {
    this.loadingAlsoBought = true;
    this.recommendationService.getCustomersAlsoBought(productId, category, 4).subscribe({
      next: (items) => {
        this.customersAlsoBought = items;
        this.loadingAlsoBought = false;
      },
      error: () => {
        this.loadingAlsoBought = false;
      }
    });
  }

  increaseQty(): void {
    const max = this.product?.quantity ?? 99;
    if (this.quantity < max) {
      this.quantity++;
    }
  }

  decreaseQty(): void {
    if (this.quantity > 1) {
      this.quantity--;
    }
  }

  addToCart(): void {
    if (!this.product) return;
    this.cart.add(this.product, this.quantity);
    this.addedMessage = `Added ${this.quantity} item(s) to your cart!`;
    setTimeout(() => {
      this.addedMessage = '';
    }, 3000);
  }

  addAlsoBoughtToCart(item: Product, event?: Event): void {
    if (event) {
      event.stopPropagation();
      event.preventDefault();
    }
    this.cart.add(item, 1);
    this.addedMessage = `Added "${item.name}" to your cart!`;
    setTimeout(() => {
      this.addedMessage = '';
    }, 3000);
  }
}
