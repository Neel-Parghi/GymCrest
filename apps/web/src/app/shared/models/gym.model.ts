export interface Address {
  line1: string;
  line2?: string;
  city: string;
  state: string;
  country: string;
  postalCode: string;
}

export interface GymBranch {
  name: string;
  address: Address;
  contactNumber: string;
  openTime: string;
  closeTime: string;
}

export interface BranchDto {
  id: string;
  name: string;
  contactNumber?: string;
  openTime?: string;
  closeTime?: string;
  managerName?: string;
  managerId?: string;
  address: Address;
}

export interface OnboardGymRequest {
  name: string;
  brandName?: string;
  description?: string;
  establishedDate?: string;
  registrationNumber?: string;
  email: string;
  phone: string;
  gstNumber?: string;
  websiteUrl?: string;
  logoUrl?: string;
  coverImageUrl?: string;
  address: Address;
  branches: GymBranch[];
  assignedOwnerId?: string | null;
  planId?: string | null;
  isTrial: boolean;
}

export interface GymListResponse {
  id: string;
  gymName: string;
  brandName: string;
  ownerName: string;
  email: string;
  phone: string;
  branchesCount: number;
  isActive: boolean;
  isVerified: boolean;

  // Detail fields
  description?: string;
  websiteUrl?: string;
  gstNumber?: string;
  registrationNumber?: string;
  establishedDate?: string;
  logoUrl?: string;
  bannerUrl?: string;

  // New Billing & Settings
  invoicePrefix?: string;
  defaultTaxRate?: number;
  overdueGraceDays?: number;
  autoEmailReceipts?: boolean;

  // Subscription info
  planName?: string;
  subscriptionExpiry?: string;
  isTrialPlan: boolean;
  paymentStatus: string;
  hasActiveSubscription: boolean;
  awaitingFirstPayment?: boolean;
  createdOn?: string;
}

export interface GymOwnerResponse {
  id: string;
  firstName: string;
  lastName: string;
  name: string;
  email: string;
  phone: string;
  joinedDate: string;
  gymsOwned: number;
  invitationStatus: string;
  status: boolean;
}

export interface UpdateGymOwnerRequest {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  status: boolean;
}

export interface UpdateMyGymRequest {
  gymName: string;
  brandName?: string;
  email?: string;
  phone?: string;
  websiteUrl?: string;
  description?: string;
  gstNumber?: string;
  registrationNumber?: string;
  logoUrl?: string;
  bannerUrl?: string;

  // New Billing & Settings
  invoicePrefix?: string;
  defaultTaxRate?: number;
  overdueGraceDays?: number;
  autoEmailReceipts?: boolean;
}

export interface Holiday {
  id: string;
  name: string;
  date: string;
  branchId?: string | null;
  branchName?: string | null;
}
