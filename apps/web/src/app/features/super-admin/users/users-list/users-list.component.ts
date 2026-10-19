import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DataGrid } from '../../../../shared/components/data-grid/data-grid.component';
import { AppGridConfig } from '../../../../shared/constants/grid-config';
import { UserService } from '../../../../core/services/user.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { FilterBarComponent } from '../../../../shared/components/filter-bar/filter-bar.component';
import { ConfirmationService } from '../../../../core/services/confirmation.service';
import { CONSTANTS } from '../../../../core/constants/constants';

@Component({
  selector: 'app-users-list',
  standalone: true,
  imports: [CommonModule, DataGrid, FilterBarComponent],
  templateUrl: './users-list.component.html',
  styleUrl: './users-list.component.scss'
})
export class UsersListComponent implements OnInit {
  gridConfig = AppGridConfig['StandaloneUsers'];

  displayData: any[] = [];
  totalItems: number = 0;
  pageSize: number = 10;
  currentPage: number = 1;
  searchQuery: string = '';

  private userService = inject(UserService);
  private notification = inject(NotificationService);
  private confirmation = inject(ConfirmationService);

  ngOnInit(): void {
    this.getUsers();
  }

  getUsers() {
    this.userService.getStandaloneUsers(this.currentPage, this.pageSize, this.searchQuery).subscribe({
      next: (res) => {
        let items = res.data?.items || [];

        this.displayData = items.map((u: any) => {
          return {
            ...u,
            status: u.deletionRequestedOn ? 'Deletion Scheduled' : 'Active'
          };
        });

        this.totalItems = res.data?.totalCount || 0;
      },
      error: (err: any) => {
        this.notification.error(err.error?.message || 'Error loading users.');
      }
    });
  }

  handleAction(event: { action: string, row: any }) {
    if (event.action === CONSTANTS.ACTIONS.DELETE) {
      this.deleteUser(event.row);
    }
  }

  deleteUser(user: any) {
    const name = `${user.firstName ?? ''} ${user.lastName ?? ''}`.trim() || user.email;
    this.confirmation.confirm({
      title: CONSTANTS.CONFIRMATIONS.DELETE_USER_TITLE,
      message: CONSTANTS.CONFIRMATIONS.DELETE_USER_MESSAGE.replace('{name}', name),
      type: 'danger'
    }).then(confirmed => {
      if (!confirmed) return;
      this.userService.deleteUser(user.id).subscribe({
        next: () => {
          this.notification.success(`${name} was deleted.`);
          if (this.displayData.length === 1 && this.currentPage > 1) {
            this.currentPage--;
          }
          this.getUsers();
        },
        error: (err: any) => {
          this.notification.error(err.error?.message || 'Could not delete this user.');
        }
      });
    });
  }

  onFilterChanged(filters: any) {
    this.searchQuery = filters.search || '';
    this.currentPage = 1;
    this.getUsers();
  }

  onPageChanged(page: number) {
    this.currentPage = page;
    this.getUsers();
  }

  onPageSizeChanged(size: number) {
    this.pageSize = size;
    this.currentPage = 1;
    this.getUsers();
  }
}
