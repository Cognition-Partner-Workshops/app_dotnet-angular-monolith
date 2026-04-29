import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { CustomerListComponent } from './customer-list.component';

describe('CustomerListComponent', () => {
  let component: CustomerListComponent;
  let fixture: ComponentFixture<CustomerListComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CustomerListComponent, HttpClientTestingModule],
    }).compileComponents();

    fixture = TestBed.createComponent(CustomerListComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should fetch and display customers on init', () => {
    const mockCustomers = [
      { name: 'Acme Corp', email: 'orders@acme.com', phone: '555-0100', city: 'Springfield', state: 'IL' },
    ];

    fixture.detectChanges();
    const req = httpMock.expectOne('/api/customers');
    expect(req.request.method).toBe('GET');
    req.flush(mockCustomers);

    expect(component.customers.length).toBe(1);
    expect(component.customers[0].name).toBe('Acme Corp');
  });

  it('should show empty state when no customers', () => {
    fixture.detectChanges();
    const req = httpMock.expectOne('/api/customers');
    req.flush([]);

    expect(component.customers.length).toBe(0);
  });

  it('should handle API error gracefully', () => {
    fixture.detectChanges();
    const req = httpMock.expectOne('/api/customers');
    req.error(new ProgressEvent('error'));

    expect(component.customers.length).toBe(0);
  });
});
