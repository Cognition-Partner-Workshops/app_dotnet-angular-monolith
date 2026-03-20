/**
 * Application entry point.
 * Bootstraps the Angular standalone application with HTTP client and router providers.
 */
import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { AppComponent } from './app/app.component';
import { routes } from './app/app.routes';

bootstrapApplication(AppComponent, {
  providers: [provideHttpClient(), provideRouter(routes)]
});
