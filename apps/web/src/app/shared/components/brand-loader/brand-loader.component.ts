import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-brand-loader',
  standalone: true,
  templateUrl: './brand-loader.component.html',
  styleUrl: './brand-loader.component.scss'
})
export class BrandLoaderComponent {
  @Input() message = '';
}
