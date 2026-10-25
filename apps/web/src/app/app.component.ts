import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { inject as injectAnalytics } from '@vercel/analytics';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  protected readonly title = signal('web');

  constructor() {
    // Initialize Vercel Analytics (browser only to avoid SSR issues)
    if (typeof window !== 'undefined') {
      injectAnalytics();
    }
  }
}
