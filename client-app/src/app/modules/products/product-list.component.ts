import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';

/**
 * Displays a table of all products fetched from the `/api/products` endpoint.
 * Shows SKU, name, category, price, and current stock level.
 * Lazy-loaded when the user navigates to the `/products` route.
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
  /** The list of products retrieved from the backend API. */
  products: any[] = [];

  constructor(private http: HttpClient) {}

  /** Fetches all products (with inventory data) from the API on component initialization. */
  ngOnInit() { this.http.get<any[]>('/api/products').subscribe(data => this.products = data); }
}
