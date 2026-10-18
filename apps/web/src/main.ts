import { bootstrapApplication } from '@angular/platform-browser';
import { NavigationEnd, NavigationError, Router } from '@angular/router';
import { filter, take } from 'rxjs';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';
import { migratePreRebrandStorage } from './app/shared/utils/storage-keys';

migratePreRebrandStorage();

/** Fades out the installed-app launch splash from index.html (no-op on the website). */
const hideSplash = () => (window as Window & { __hideSplash?: () => void }).__hideSplash?.();

bootstrapApplication(AppComponent, appConfig)
  .then(appRef => {
    // Wait for the first screen (e.g. /login redirecting to the dashboard) so the splash never reveals a blank page
    appRef.injector.get(Router).events
      .pipe(filter(e => e instanceof NavigationEnd || e instanceof NavigationError), take(1))
      .subscribe(hideSplash);
  })
  .catch((err) => {
    hideSplash();
    console.error(err);
  });
