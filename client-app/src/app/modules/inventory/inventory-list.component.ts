import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';

/**
 * Displays a table of all inventory items fetched from the `/api/inventory` endpoint.
 * Shows product name, quantity on hand, reorder level, warehouse location, and last restock date.
 * Rows where `quantityOnHand <= reorderLevel` are highlighted with the `low-stock` CSS class.
 * Lazy-loaded when the user navigates to the `/inventory` route.
 */
@Component({
  selector: 'app-inventory-list',
  standalone: true,
  imports: [CommonModule],
  template: `
    <h2>Inventory</h2>
    <table *ngIf="items.length">
      <thead><tr><th>Product</th><th>On Hand</th><th>Reorder Level</th><th>Location</th><th>Last Restocked</th></tr></thead>
      <tbody>
        <tr *ngFor="let i of items" [class.low-stock]="i.quantityOnHand <= i.reorderLevel">
          <td>{{i.product?.name}}</td><td>{{i.quantityOnHand}}</td><td>{{i.reorderLevel}}</td><td>{{i.warehouseLocation}}</td><td>{{i.lastRestocked | date}}</td>
        </tr>
      </tbody>
    </table>
  `
})
export class InventoryListComponent implements OnInit {
  /** The list of inventory items retrieved from the backend API. */
  items: any[] = [];

  constructor(private http: HttpClient) {}

  /** Fetches all inventory items from the API on component initialization. */
  ngOnInit() { this.http.get<any[]>('/api/inventory').subscribe(data => this.items = data); }
}
