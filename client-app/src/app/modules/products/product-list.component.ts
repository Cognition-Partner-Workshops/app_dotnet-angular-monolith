import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CurrencyPipe } from '@angular/common';

@Component({
    selector: 'app-product-list',
    imports: [CurrencyPipe],
    template: `
    <h2>Products</h2>
    @if (products.length) {
      <table>
        <thead><tr><th>SKU</th><th>Name</th><th>Category</th><th>Price</th><th>Stock</th></tr></thead>
        <tbody>
          @for (p of products; track p.sku) {
            <tr>
              <td>{{p.sku}}</td><td>{{p.name}}</td><td>{{p.category}}</td><td>{{p.price | currency}}</td><td>{{p.inventory?.quantityOnHand ?? 'N/A'}}</td>
            </tr>
          }
        </tbody>
      </table>
    }
  `
})
export class ProductListComponent implements OnInit {
  products: any[] = [];
  constructor(private http: HttpClient) {}
  ngOnInit() { this.http.get<any[]>('/api/products').subscribe(data => this.products = data); }
}
