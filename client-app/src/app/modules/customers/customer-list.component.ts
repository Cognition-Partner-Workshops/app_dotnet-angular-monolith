import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
    selector: 'app-customer-list',
    template: `
    <h2>Customers</h2>
    @if (customers.length) {
      <table>
        <thead><tr><th>Name</th><th>Email</th><th>Phone</th><th>City</th></tr></thead>
        <tbody>
          @for (c of customers; track c.email) {
            <tr>
              <td>{{c.name}}</td><td>{{c.email}}</td><td>{{c.phone}}</td><td>{{c.city}}, {{c.state}}</td>
            </tr>
          }
        </tbody>
      </table>
    }
  `
})
export class CustomerListComponent implements OnInit {
  customers: any[] = [];
  constructor(private http: HttpClient) {}
  ngOnInit() { this.http.get<any[]>('/api/customers').subscribe(data => this.customers = data); }
}
