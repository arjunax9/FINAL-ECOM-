import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ProductService } from '../../services/product.service';
import { CartService } from '../../services/cart.service';
import { ProductImageService } from '../../services/product-image.service';
import { Product } from '../../models/product';

@Component({
  selector: 'app-products',
  templateUrl: './products.component.html',
  styleUrls: ['./products.component.css']
})
export class ProductsComponent implements OnInit {
  products: Product[] = [];
  filteredProducts: Product[] = [];
  categories: string[] = ['All', 'Electronics', 'Accessories', 'Apparel', 'Footwear', 'Home', 'Stationery'];
  selectedCategory = 'All';
  searchTerm = '';
  sortBy = 'featured';
  
  loading = false;
  error = '';
  addedNotification = '';

  constructor(
    private svc: ProductService,
    private cart: CartService,
    public imgSvc: ProductImageService,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      if (params['category']) {
        this.selectedCategory = params['category'];
      }
      this.load();
    });
  }

  load(): void {
    this.loading = true;
    this.svc.getAll().subscribe({
      next: p => {
        this.products = p;
        this.applyFilters();
        this.loading = false;
      },
      error: () => {
        this.error = 'Failed to retrieve products. Please ensure services are running.';
        this.loading = false;
      }
    });
  }

  selectCategory(cat: string): void {
    this.selectedCategory = cat;
    this.applyFilters();
  }

  onSearchChange(): void {
    this.applyFilters();
  }

  onSortChange(): void {
    this.applyFilters();
  }

  applyFilters(): void {
    let list = [...this.products];

    // Category filter
    if (this.selectedCategory && this.selectedCategory !== 'All') {
      list = list.filter(p => p.category?.toLowerCase() === this.selectedCategory.toLowerCase());
    }

    // Search filter
    if (this.searchTerm && this.searchTerm.trim() !== '') {
      const term = this.searchTerm.toLowerCase().trim();
      list = list.filter(p =>
        p.name.toLowerCase().includes(term) ||
        (p.description && p.description.toLowerCase().includes(term))
      );
    }

    // Sorting
    if (this.sortBy === 'price-low') {
      list.sort((a, b) => a.price - b.price);
    } else if (this.sortBy === 'price-high') {
      list.sort((a, b) => b.price - a.price);
    } else if (this.sortBy === 'name') {
      list.sort((a, b) => a.name.localeCompare(b.name));
    }

    this.filteredProducts = list;
  }

  addToCart(product: Product, event?: Event): void {
    if (event) {
      event.stopPropagation();
      event.preventDefault();
    }
    this.cart.add(product, 1);
    this.addedNotification = `Added "${product.name}" to your cart!`;
    setTimeout(() => {
      this.addedNotification = '';
    }, 2800);
  }
}
