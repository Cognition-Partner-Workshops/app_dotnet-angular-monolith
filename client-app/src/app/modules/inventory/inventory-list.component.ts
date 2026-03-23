import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';

/**
 * Standalone component that displays warehouse inventory stock levels.
 *
 * Fetches inventory data from the `/api/inventory` endpoint on initialization and renders
 * them in a table showing the product name, quantity on hand, reorder level, warehouse
 * location, and last restocked date.
 *
 * Rows where the quantity on hand is at or below the reorder level are highlighted
 * with the `low-stock` CSS class to visually flag items that need replenishment.
 *
 * Uses Angular's `date` pipe for formatting the last restocked timestamp.
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
  /** Array of inventory item objects fetched from the API, each including nested product data. */
  items: any[] = [];

  /** @param http - Angular HTTP client used to communicate with the backend API. */
  constructor(private http: HttpClient) {}

  /** Fetches all inventory items from the backend API when the component initializes. */
  ngOnInit() { this.http.get<any[]>('/api/inventory').subscribe(data => this.items = data); }
}
