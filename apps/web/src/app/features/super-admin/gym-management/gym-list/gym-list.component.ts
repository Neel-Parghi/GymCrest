import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DataGrid } from '../../../../shared/components/data-grid/data-grid.component';
import { AppGridConfig } from '../../../../shared/constants/grid-config';
import { GymOnboardingModalComponent } from '../gym-onboarding-modal/gym-onboarding-modal.component';
import { FilterBarComponent } from '../../../../shared/components/filter-bar/filter-bar.component';
import { FilterConfig } from '../../../../shared/models/filter.model';
import { GymService } from '../../../../core/services/gym.service';
import { GymListResponse } from '../../../../shared/models/gym.model';
import { finalize } from 'rxjs';

import { GymDetailsDrawerComponent } from '../gym-details-drawer/gym-details-drawer.component';
import { CONSTANTS } from '../../../../core/constants/constants';
import { NotificationService } from '../../../../core/services/notification.service';
import { ConfirmationService } from '../../../../core/services/confirmation.service';

@Component({
  selector: 'app-gym-list',
  standalone: true,
  imports: [CommonModule, DataGrid, GymOnboardingModalComponent, FilterBarComponent, GymDetailsDrawerComponent],
  templateUrl: './gym-list.component.html',
  styleUrl: './gym-list.component.scss',
})
export class GymList implements OnInit {
  private gymService = inject(GymService);
  private notification = inject(NotificationService);
  private confirmation = inject(ConfirmationService);

  gridConfig = AppGridConfig['GymList'];
  isAddGymModalOpen = false;

  // Drawer state
  isDetailsDrawerOpen = false;
  isEditMode: boolean = false;
  selectedGymDetail?: GymListResponse;

  // Data state
  originalData: GymListResponse[] = [];
  filteredData: GymListResponse[] = [];
  displayData: GymListResponse[] = [];
  isLoading = false;

  // Pagination state
  totalItems = 0;
  pageSize = 10;
  currentPage = 1;

  // Filter configuration
  filterConfigs: FilterConfig[] = [
    {
      key: 'isActive',
      label: 'Status',
      options: [
        { label: CONSTANTS.UI_LABELS.ACTIVE, value: 'true' },
        { label: CONSTANTS.UI_LABELS.INACTIVE, value: 'false' }
      ]
    }
  ];

  selectedGyms: any[] = [];

  ngOnInit(): void {
    this.loadGyms();
  }

  loadGyms(search: string = '') {
    this.isLoading = true;
    this.gymService.getGymList(this.currentPage, this.pageSize, search)
      .pipe(finalize(() => this.isLoading = false))
      .subscribe({
        next: (res) => {
          this.displayData = res.data.items;
          this.totalItems = res.data.totalCount;
          this.filteredData = res.data.items;
        },
        error: (err) => this.notification.error(err.error?.message || CONSTANTS.GYM_LOAD_ERROR_MESSAGE)
      });
  }

  updateGym(gym: GymListResponse) {
    this.gymService.updateGym(gym.id, gym).subscribe({
      next: () => {
        this.notification.success(CONSTANTS.GYM_UPDATE_SUCCESS_MESSAGE);
        this.loadGyms();
      },
      error: (err) => {
        this.notification.error(err.error?.message || CONSTANTS.GYM_UPDATE_ERROR_MESSAGE);
      }
    });
  }

  applyFilters(filters: any) {
    this.currentPage = 1;
    this.loadGyms(filters.search);
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
    this.loadGyms();
  }

  onPageSizeChanged(size: number) {
    this.pageSize = size;
    this.currentPage = 1;
    this.loadGyms();
  }

  handleAction(event: { action: string, row: any }) {
    this.selectedGymDetail = event.row;

    if (event.action === CONSTANTS.ACTIONS.VIEW || event.action === CONSTANTS.ACTIONS.EDIT || event.action === CONSTANTS.ACTIONS.ROW_CLICK) {
      this.openDrawer(event.row, event.action);
    }
    if (event.action == CONSTANTS.ACTIONS.DELETE) {
      this.handleDelete(event.row);
    }
  }

  async handleDelete(gym: GymListResponse) {
    const confirmed = await this.confirmation.confirm({
      title: CONSTANTS.CONFIRMATIONS.DELETE_GYM_TITLE,
      message: CONSTANTS.CONFIRMATIONS.DELETE_GYM_MESSAGE.replace('{name}', gym.gymName),
      confirmText: CONSTANTS.ACTIONS.DELETE,
      type: 'danger'
    });

    if (confirmed) {
      this.gymService.deleteGym(gym.id).subscribe({
        next: (res: any) => {
          const data = res?.data || res;

          if (data?.Success === false || data?.success === false) {
            const message = (data?.Message || data?.message) === 'GYM_HAS_USERS'
              ? CONSTANTS.GYM_DELETE_VALIDATION_MESSAGE
              : (data?.Message || data?.message || CONSTANTS.GYM_DELETE_ERROR_MESSAGE);

            this.notification.error(message);
            return;
          }

          this.notification.success(CONSTANTS.GYM_DELETE_SUCCESS_MESSAGE);
          this.loadGyms();
        },
        error: (err) => {
          this.notification.error(err.error?.message || CONSTANTS.GYM_DELETE_ERROR_MESSAGE);
        }
      });
    }
  }

  openDrawer(rowInfo: GymListResponse, action: string) {
    this.selectedGymDetail = rowInfo;
    this.isDetailsDrawerOpen = true;
    this.isEditMode = action === CONSTANTS.ACTIONS.EDIT;
  }

  onSelectionChange(selected: any[]) {
    this.selectedGyms = selected;
  }

  openAddGymModal() {
    this.isAddGymModalOpen = true;
  }

  closeAddGymModal() {
    this.isAddGymModalOpen = false;
  }

  closeDetailDrawer() {
    this.isDetailsDrawerOpen = false;
    this.isEditMode = false;
    this.selectedGymDetail = undefined;
  }
}
