import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-inventory-list',
  standalone: true,
  imports: [CommonModule],
  template: `
    <h2>Inventory</h2>
    <p *ngIf="error" style="color:red">{{error}}</p>
    <table *ngIf="items.length">
      <thead><tr><th>Product</th><th>On Hand</th><th>Reorder Level</th><th>Location</th><th>Last Restocked</th></tr></thead>
      <tbody>
        <tr *ngFor="let i of items" [class.low-stock]="i.quantityOnHand <= i.reorderLevel">
          <td>{{i.productName}}</td><td>{{i.quantityOnHand}}</td><td>{{i.reorderLevel}}</td><td>{{i.warehouseLocation}}</td><td>{{i.lastRestocked | date}}</td>
        </tr>
      </tbody>
    </table>
  `
})
export class InventoryListComponent implements OnInit {
  items: any[] = [];
  error = '';
  constructor(private http: HttpClient) {}
  ngOnInit() {
    const baseUrl = environment.inventoryServiceUrl;
    this.http.get<any[]>(`${baseUrl}/api/inventory`).subscribe({
      next: data => this.items = data,
      error: () => {
        this.error = 'Could not reach inventory service. Falling back to monolith proxy.';
        this.http.get<any[]>('/api/inventory').subscribe(data => this.items = data);
      }
    });
  }
}
