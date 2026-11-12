import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class ProductImageService {
  // Mapping specific product names to curated, ultra-crisp Unsplash photography
  private readonly productMap: Record<string, string> = {
    'wireless noise-canceling headphones': 'https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=800&auto=format&fit=crop&q=80',
    'ergonomic mechanical keyboard': 'https://images.unsplash.com/photo-1587829741301-dc798b83add3?w=800&auto=format&fit=crop&q=80',
    'ultra hd 4k monitor 27-inch': 'https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?w=800&auto=format&fit=crop&q=80',
    'blue t-shirt': 'https://images.unsplash.com/photo-1521572267360-ee0c2909d518?w=800&auto=format&fit=crop&q=80',
    'red mug': 'https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=800&auto=format&fit=crop&q=80',
    'wireless mouse': 'https://images.unsplash.com/photo-1615663245857-ac93bb7c39e7?w=800&auto=format&fit=crop&q=80',
    'notebook': 'https://images.unsplash.com/photo-1531346878377-a5be20888e57?w=800&auto=format&fit=crop&q=80',
    'running shoes': 'https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=800&auto=format&fit=crop&q=80'
  };

  // Category fallback images
  private readonly categoryMap: Record<string, string> = {
    'electronics': 'https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=800&auto=format&fit=crop&q=80',
    'accessories': 'https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=800&auto=format&fit=crop&q=80',
    'apparel': 'https://images.unsplash.com/photo-1521572267360-ee0c2909d518?w=800&auto=format&fit=crop&q=80',
    'footwear': 'https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=800&auto=format&fit=crop&q=80',
    'home': 'https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=800&auto=format&fit=crop&q=80',
    'stationery': 'https://images.unsplash.com/photo-1531346878377-a5be20888e57?w=800&auto=format&fit=crop&q=80'
  };

  private readonly defaultImage = 'https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=800&auto=format&fit=crop&q=80';

  /**
   * Resolves a photo URL for any product based on its existing imageUrl, name, or category.
   */
  getImage(item: { name?: string; category?: string; imageUrl?: string } | null | undefined): string {
    if (!item) return this.defaultImage;
    if (item.imageUrl && item.imageUrl.trim().length > 0) return item.imageUrl;

    const lowerName = (item.name || '').toLowerCase().trim();
    for (const [key, url] of Object.entries(this.productMap)) {
      if (lowerName.includes(key) || key.includes(lowerName)) {
        return url;
      }
    }

    const lowerCat = (item.category || '').toLowerCase().trim();
    if (this.categoryMap[lowerCat]) {
      return this.categoryMap[lowerCat];
    }

    return this.defaultImage;
  }
}
