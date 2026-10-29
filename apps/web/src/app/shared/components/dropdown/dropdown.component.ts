import { Component, EventEmitter, Input, Output, forwardRef, ElementRef, inject, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { FIELD_LIMITS } from '../../constants/validation.constants';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { DropdownOption } from '../../models/dropdown.model';

@Component({
  selector: 'app-dropdown',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './dropdown.component.html',
  styleUrl: './dropdown.component.scss',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => DropdownComponent),
      multi: true
    }
  ]
})
export class DropdownComponent implements ControlValueAccessor, OnInit {
  readonly limits = FIELD_LIMITS;

  @Input() options: DropdownOption[] = [];
  @Input() placeholder: string = 'Select...';
  @Input() disabled: boolean = false;
  @Input() value: any;
  @Input() align: 'left' | 'right' = 'left';
  @Input() direction: 'up' | 'down' | 'auto' = 'down';
  @Input() minWidth: string = '150px';
  @Input() isSearchable: boolean = false;

  @Output() selectionChange = new EventEmitter<any>();

  private el = inject(ElementRef);
  isOpen = false;
  actualDirection: 'up' | 'down' = 'down';
  searchControl = new FormControl('');

  insideDrawer = false;
  dropdownTop = 'auto';
  dropdownBottom = 'auto';
  dropdownLeft = '0px';
  dropdownWidth = '0px';

  ngOnInit() {
    this.insideDrawer = !!this.el.nativeElement.closest('.drawer-container, .step-pane');
  }

  @HostListener('document:click', ['$event'])
  clickout(event: Event) {
    if (this.isOpen && !this.el.nativeElement.contains(event.target)) {
      this.isOpen = false;
    }
  }

  onChange: any = () => { };
  onTouched: any = () => { };

  toggleDropdown() {
    if (!this.disabled) {
      if (!this.isOpen) {
        this.updateDirection();
        setTimeout(() => this.ensureVisibility(), 50);
      } else {
        this.searchControl.setValue('', { emitEvent: false });
      }
      this.isOpen = !this.isOpen;
    }
  }

  private ensureVisibility() {
    if (this.isOpen && this.actualDirection === 'down' && this.insideDrawer) {
      this.el.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
  }

  updateDirection() {
    const rect = this.el.nativeElement.getBoundingClientRect();
    const parsedMinWidth = parseInt(this.minWidth) || 150;
    const actualDropdownWidth = Math.max(rect.width, parsedMinWidth);
    this.dropdownWidth = `${actualDropdownWidth}px`;

    if (this.direction !== 'auto') {
      this.actualDirection = this.direction;
    } else {
      const windowHeight = window.innerHeight;
      let spaceBelow = windowHeight - rect.bottom;
      let spaceAbove = rect.top;

      const scrollParent = this.el.nativeElement.closest('.modal-body, .drawer-body, .drawer-container, .form-container, .form-scroll-content, .modal-card');
      if (scrollParent) {
        const parentRect = scrollParent.getBoundingClientRect();
        const parentSpaceBelow = parentRect.bottom - rect.bottom;
        const parentSpaceAbove = rect.top - parentRect.top;

        spaceBelow = Math.min(spaceBelow, parentSpaceBelow);
        spaceAbove = Math.min(spaceAbove, parentSpaceAbove);
      }

      if (spaceBelow < 240 && spaceAbove > spaceBelow) {
        this.actualDirection = 'up';
      } else {
        this.actualDirection = 'down';
      }
    }

    if (!this.insideDrawer) {
      if (this.actualDirection === 'up') {
        this.dropdownTop = 'auto';
        this.dropdownBottom = `${window.innerHeight - rect.top + 4}px`;
      } else {
        this.dropdownTop = `${rect.bottom + 4}px`;
        this.dropdownBottom = 'auto';
      }

      // Dynamic horizontal shift: align to right if it overflows viewport on the right
      const spaceRight = window.innerWidth - rect.left;
      if (spaceRight < actualDropdownWidth || this.align === 'right') {
        this.dropdownLeft = `${rect.right - actualDropdownWidth}px`;
        this.align = 'right';
      } else {
        this.dropdownLeft = `${rect.left}px`;
        this.align = 'left';
      }
    }
  }

  selectOption(option: DropdownOption) {
    this.value = option.value;
    this.isOpen = false;
    this.searchControl.setValue('', { emitEvent: false });
    this.onChange(this.value);
    this.onTouched();
    this.selectionChange.emit(this.value);
  }

  getSelectedLabel(): string {
    const option = this.options.find(o => o.value === this.value);
    return option ? option.label : this.placeholder;
  }

  getLongestLabel(): string {
    let longest = this.placeholder || '';
    if (this.options && this.options.length > 0) {
      for (const option of this.options) {
        if (option.label && option.label.length > longest.length) {
          longest = option.label;
        }
      }
    }
    return longest;
  }

  get filteredOptions(): DropdownOption[] {
    const query = (this.searchControl.value || '').trim();
    if (!this.isSearchable || !query) {
      return this.options;
    }
    const search = query.toLowerCase();
    return this.options.filter(o => o.label.toLowerCase().includes(search));
  }

  trackByValue(_: number, option: DropdownOption): any {
    return option.value;
  }

  onSearch(event: Event) {
    event.stopPropagation();
  }

  getSelectedIcon(): string | undefined {
    const option = this.options.find(o => o.value === this.value);
    return option?.icon;
  }

  writeValue(value: any): void {
    this.value = value;
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }
}
