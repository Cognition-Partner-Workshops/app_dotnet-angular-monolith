import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { DatePipe } from '@angular/common';

@Component({
    selector: 'app-inventory-list',
    imports: [DatePipe],
    template: `
    <h2>Inventory</h2>
    @if (items.length) {
      <table>
        <thead><tr><th>Product</th><th>On Hand</th><th>Reorder Level</th><th>Location</th><th>Last Restocked</th></tr></thead>
        <tbody>
          @for (i of items; track i.id) {
            <tr [class.low-stock]="i.quantityOnHand <= i.reorderLevel">
              <td>{{i.product?.name}}</td><td>{{i.quantityOnHand}}</td><td>{{i.reorderLevel}}</td><td>{{i.warehouseLocation}}</td><td>{{i.lastRestocked | date}}</td>
            </tr>
          }
        </tbody>
      </table>
    }
  `
})
export class InventoryListComponent implements OnInit {
  items: any[] = [];
  constructor(private http: HttpClient) {}
  ngOnInit() { this.http.get<any[]>('/api/inventory').subscribe(data => this.items = data); }
}
