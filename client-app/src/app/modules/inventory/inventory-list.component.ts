import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-inventory-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <h2>Inventory</h2>
    <p *ngIf="error" style="color:red">{{error}}</p>
    <table *ngIf="items.length">
      <thead><tr><th>Product</th><th>On Hand</th><th>Reorder Level</th><th>Location</th><th>Last Restocked</th><th>Actions</th></tr></thead>
      <tbody>
        <tr *ngFor="let i of items" [class.low-stock]="i.quantityOnHand <= i.reorderLevel">
          <td>{{i.productName}}</td><td>{{i.quantityOnHand}}</td><td>{{i.reorderLevel}}</td><td>{{i.warehouseLocation}}</td><td>{{i.lastRestocked | date}}</td>
          <td>
            <input type="number" [(ngModel)]="restockQuantities[i.productId]" placeholder="Qty" min="1" style="width:60px">
            <button (click)="restock(i.productId)">Restock</button>
          </td>
        </tr>
      </tbody>
    </table>
    <p *ngIf="!items.length && !error">No inventory items found.</p>
  `
})
export class InventoryListComponent implements OnInit {
  items: any[] = [];
  restockQuantities: { [key: number]: number } = {};
  error = '';

  constructor(private http: HttpClient) {}

  ngOnInit() { this.loadInventory(); }

  loadInventory() {
    this.error = '';
    this.http.get<any[]>(`${environment.inventoryServiceUrl}/api/inventory`).subscribe({
      next: data => this.items = data,
      error: () => {
        this.error = 'Unable to reach inventory service. Falling back to monolith API.';
        this.http.get<any[]>('/api/inventory').subscribe(data => this.items = data);
      }
    });
  }

  restock(productId: number) {
    const qty = this.restockQuantities[productId];
    if (!qty || qty <= 0) return;
    this.http.post(`${environment.inventoryServiceUrl}/api/inventory/product/${productId}/restock`, { quantity: qty })
      .subscribe(() => {
        this.restockQuantities[productId] = 0;
        this.loadInventory();
      });
  }
}
