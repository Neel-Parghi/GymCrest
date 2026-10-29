import { GridConfigDef } from '../models/grid-config.model';

export const AppGridConfig: Record<string, GridConfigDef> = {
  GymList: {
    columns: [
      { key: 'gymName', label: 'Gym Name', isClickable: true },
      { key: 'brandName', label: 'Brand / Franchise' },
      { key: 'ownerName', label: 'Owner' },
      { key: 'planName', label: 'Current Plan' },
      { key: 'paymentStatus', label: 'Payment', type: 'badge' },
      { key: 'branchesCount', label: 'Branches' },
      { key: 'membersCount', label: 'Members' },
      { key: 'isVerified', label: 'Verification', type: 'bool' },
      { key: 'isActive', label: 'Status', type: 'bool' },
      { key: 'actions', label: 'Manage', type: 'action' }
    ],
    selectable: true
  },
  GymOwners: {
    columns: [
      { key: 'name', label: 'Owner Name', isClickable: true },
      { key: 'email', label: 'Email' },
      { key: 'joinedDate', label: 'Joined Date', type: 'date' },
      { key: 'phone', label: 'Phone Number' },
      { key: 'gymNames', label: 'Gyms' },
      { key: 'invitationStatus', label: 'Invite Status', type: 'badge' },
      { key: 'status', label: 'Status', type: 'bool' },
      { key: 'deletionRequestedOn', label: 'Deletion Requested', type: 'date' },
      { key: 'actions', label: 'Actions', type: 'action' }
    ],
    selectable: true
  },
  StandaloneUsers: {
    columns: [
      { key: 'firstName', label: 'User Name', type: 'profile', subKey: 'email' },
      { key: 'phone', label: 'Phone Number', type: 'text' },
      { key: 'role', label: 'Role', type: 'badge' },
      { key: 'createdOn', label: 'Joined Date', type: 'date' },
      { key: 'isEmailVerified', label: 'Email Verified', type: 'bool' },
      { key: 'status', label: 'Status', type: 'badge' },
      { key: 'deletionRequestedOn', label: 'Deletion Scheduled On', type: 'date' },
      { key: 'actions', label: 'Actions', type: 'action' }
    ],
    selectable: false,
    excludeActions: ['edit', 'view']
  },
  PricingList: {
    columns: [
      { key: 'name', label: 'Plan Name', isClickable: true },
      { key: 'description', label: 'Description' },
      { key: 'durationInDays', label: 'Duration (Days)', type: 'number' },
      { key: 'price', label: 'Price', type: 'currency' },
      { key: 'maxBranches', label: 'Max Branches', type: 'number' },
      { key: 'maxMembers', label: 'Max Members', type: 'number' },
      // { key: 'createdAt', label: 'Created Date', type: 'date' },
      { key: 'status', label: 'Status', type: 'badge' },
      { key: 'isTrial', label: 'Trial', type: 'bool' },
      { key: 'actions', label: 'Actions', type: 'action' }
    ],
    selectable: true
  },
  PaymentList: {
    columns: [
      { key: 'gymName', label: 'Gym Name' },
      { key: 'planName', label: 'Plan' },
      { key: 'amount', label: 'Amount', type: 'currency' },
      { key: 'createdAt', label: 'Payment Date', type: 'date' },
      { key: 'status', label: 'Status', type: 'badge' },
      { key: 'gatewayTransactionId', label: 'Transaction ID' },
    ],
    selectable: false
  },
  GymMembers: {
    columns: [
      { key: 'firstName', label: 'Member', type: 'profile', subKey: 'email' },
      { key: 'membershipNumber', label: 'Membership ID', type: 'text' },
      { key: 'currentSubscription.planNameSnapshot', label: 'Plan', type: 'complex', subKey: 'currentSubscription.pricePaid' },
      { key: 'joiningDate', label: 'Joined', type: 'date' },
      { key: 'personalTrainer', label: 'Trainer', type: 'text' },
      { key: 'statusLabel', label: 'Status', type: 'badge' },
      { key: 'currentSubscription.paymentStatusLabel', label: 'Payment', type: 'badge' },
      { key: 'currentSubscription.endDate', label: 'Expiry', type: 'date' },
      { key: 'actions', label: 'Actions', type: 'action' }
    ],
    selectable: false
  },
  StaffList: {
    columns: [
      { key: 'fullName', label: 'Name', isClickable: true },
      { key: 'staffNumber', label: 'Staff ID' },
      { key: 'email', label: 'Email' },
      { key: 'phoneNumber', label: 'Phone' },
      { key: 'roleName', label: 'Role', type: 'badge' },
      { key: 'joiningDate', label: 'Joined', type: 'date' },
      { key: 'isActive', label: 'Status', type: 'bool' },
      { key: 'actions', label: 'Actions', type: 'action' }
    ],
    selectable: false,
    excludeActions: ['delete']
  },
  InventoryItems: {
    columns: [
      { key: 'name', label: 'Product', type: 'profile', subKey: 'sku' },
      { key: 'category', label: 'Category', type: 'text' },
      { key: 'stockQuantity', label: 'Qty in Stock', type: 'number' },
      { key: 'sellingPrice', label: 'Unit Price', type: 'currency' },
      { key: 'stockStatus', label: 'Availability', type: 'badge' },
      { key: 'actions', label: 'Actions', type: 'action' }
    ],
    selectable: false,
  },
  EquipmentItems: {
    columns: [
      { key: 'name', label: 'Equipment', type: 'profile', subKey: 'category' },
      { key: 'condition', label: 'Condition', type: 'text' },
      { key: 'lastService', label: 'Last Service', type: 'date' },
      { key: 'status', label: 'Status', type: 'badge' },
      { key: 'actions', label: 'Actions', type: 'action' }
    ],
    selectable: false,
    excludeActions: ['delete']
  },
  MaintenanceTasks: {
    columns: [
      { key: 'name', label: 'Equipment Asset', type: 'profile', subKey: 'serialNumber' },
      { key: 'health', label: 'Health Status', type: 'progress' },
      { key: 'lastService', label: 'Last Serviced', type: 'date' },
      { key: 'status', label: 'Current Status', type: 'badge' },
      { key: 'actions', label: 'Record Service', type: 'action' }
    ],
    selectable: false,
    excludeActions: ['delete', 'view']
  },
  SalesHistory: {
    columns: [
      { key: 'memberName', label: 'Member', type: 'profile', subKey: 'memberId' },
      { key: 'productName', label: 'Product', type: 'text' },
      { key: 'quantity', label: 'Qty', type: 'number' },
      { key: 'totalAmount', label: 'Total', type: 'currency' },
      { key: 'date', label: 'Date', type: 'date' },
      { key: 'paymentMethod', label: 'Method', type: 'badge' },
      { key: 'actions', label: 'Actions', type: 'action' }
    ],
    selectable: false,
    excludeActions: ['edit', 'delete']
  },
  ServiceHistory: {
    columns: [
      { key: 'equipmentName', label: 'Equipment', type: 'text' },
      { key: 'serviceType', label: 'Type', type: 'badge' },
      { key: 'technicianName', label: 'Technician', type: 'text' },
      { key: 'startDate', label: 'Date', type: 'date' },
      { key: 'cost', label: 'Cost', type: 'currency' },
      { key: 'status', label: 'Status', type: 'badge' },
      { key: 'actions', label: 'Actions', type: 'action' }
    ],
    selectable: false,
    excludeActions: ['delete']
  },
  MemberInvoices: {
    columns: [
      { key: 'formattedInvoiceId', label: 'Invoice ID', type: 'text' },
      { key: 'memberName', label: 'Member', type: 'profile', subKey: 'email' },
      { key: 'type', label: 'Billing Type', type: 'text' },
      { key: 'amount', label: 'Amount', type: 'currency' },
      { key: 'balance', label: 'Balance Due', type: 'currency' },
      { key: 'dateIssued', label: 'Date Issued', type: 'date' },
      { key: 'dueDate', label: 'Due Date', type: 'date' },
      { key: 'status', label: 'Status', type: 'badge' },
      { key: 'actions', label: 'Actions', type: 'action' }
    ],
    selectable: false,
    excludeActions: ['delete']
  },
  PlatformInvoices: {
    columns: [
      { key: 'id', label: 'Transaction ID', type: 'text' },
      { key: 'planName', label: 'Plan Tier', type: 'text' },
      { key: 'amount', label: 'Amount', type: 'currency' },
      { key: 'billingDate', label: 'Date Paid', type: 'date' },
      { key: 'status', label: 'Status', type: 'badge' },
      { key: 'receipt', label: 'Receipt', type: 'action' }
    ],
    selectable: false
  },
  StaffPayouts: {
    columns: [
      { key: 'staffName', label: 'Staff Member', type: 'profile', subKey: 'email' },
      { key: 'role', label: 'Role', type: 'text' },
      { key: 'baseSalary', label: 'Base Salary', type: 'currency' },
      { key: 'commissions', label: 'PT Commissions', type: 'currency' },
      { key: 'totalPayout', label: 'Net Payout', type: 'currency' },
      { key: 'status', label: 'Payout Status', type: 'badge' },
      { key: 'actions', label: 'Actions', type: 'action' }
    ],
    selectable: false
  },
  MemberAttendanceLogs: {
    columns: [
      { key: 'memberName', label: 'Member Name', type: 'text' },
      { key: 'membershipNumber', label: 'Membership ID', type: 'text' },
      { key: 'checkInTime', label: 'Check-In Time', type: 'date' },
      { key: 'checkOutTime', label: 'Check-Out Time', type: 'date' },
      { key: 'duration', label: 'Workout Duration', type: 'text' },
      { key: 'status', label: 'Status', type: 'badge' },
      { key: 'branchName', label: 'Branch', type: 'text' }
    ],
    selectable: false
  },
  StaffAttendanceLogs: {
    columns: [
      { key: 'staffName', label: 'Staff Name', type: 'text' },
      { key: 'staffNumber', label: 'Staff Number', type: 'text' },
      { key: 'roleName', label: 'Role', type: 'text' },
      { key: 'checkInTime', label: 'Clock-In Time', type: 'date' },
      { key: 'checkOutTime', label: 'Clock-Out Time', type: 'date' },
      { key: 'hoursWorkedLabel', label: 'Hours Worked', type: 'text' },
      { key: 'notes', label: 'Shift Notes', type: 'text' }
    ],
    selectable: false
  },
  PTClients: {
    columns: [
      { key: 'firstName', label: 'Client Name', type: 'profile', subKey: 'email' },
      { key: 'membershipNumber', label: 'Membership No.', type: 'text' },
      { key: 'assignedSlot', label: 'Preferred Slot', type: 'text' },
      { key: 'assignedDate', label: 'Assigned Date', type: 'date' },
      { key: 'endDate', label: 'Expiration', type: 'date' },
      { key: 'status', label: 'Status', type: 'badge' },
      { key: 'actions', label: 'Actions', type: 'action' }
    ],
    selectable: false,
    excludeActions: ['edit']
  },
  Announcements: {
    columns: [
      { key: 'title', label: 'Announcement Title', type: 'text' },
      { key: 'message', label: 'Message', type: 'text' },
      { key: 'createdOn', label: 'Broadcast Date', type: 'date' },
      { key: 'isActive', label: 'Status', type: 'bool' },
      { key: 'actions', label: 'Actions', type: 'action' }
    ],
    selectable: false,
    excludeActions: ['view']
  },
  Templates: {
    columns: [
      { key: 'name', label: 'Template Name', type: 'text' },
      { key: 'typeLabel', label: 'Type', type: 'badge' },
      { key: 'titleTemplate', label: 'Title Template', type: 'text' },
      { key: 'isActive', label: 'Status', type: 'bool' },
      { key: 'actions', label: 'Actions', type: 'action' }
    ],
    selectable: false,
    excludeActions: ['view']
  }
};
