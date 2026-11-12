import { Component, OnInit } from '@angular/core';
import { RecommendationService, RecommendedProduct } from '../../services/recommendation.service';
import { ProductImageService } from '../../services/product-image.service';
import { CartService } from '../../services/cart.service';
import { Product } from '../../models/product';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  recommendedItems: RecommendedProduct[] = [];
  loadingRecommendations = false;
  addedMessage = '';

  constructor(
    private recommendationService: RecommendationService,
    public imgSvc: ProductImageService,
    private cartService: CartService
  ) {}

  ngOnInit(): void {
    this.loadRecommendations();
  }

  loadRecommendations(): void {
    this.loadingRecommendations = true;
    this.recommendationService.getRecommendedForYou(4).subscribe({
      next: (items) => {
        this.recommendedItems = items;
        this.loadingRecommendations = false;
      },
      error: () => {
        this.loadingRecommendations = false;
      }
    });
  }

  addToCart(product: Product, event?: Event): void {
    if (event) {
      event.stopPropagation();
      event.preventDefault();
    }
    this.cartService.add(product, 1);
    this.addedMessage = `Added "${product.name}" to your shopping bag!`;
    setTimeout(() => {
      this.addedMessage = '';
    }, 3000);
  }
}
