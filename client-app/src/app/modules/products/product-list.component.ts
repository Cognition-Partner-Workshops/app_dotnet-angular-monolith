import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { forkJoin } from 'rxjs';

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
          <td>{{p.sku}}</td><td>{{p.name}}</td><td>{{p.category}}</td><td>{{p.price | currency}}</td><td>{{p.stock ?? 'N/A'}}</td>
        </tr>
      </tbody>
    </table>
  `
})
export class ProductListComponent implements OnInit {
  products: any[] = [];
  constructor(private http: HttpClient) {}
  ngOnInit() {
    forkJoin([
      this.http.get<any[]>('/api/products'),
      this.http.get<any[]>('/api/inventory')
    ]).subscribe(([products, inventory]) => {
      const stockMap = new Map(inventory.map(i => [i.productId, i.quantityOnHand]));
      this.products = products.map(p => ({ ...p, stock: stockMap.get(p.id) }));
    });
  }
}
