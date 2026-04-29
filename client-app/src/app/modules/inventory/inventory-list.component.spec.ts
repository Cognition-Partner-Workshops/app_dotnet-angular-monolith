import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { InventoryListComponent } from './inventory-list.component';

describe('InventoryListComponent', () => {
  let component: InventoryListComponent;
  let fixture: ComponentFixture<InventoryListComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InventoryListComponent, HttpClientTestingModule],
    }).compileComponents();

    fixture = TestBed.createComponent(InventoryListComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should fetch and display inventory items on init', () => {
    const mockItems = [
      { product: { name: 'Widget A' }, quantityOnHand: 50, reorderLevel: 10, warehouseLocation: 'A-01', lastRestocked: '2024-01-01' },
    ];

    fixture.detectChanges();
    const req = httpMock.expectOne('/api/inventory');
    expect(req.request.method).toBe('GET');
    req.flush(mockItems);

    expect(component.items.length).toBe(1);
    expect(component.items[0].quantityOnHand).toBe(50);
  });

  it('should show empty state when no inventory', () => {
    fixture.detectChanges();
    const req = httpMock.expectOne('/api/inventory');
    req.flush([]);

    expect(component.items.length).toBe(0);
  });

  it('should handle API error gracefully', () => {
    fixture.detectChanges();
    const req = httpMock.expectOne('/api/inventory');
    req.error(new ProgressEvent('error'));

    expect(component.items.length).toBe(0);
  });
});
