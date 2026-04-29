import { routes } from './app.routes';

describe('App Routes', () => {
  it('should have routes defined', () => {
    expect(routes.length).toBeGreaterThan(0);
  });

  it('should redirect empty path to /orders', () => {
    const defaultRoute = routes.find(r => r.path === '');
    expect(defaultRoute).toBeDefined();
    expect(defaultRoute?.redirectTo).toBe('/orders');
  });

  it('should have orders route', () => {
    const route = routes.find(r => r.path === 'orders');
    expect(route).toBeDefined();
    expect(route?.loadComponent).toBeDefined();
  });

  it('should have products route', () => {
    const route = routes.find(r => r.path === 'products');
    expect(route).toBeDefined();
    expect(route?.loadComponent).toBeDefined();
  });

  it('should have customers route', () => {
    const route = routes.find(r => r.path === 'customers');
    expect(route).toBeDefined();
    expect(route?.loadComponent).toBeDefined();
  });

  it('should have inventory route', () => {
    const route = routes.find(r => r.path === 'inventory');
    expect(route).toBeDefined();
    expect(route?.loadComponent).toBeDefined();
  });
});
