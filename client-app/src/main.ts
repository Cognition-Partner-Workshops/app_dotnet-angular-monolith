import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { AppComponent } from './app/app.component';
import { routes } from './app/app.routes';

/**
 * Application entry point.
 * Bootstraps the Angular SPA as a standalone component with HTTP and routing providers.
 * The app is served by the .NET backend via the wwwroot static-files middleware.
 */
bootstrapApplication(AppComponent, {
  providers: [provideHttpClient(), provideRouter(routes)]
});
