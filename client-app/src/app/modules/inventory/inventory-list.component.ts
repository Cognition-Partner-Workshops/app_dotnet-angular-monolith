import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-inventory-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <h2>Inventory</h2>
    <div>
      <button (click)="showLowStock = !showLowStock">
        {{ showLowStock ? 'Show All' : 'Show Low Stock' }}
      </button>
    </div>
    <table *ngIf="displayItems.length">
      <thead>
        <tr>
          <th>Product</th>
          <th>SKU</th>
          <th>On Hand</th>
          <th>Reorder Level</th>
          <th>Location</th>
          <th>Last Restocked</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
        <tr *ngFor="let i of displayItems" [class.low-stock]="i.quantityOnHand <= i.reorderLevel">
          <td>{{i.productName}}</td>
          <td>{{i.productSku}}</td>
          <td>{{i.quantityOnHand}}</td>
          <td>{{i.reorderLevel}}</td>
          <td>{{i.warehouseLocation}}</td>
          <td>{{i.lastRestocked | date}}</td>
          <td>
            <input type="number" [(ngModel)]="restockQuantities[i.productId]" min="1" placeholder="Qty" style="width:60px">
            <button (click)="restock(i.productId)">Restock</button>
          </td>
        </tr>
      </tbody>
    </table>
    <p *ngIf="!displayItems.length">No inventory items found.</p>
  `
})
export class InventoryListComponent implements OnInit {
  items: any[] = [];
  lowStockItems: any[] = [];
  showLowStock = false;
  restockQuantities: { [key: number]: number } = {};

  get displayItems() {
    return this.showLowStock ? this.lowStockItems : this.items;
  }

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.loadAll();
    this.loadLowStock();
  }

  loadAll() {
    this.http.get<any[]>('/api/inventory').subscribe(data => this.items = data);
  }

  loadLowStock() {
    this.http.get<any[]>('/api/inventory/low-stock').subscribe(data => this.lowStockItems = data);
  }

  restock(productId: number) {
    const qty = this.restockQuantities[productId] || 0;
    if (qty <= 0) return;
    this.http.post('/api/inventory/product/' + productId + '/restock', { quantity: qty })
      .subscribe(() => {
        this.restockQuantities[productId] = 0;
        this.loadAll();
        this.loadLowStock();
      });
  }
}
