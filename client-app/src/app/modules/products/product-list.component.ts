import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';

/**
 * Displays a table of all products fetched from the `/api/products` endpoint.
 * Shows SKU, name, category, price, and current stock level from the
 * eagerly-loaded inventory relationship.
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
  products: any[] = [];
  constructor(private http: HttpClient) {}

  /** Fetches all products from the backend on component initialization. */
  ngOnInit() { this.http.get<any[]>('/api/products').subscribe(data => this.products = data); }
}
