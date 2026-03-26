import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-low-stock',
  standalone: true,
  imports: [CommonModule],
  template: `
    <h2>Low Stock Items</h2>
    <table *ngIf="items.length">
      <thead>
        <tr>
          <th>Product</th>
          <th>On Hand</th>
          <th>Reorder Level</th>
          <th>Location</th>
          <th>Last Restocked</th>
        </tr>
      </thead>
      <tbody>
        <tr *ngFor="let i of items" class="low-stock">
          <td>{{i.productName}}</td>
          <td>{{i.quantityOnHand}}</td>
          <td>{{i.reorderLevel}}</td>
          <td>{{i.warehouseLocation}}</td>
          <td>{{i.lastRestocked | date}}</td>
        </tr>
      </tbody>
    </table>
    <p *ngIf="!items.length">No low stock items.</p>
  `
})
export class LowStockComponent implements OnInit {
  items: any[] = [];
  constructor(private http: HttpClient) {}
  ngOnInit() {
    this.http.get<any[]>(`${environment.apiUrl}/api/inventory/low-stock`).subscribe(data => this.items = data);
  }
}
