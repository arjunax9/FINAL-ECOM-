import { Injectable } from '@angular/core';
import { Observable, of, forkJoin } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { ProductService } from './product.service';
import { OrderService } from './order.service';
import { AuthService } from './auth.service';
import { Product } from '../models/product';

export interface RecommendedProduct {
  product: Product;
  reason: string;
}

interface BrowsingRecord {
  productId: number;
  category: string;
  timestamp: number;
}

@Injectable({
  providedIn: 'root'
})
export class RecommendationService {
  private readonly browsingKey = 'ecommerce_browsed_history';

  // Map complementary categories that customers frequently cross-shop
  private readonly complementaryCategories: Record<string, string[]> = {
    'electronics': ['accessories'],
    'accessories': ['electronics'],
    'apparel': ['footwear', 'accessories'],
    'footwear': ['apparel', 'accessories'],
    'home': ['stationery'],
    'stationery': ['home', 'accessories']
  };

  constructor(
    private productService: ProductService,
    private orderService: OrderService,
    private authService: AuthService
  ) {}

  /**
   * Tracks when a user views a product to establish browsing patterns.
   */
  trackProductView(product: Product): void {
    if (!product || !product.id) return;

    try {
      const history = this.getBrowsingHistory();
      // Filter out previous occurrences of the same product to move it to the top
      const filtered = history.filter(item => item.productId !== product.id);
      
      const newRecord: BrowsingRecord = {
        productId: product.id,
        category: (product.category || '').trim().toLowerCase(),
        timestamp: Date.now()
      };

      // Keep recent 20 browsing records
      filtered.unshift(newRecord);
      const trimmed = filtered.slice(0, 20);
      localStorage.setItem(this.browsingKey, JSON.stringify(trimmed));
    } catch (e) {
      console.warn('Could not store browsing history', e);
    }
  }

  /**
   * Gets stored browsing records.
   */
  getBrowsingHistory(): BrowsingRecord[] {
    try {
      const raw = localStorage.getItem(this.browsingKey);
      return raw ? JSON.parse(raw) : [];
    } catch {
      return [];
    }
  }

  /**
   * Provides personalized product recommendations for the homepage.
   * Logic bases scores on:
   * 1. Past order history (highest weight: +4 per order in category).
   * 2. Browsing patterns (weight: +2 per view in category, +1.5 for recent).
   * 3. Fallback to top-stocked / diverse catalog items for cold-start visitors.
   */
  getRecommendedForYou(limit: number = 4): Observable<RecommendedProduct[]> {
    const products$ = this.productService.getAll().pipe(catchError(() => of([] as Product[])));
    
    const userId = this.authService.getUserId();
    const orders$ = (userId && this.authService.isLoggedIn())
      ? this.orderService.getByUser(userId).pipe(catchError(() => of([] as any[])))
      : of([] as any[]);

    return forkJoin({
      allProducts: products$,
      userOrders: orders$
    }).pipe(
      map(({ allProducts, userOrders }) => {
        if (!allProducts || allProducts.length === 0) return [];

        const browsing = this.getBrowsingHistory();
        const purchasedProductIds = new Set<number>();
        const categoryOrderCounts: Record<string, number> = {};

        // 1. Analyze Order History
        if (Array.isArray(userOrders)) {
          for (const order of userOrders) {
            const items = order.orderItems || order.items || [];
            for (const item of items) {
              const pId = Number(item.productId);
              if (pId) {
                purchasedProductIds.add(pId);
                // Find category of purchased product
                const matchedProduct = allProducts.find(p => p.id === pId);
                if (matchedProduct && matchedProduct.category) {
                  const cat = matchedProduct.category.toLowerCase().trim();
                  categoryOrderCounts[cat] = (categoryOrderCounts[cat] || 0) + 1;
                }
              }
            }
          }
        }

        // 2. Analyze Browsing Patterns
        const categoryBrowseCounts: Record<string, number> = {};
        for (const item of browsing) {
          if (item.category) {
            categoryBrowseCounts[item.category] = (categoryBrowseCounts[item.category] || 0) + 1;
          }
        }

        const hasOrderHistory = Object.keys(categoryOrderCounts).length > 0;
        const hasBrowsingHistory = browsing.length > 0;

        // 3. Score every available product
        const scored = allProducts.map(product => {
          let score = 0;
          let reason = 'Trending Pick';
          const pCat = (product.category || '').toLowerCase().trim();

          // Order-based affinity
          if (categoryOrderCounts[pCat]) {
            score += categoryOrderCounts[pCat] * 5;
            reason = `Because you love ${product.category}`;
          }

          // Browsing-based affinity
          if (categoryBrowseCounts[pCat]) {
            score += categoryBrowseCounts[pCat] * 2.5;
            if (!categoryOrderCounts[pCat]) {
              reason = `Inspired by your browsing in ${product.category}`;
            }
          }

          // Slight demotion if exact item was already bought (encourage new item discovery)
          if (purchasedProductIds.has(product.id)) {
            score -= 2.0;
          }

          // Bonus if user browsed this specific product recently
          if (browsing.some(b => b.productId === product.id)) {
            score += 1.5;
            if (!categoryOrderCounts[pCat]) {
              reason = 'Recently Viewed';
            }
          }

          // In-stock bonus
          if ((product.quantity ?? 0) > 0) {
            score += 2;
          }

          // Cold start fallback: variety & stock-based
          if (!hasOrderHistory && !hasBrowsingHistory) {
            score = (product.quantity ?? 0) > 0 ? 5 : 1;
            reason = 'Featured Recommendation';
          }

          return {
            product,
            score,
            reason
          };
        });

        // 4. Sort by highest score descending
        scored.sort((a, b) => b.score - a.score);

        return scored.slice(0, limit).map(item => ({
          product: item.product,
          reason: item.reason
        }));
      })
    );
  }

  /**
   * Provides "Customers also bought" recommendations for a specific product detail view.
   * Simple logic:
   * - Filters out the current product.
   * - Prioritizes products in the same category (+10 points).
   * - Prioritizes complementary categories (+5 points) e.g., Electronics <-> Accessories.
   * - Rewards in-stock availability (+2 points).
   */
  getCustomersAlsoBought(currentProductId: number, currentCategory: string, limit: number = 4): Observable<Product[]> {
    return this.productService.getAll().pipe(
      catchError(() => of([] as Product[])),
      map(allProducts => {
        if (!allProducts || allProducts.length === 0) return [];

        const catKey = (currentCategory || '').toLowerCase().trim();
        const complementary = this.complementaryCategories[catKey] || [];

        // Exclude current product
        const candidates = allProducts.filter(p => p.id !== currentProductId);

        const scored = candidates.map(product => {
          let score = 0;
          const pCat = (product.category || '').toLowerCase().trim();

          if (pCat === catKey) {
            score += 10; // Same category co-purchase
          } else if (complementary.includes(pCat)) {
            score += 5; // Complementary cross-category purchase
          } else {
            score += 1;
          }

          // In-stock availability preference
          if ((product.quantity ?? 0) > 0) {
            score += 2;
          }

          return { product, score };
        });

        scored.sort((a, b) => b.score - a.score);

        return scored.slice(0, limit).map(s => s.product);
      })
    );
  }
}
