import { Component, Input, Output, EventEmitter, ContentChild, ContentChildren, QueryList, TemplateRef, Directive } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ColumnDef } from '../../models/column-def.model';
import { DropdownComponent } from '../dropdown/dropdown.component';
import { DropdownOption } from '../../models/dropdown.model';
import { GridConfigDef } from '../../models/grid-config.model';
import { round2 } from '../../utils/number';

@Directive({
  selector: '[gridCell]',
  standalone: true
})
export class GridCellDirective {
  @Input('gridCell') columnName!: string;
  constructor(public templateRef: TemplateRef<any>) {}
}

@Component({
  selector: 'app-data-grid',
  standalone: true,
  imports: [CommonModule, DropdownComponent],
  templateUrl: './data-grid.component.html',
  styleUrl: './data-grid.component.scss',
})
export class DataGrid {
  @ContentChild('emptyState') emptyStateTemplate?: TemplateRef<any>;
  @ContentChildren(GridCellDirective) customCells!: QueryList<GridCellDirective>;
  @Input() config!: GridConfigDef;
  @Input() data: any[] = [];
  @Input() totalItems: number = 0;
  @Input() pageSize: number = 10;
  @Input() currentPage: number = 1;

  @Output() actionEvent = new EventEmitter<{ action: string, row: any }>();
  @Output() rowClick = new EventEmitter<any>();
  @Output() selectionChange = new EventEmitter<any[]>();
  @Output() pageChange = new EventEmitter<number>();
  @Output() pageSizeChange = new EventEmitter<number>();

  get columns() { return this.config?.columns || []; }
  get selectable() { return this.config?.selectable || false; }
  get isRowClickable() { return this.config?.isRowClickable || false; }

  get totalPages(): number {
    return Math.ceil(this.totalItems / this.pageSize);
  }

  get startRange(): number {
    return (this.currentPage - 1) * this.pageSize + 1;
  }

  get endRange(): number {
    return Math.min(this.currentPage * this.pageSize, this.totalItems);
  }

  selectedRows: any[] = [];

  getCustomTemplate(columnName: string): TemplateRef<any> | null {
    const cell = this.customCells?.find(c => c.columnName === columnName);
    return cell ? cell.templateRef : null;
  }

  ngOnInit() {
  }

  readonly pageSizeOptions: DropdownOption[] = [
    { label: '10', value: 10 },
    { label: '20', value: 20 },
    { label: '50', value: 50 }
  ];

  onPageSizeChange(size: any) {
    const newSize = typeof size === 'object' ? +(size.target as HTMLSelectElement).value : size;
    this.pageSizeChange.emit(newSize);
  }

  onPageChange(page: number) {
    if (page >= 1 && page <= this.totalPages) {
      this.pageChange.emit(page);
    }
  }

  onAction(action: string, row: any) {
    this.actionEvent.emit({ action, row });
  }

  onRowClick(row: any, event: MouseEvent) {
    const target = event.target as HTMLElement;
    if (target.closest('.checkbox-col') || target.closest('.actions-cell') || target.closest('.action-btn')) {
      return;
    }

    if (this.isRowClickable) {
      this.actionEvent.emit({ action: 'row-click', row });
    }

    this.rowClick.emit(row);
  }

  toggleSelection(row: any, event: Event) {
    const isChecked = (event.target as HTMLInputElement).checked;
    if (isChecked) {
      this.selectedRows.push(row);
    } else {
      this.selectedRows = this.selectedRows.filter(r => r !== row);
    }
    this.selectionChange.emit(this.selectedRows);
  }

  toggleAll(event: Event) {
    const isChecked = (event.target as HTMLInputElement).checked;
    if (isChecked) {
      this.selectedRows = [...(this.data || [])];
    } else {
      this.selectedRows = [];
    }
    this.selectionChange.emit(this.selectedRows);
  }

  clearSelection() {
    this.selectedRows = [];
    this.selectionChange.emit(this.selectedRows);
  }

  isAllSelected() {
    return this.data?.length > 0 && this.selectedRows.length === this.data.length;
  }

  isSelected(row: any) {
    return this.selectedRows.includes(row);
  }

  getBadgeClass(status: string): string {
    if (!status) return 'badge-neutral';
    const s = status.toLowerCase();
    switch (s) {
      case 'active':
      case 'accepted':
      case 'success':
      case 'paid':
      case 'in stock':
      case 'excellent':
      case 'good':
      case 'routine':
      case 'completed':
      case 'cash':
      case 'card':
      case 'upi':
        return 'badge-active';
      case 'frozen':
      case 'freeze':
        return 'badge-frozen';
      case 'pending':
      case 'partial':
      case 'low stock':
      case 'fair':
      case 'repair':
      case 'scheduled':
      case 'maintenance due':
      case 'in progress':
      case 'awaiting release':
        return 'badge-pending';
      case 'unpaid':
      case 'refunded':
      case 'out of stock':
      case 'broken':
      case 'critical':
      case 'expired':
      case 'repair needed':
      case 'danger':
      case 'inactive':
      case 'failed':
      case 'overdue':
        return 'badge-unpaid';
      default:
        return 'badge-neutral';
    }
  }

  getNestedValue(row: any, key: string): any {
    if (!row || !key) return '';
    if (!key.includes('.')) return row[key] ?? '';
    return key.split('.').reduce((acc, part) => (acc && acc[part] !== undefined) ? acc[part] : '', row) || '';
  }

  /** Cell text: like getNestedValue, but fractional numbers are capped at 2 decimals. */
  displayValue(row: any, key: string): any {
    const value = this.getNestedValue(row, key);
    return typeof value === 'number' && !Number.isInteger(value) ? round2(value) : value;
  }
}
