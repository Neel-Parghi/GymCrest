import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';
import { migratePreRebrandStorage } from './app/shared/utils/storage-keys';

migratePreRebrandStorage();

bootstrapApplication(AppComponent, appConfig)
  .catch((err) => console.error(err));
