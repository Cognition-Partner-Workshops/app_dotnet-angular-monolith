import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ProductListComponent } from './product-list.component';

describe('ProductListComponent', () => {
  let component: ProductListComponent;
  let fixture: ComponentFixture<ProductListComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductListComponent, HttpClientTestingModule],
    }).compileComponents();

    fixture = TestBed.createComponent(ProductListComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should fetch and display products on init', () => {
    const mockProducts = [
      { sku: 'WGT-001', name: 'Widget A', category: 'Widgets', price: 9.99, inventory: { quantityOnHand: 50 } },
    ];

    fixture.detectChanges();
    const req = httpMock.expectOne('/api/products');
    expect(req.request.method).toBe('GET');
    req.flush(mockProducts);

    expect(component.products.length).toBe(1);
    expect(component.products[0].name).toBe('Widget A');
  });

  it('should show empty state when no products', () => {
    fixture.detectChanges();
    const req = httpMock.expectOne('/api/products');
    req.flush([]);

    expect(component.products.length).toBe(0);
  });

  it('should handle API error gracefully', () => {
    fixture.detectChanges();
    const req = httpMock.expectOne('/api/products');
    req.error(new ProgressEvent('error'));

    expect(component.products.length).toBe(0);
  });
});
