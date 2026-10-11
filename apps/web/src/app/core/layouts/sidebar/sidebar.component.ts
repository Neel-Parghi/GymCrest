import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MenuItem } from '../../../shared/models/menu-item.model';
import { BrandLogoComponent } from '../../../shared/components/brand-logo/brand-logo.component';

@Component({
  selector: 'app-sidebar',
  imports: [CommonModule, RouterModule, BrandLogoComponent],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss',
})
export class SidebarComponent {

  @Input() menuItems: MenuItem[] = [];

  toggleExpand(item: MenuItem) {
    if (item.children) {
      item.expanded = !item.expanded;
    }
  }

}
