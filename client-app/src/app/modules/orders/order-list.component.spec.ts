import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { OrderListComponent } from './order-list.component';

describe('OrderListComponent', () => {
  let component: OrderListComponent;
  let fixture: ComponentFixture<OrderListComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OrderListComponent, HttpClientTestingModule],
    }).compileComponents();

    fixture = TestBed.createComponent(OrderListComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should fetch and display orders on init', () => {
    const mockOrders = [
      { id: 1, customer: { name: 'Test' }, orderDate: '2024-01-01', status: 'Pending', totalAmount: 100 },
    ];

    fixture.detectChanges();
    const req = httpMock.expectOne('/api/orders');
    expect(req.request.method).toBe('GET');
    req.flush(mockOrders);

    expect(component.orders.length).toBe(1);
    expect(component.orders[0].id).toBe(1);
  });

  it('should show empty state when no orders', () => {
    fixture.detectChanges();
    const req = httpMock.expectOne('/api/orders');
    req.flush([]);

    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('p')?.textContent).toContain('No orders yet');
  });

  it('should handle API error gracefully', () => {
    fixture.detectChanges();
    const req = httpMock.expectOne('/api/orders');
    req.error(new ProgressEvent('error'));

    expect(component.orders.length).toBe(0);
  });
});
