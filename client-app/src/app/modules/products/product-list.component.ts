import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';

/**
 * Standalone component that displays the product catalog.
 *
 * Fetches product data from the `/api/products` endpoint on initialization and renders
 * them in a table showing the SKU, name, category, price, and current stock level.
 * Stock level is sourced from the product's associated inventory record and shows 'N/A'
 * if no inventory record exists.
 *
 * Uses Angular's `currency` pipe for price formatting.
 */
@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule],
  template: `
    <h2>Products</h2>
    <table *ngIf="products.length">
      <thead><tr><th>SKU</th><th>Name</th><th>Category</th><th>Price</th><th>Stock</th></tr></thead>
      <tbody>
        <tr *ngFor="let p of products">
          <td>{{p.sku}}</td><td>{{p.name}}</td><td>{{p.category}}</td><td>{{p.price | currency}}</td><td>{{p.inventory?.quantityOnHand ?? 'N/A'}}</td>
        </tr>
      </tbody>
    </table>
  `
})
export class ProductListComponent implements OnInit {
  /** Array of product objects fetched from the API, each including nested inventory data. */
  products: any[] = [];

  /** @param http - Angular HTTP client used to communicate with the backend API. */
  constructor(private http: HttpClient) {}

  /** Fetches all products (with inventory) from the backend API when the component initializes. */
  ngOnInit() { this.http.get<any[]>('/api/products').subscribe(data => this.products = data); }
}
