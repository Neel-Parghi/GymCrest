import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DataGrid } from '../../../../shared/components/data-grid/data-grid.component';
import { AppGridConfig } from '../../../../shared/constants/grid-config';
import { AddOwnerModalComponent } from '../add-owner-modal/add-owner-modal.component';
import { FilterBarComponent } from '../../../../shared/components/filter-bar/filter-bar.component';
import { FilterConfig } from '../../../../shared/models/filter.model';
import { UserService } from '../../../../core/services/user.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { GymService } from '../../../../core/services/gym.service';
import { ApiResponse } from '../../../../shared/models/api-response.model';
import { GymOwnerResponse } from '../../../../shared/models/gym.model';
import { OwnerDetailsDrawerComponent } from '../owner-details-drawer/owner-details-drawer.component';
import { CONSTANTS } from '../../../../core/constants/constants';
import { ConfirmationService } from '../../../../core/services/confirmation.service';

@Component({
  selector: 'app-gym-owners',
  standalone: true,
  imports: [CommonModule, DataGrid, AddOwnerModalComponent, OwnerDetailsDrawerComponent, FilterBarComponent],
  templateUrl: './gym-owners.component.html',
  styleUrl: './gym-owners.component.scss',
})
export class GymOwners implements OnInit {
  gridConfig = AppGridConfig['GymOwners'];
  isAddOwnerModalOpen = false;
  isViewDrawerOpen = false;
  isEditMode: boolean = false;

  // Data state
  originalData: GymOwnerResponse[] = [];
  filteredData: GymOwnerResponse[] = [];
  displayData: GymOwnerResponse[] = [];

  // Pagination state
  totalItems = 0;
  pageSize = 10;
  currentPage = 1;

  // Filter configuration
  filterConfigs: FilterConfig[] = [
    {
      key: 'status',
      label: 'Status',
      options: [
        { label: 'Active', value: 'Active' },
        { label: 'Pending', value: 'Pending' },
        { label: 'Inactive', value: 'Inactive' }
      ]
    }
  ];

  selectedOwners: GymOwnerResponse[] = [];
  selectedContext: GymOwnerResponse | null = null;

  private userService = inject(UserService);
  private gymService = inject(GymService);
  private notification = inject(NotificationService);
  private confirmation = inject(ConfirmationService);

  ngOnInit(): void {
    this.getGymOwners();
  }

  getGymOwners(search: string = '') {
    this.gymService.getGymOwnersList(this.currentPage, this.pageSize, search).subscribe({
      next: (res) => {
        const items = res.data.items.map(owner => ({
          ...owner,
          gymNames: owner.gyms?.length ? owner.gyms.map(g => g.gymName).join(', ') : CONSTANTS.UI_LABELS.NO_GYM_YET
        }));
        this.displayData = items;
        this.totalItems = res.data.totalCount;
        this.filteredData = items;
      },
      error: (err: any) => {
        this.notification.error(err.error?.message || CONSTANTS.GYM_OWNER_LOAD_ERROR_MESSAGE);
      }
    });
  }

  applyFilters(filters: any) {
    this.currentPage = 1;
    this.getGymOwners(filters.search);
  }

  updateDisplayData() {
    const startIndex = (this.currentPage - 1) * this.pageSize;
    const endIndex = startIndex + this.pageSize;
    this.displayData = this.filteredData.slice(startIndex, endIndex);
  }

  onFilterChanged(filters: any) {
    this.applyFilters(filters);
  }

  onPageChanged(page: number) {
    this.currentPage = page;
    this.getGymOwners();
  }

  onPageSizeChanged(size: number) {
    this.pageSize = size;
    this.currentPage = 1;
    this.getGymOwners();
  }

  openDrawer(rowInfo: GymOwnerResponse, action: string) {
    this.selectedContext = rowInfo;
    this.isViewDrawerOpen = true;
    this.isEditMode = action === CONSTANTS.ACTIONS.EDIT;
  }

  handleAction(event: { action: string, row: GymOwnerResponse }) {
    if (event.action === CONSTANTS.ACTIONS.EDIT || event.action === CONSTANTS.ACTIONS.VIEW || event.action === CONSTANTS.ACTIONS.ROW_CLICK) {
      this.openDrawer(event.row, event.action);
      return;
    }
    if (event.action === CONSTANTS.ACTIONS.RE_INVITE) {
      this.reInvite(event.row.id);
    }
    if (event.action === CONSTANTS.ACTIONS.DELETE) {
      if (event.row.gymsOwned > 0) {
        this.notification.error(CONSTANTS.GYM_OWNER_DELETE_VALIDATION_MESSAGE);
        return;
      }
      this.deleteGymOwner(event.row.id);
    }
  }

  deleteGymOwner(ownerId: string) {
    this.confirmation.confirm({
      title: CONSTANTS.CONFIRMATIONS.DELETE_OWNER_TITLE,
      message: CONSTANTS.CONFIRMATIONS.DELETE_OWNER_MESSAGE,
      type: 'danger'
    }).then(confirmed => {
      if (confirmed) {
        this.gymService.deleteGymOwner(ownerId).subscribe({
          next: (res: any) => {
            const data = res?.data || res;

            if (data?.Success === false || data?.success === false) {
              const message = (data?.Message || data?.message) === 'OWNER_HAS_GYMS'
                ? CONSTANTS.GYM_OWNER_DELETE_VALIDATION_MESSAGE
                : (data?.Message || data?.message || CONSTANTS.GYM_OWNER_DELETE_ERROR_MESSAGE);

              this.notification.error(message);
              return;
            }

            this.notification.success(CONSTANTS.GYM_OWNER_DELETE_SUCCESS_MESSAGE);
            this.getGymOwners();
          },
          error: (err) => {
            this.notification.error(err.error?.message || CONSTANTS.GYM_OWNER_DELETE_ERROR_MESSAGE);
          }
        });
      }
    });
  }

  updateGymOwner(owner: any) {

    this.gymService.updateGymOwner(owner.id, owner).subscribe({
      next: (res: ApiResponse<unknown>) => {
        this.notification.success(CONSTANTS.GYM_OWNER_UPDATE_SUCCESS_MESSAGE);
        this.isViewDrawerOpen = false;
        this.getGymOwners();
      },
      error: (err) => {
        this.notification.error(err.error?.message || CONSTANTS.GYM_OWNER_UPDATE_ERROR_MESSAGE);
      }
    })

  }

  reInvite(ownerId: string) {
    this.userService.reInviteOwner(ownerId).subscribe({
      next: () => {
        this.notification.success(CONSTANTS.GYM_OWNER_RE_INVITE_SUCCESS_MESSAGE);
      },
      error: (err) => {
        this.notification.error(err.error?.message || CONSTANTS.GYM_OWNER_RE_INVITE_ERROR_MESSAGE);
      }
    });
  }

  onSelectionChange(selected: GymOwnerResponse[]) {
    this.selectedOwners = selected;
  }

  openAddOwnerModal() {
    this.isAddOwnerModalOpen = true;
  }

  closeAddOwnerModal() {
    this.isAddOwnerModalOpen = false;
  }

  onOwnerInvited() {
    this.getGymOwners();
  }
}
