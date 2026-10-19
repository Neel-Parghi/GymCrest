import { environment } from '../../../environments/environment';

export const API_CONSTANTS = {
  BASE_URL: environment.apiUrl,

  AUTH: {
    LOGIN: 'auth/login',
    REGISTER: 'auth/register',
    VERIFY_OTP: 'auth/verify-otp',
    RESEND_OTP: 'auth/resend-otp',
    REGISTER_SUPERADMIN: 'auth/register-superadmin',
    LOGOUT: 'auth/logout',
    REFRESH: 'auth/refresh',
    ME: 'auth/me',
    FORGOT_PASSWORD: 'auth/forgot-password',
    RESET_PASSWORD: 'auth/reset-password',
    REQUEST_DELETION: 'auth/request-deletion'
  },
  USER: {
    PROFILE: 'users/profile',
    UPDATE_PROFILE: 'users/profile',
    INVITE_OWNER: 'users/invite-owner',
    RE_INVITE: 'users/re-invite',
    SET_PASSWORD: 'users/set-password',
    CHANGE_PASSWORD: 'users/change-password',
    VALIDATE_INVITATION: 'users/validate-invitation',
    PREFERENCES: 'users/preferences',
    SUGGESTED_TARGETS: 'users/preferences/suggested-targets',
    REGENERATE_STARTER_PLANS: 'users/regenerate-starter-plans',
    UPLOAD_AVATAR: 'users/profile/upload-avatar',
    MY_SUBSCRIPTIONS: 'users/my-subscriptions',
    MY_GYM: 'users/my-gym',
    STANDALONE: 'users/standalone',
    DELETE: 'users',
    SAVE_ONBOARDING_STEP: 'users/save-onboarding-step',
    COMPLETE_ONBOARDING: 'users/complete-user-onboarding',
    ONBOARDING_PREVIEW_TARGETS: 'users/onboarding/preview-targets',
    DASHBOARD: 'users/dashboard',
    ROUTINES: 'users/routines',
    AVAILABLE_PLANS: 'user-plans/available'
  },
  GYM: {
    ONBOARD: 'my-gym/onboard',
    UPDATE: 'gyms',
    LIST: 'gyms',
    DELETE: 'gyms',
    BRANCHES: 'gyms/{id}/branches',
    MY_GYM: 'my-gym',
    MY_BRANCHES: 'my-gym/branches',
    SETTINGS: 'my-gym/settings',
    HOLIDAYS: 'my-gym/holidays'
  },
  GYM_OWNER: {
    LIST: 'gym-owners',
    UPDATE: 'gym-owners',
    DELETE: 'gym-owners',
    DASHBOARD: 'gym-owner/dashboard/stats'
  },
  PRICING: {
    LIST: 'saas-plans',
    GET: 'saas-plans',
    ADD: 'saas-plans',
    UPDATE: 'saas-plans',
    DELETE: 'saas-plans',
  },
  SUPER_ADMIN: {
    DASHBOARD: 'superadmin/dashboard/stats',
    REVENUE: 'superadmin/payments/stats',
    CONFIG: 'superadmin/config',
    REPORTS: {
      EXPORT: 'superadmin/reports/export'
    }
  },
  MEMBERS: {
    ONBOARD: 'members',
    LIST: 'members',
    GET: 'members',
    UPDATE: 'members',
    TOGGLE_STATUS: 'members/{id}/status/toggle',
    FREEZE: 'members/{id}/status/freeze',
    UNFREEZE: 'members/{id}/status/unfreeze',
    RENEW: 'members/{id}/subscriptions',
    DELETE: 'members',
    SUBSCRIPTION_HISTORY: 'members/{id}/subscriptions',
    EXPORT: 'members/export',
    MEASUREMENTS: 'members/{memberId}/measurements',
    DASHBOARD: 'members/dashboard',
    ACTIVE_PLAN: 'members/{memberId}/active-plan',
    ASSIGN_PLAN: 'members/{memberId}/assign-plan',
    WORKOUT_LOGS: 'members/{memberId}/workout-logs',
    RECURRING_OVERRIDE: 'members/{memberId}/recurring-override',
    PLAN_ASSIGNMENTS: 'members/{memberId}/plan-assignments',
    ACTIVE_DIET: 'members/{memberId}/active-diet',
    ASSIGN_DIET: 'members/{memberId}/assign-diet',
    CUSTOM_DIET: 'members/{memberId}/custom-diet',
  },
  STAFF: {
    BASE: 'staff',
    LIST: 'staff',
  },
  INVENTORY: {
    PRODUCTS: 'inventory/products',
    EQUIPMENT: 'equipment',
    SALES: 'inventory/sales',
    SALES_HISTORY: 'inventory/sales/history',
    MAINTENANCE: 'maintenance',
    MAINTENANCE_HISTORY: 'maintenance/history',
    STATS: 'inventory/stats'
  },
  PAYMENTS: {
    STATS: 'payments/stats',
    TRANSACTIONS: 'payments/transactions',
    INITIATE: 'payments/initiate',
    VERIFY: 'payments/verify',
    SETTINGS: 'payments/settings',
    UPDATE_SAAS_CONFIG: 'payments/settings/update',
    SUBSCRIPTION: 'payments/subscription',
    HISTORY: 'payments/history'
  },
  MEMBER_PAYMENT: {
    INITIATE_CHECKOUT: 'member-payment/initiate-checkout',
    VERIFY_CHECKOUT: 'member-payment/verify-checkout',
    OFFLINE_CHECKOUT: 'member-payment/offline-checkout'
  },
  GYM_PLAN: {
    GET: 'gym-plans',
    LIST: 'gym-plans',
    ADD: 'gym-plans',
    UPDATE: 'gym-plans',
    DELETE: 'gym-plans',
  },
  ATTENDANCE: {
    CHECK_IN: 'attendance/check-in',
    CHECK_OUT: 'attendance/check-out',
    OCCUPANCY: 'attendance/occupancy',
    OCCUPANCY_STATS: 'attendance/occupancy/stats',
    LOGS: 'attendance/logs',
  },
  BILLING: {
    MEMBERS_OVERVIEW: 'billing/members/overview',
    CREATE_INVOICE: 'billing/members/invoice',
    PAY_INVOICE: 'billing/members/pay',
    STAFF_OVERVIEW: 'billing/staff/overview',
    UPDATE_STAFF_RULES: 'billing/staff/rules',
    RELEASE_STAFF_PAYOUT: 'billing/staff/payout/release',
    UPDATE_STAFF: (id: string) => `gym-owner/staff/${id}`,
    DELETE_STAFF: (id: string) => `gym-owner/staff/${id}`
  },
  DIET_TRACKING: {
    USER_GET_LOG: (date: string) => `user/diet-tracking/${date}`,
    USER_ADD_MEAL: 'user/diet-tracking/meals',
    USER_UPDATE_MEAL: (id: string) => `user/diet-tracking/meals/${id}`,
    USER_REMOVE_MEAL: (id: string) => `user/diet-tracking/meals/${id}`,
    USER_SUMMARY: (endDate: string) => `user/diet-tracking/summary/${endDate}`,
    USER_SEARCH_FOOD: (query: string) => `user/diet-tracking/search-food?query=${encodeURIComponent(query)}`,

    TRAINER_GET_LOG: (memberId: string, date: string) => `trainer/diet-tracking/${memberId}/${date}`,
    TRAINER_SUMMARY: (memberId: string, endDate: string) => `trainer/diet-tracking/${memberId}/summary/${endDate}`,
  },
  WORKOUT_PROGRESS: {
    EXERCISES: 'user/workout-progress/exercises',
    MUSCLE_GROUPS: 'user/workout-progress/muscle-groups',
    PROGRESS: 'user/workout-progress'
  },
  COMMON: {
    UPLOAD: 'fileupload'
  },
  WORKOUT_MASTER: {
    CATEGORIES: 'workout-master/categories',
    EXERCISES: 'workout-master/exercises',
    BY_CATEGORY: 'workout-master/exercises/{category}'
  },
  WORKOUT_PLAN: {
    BASE: 'workout-plan',
    BY_ID: 'workout-plan/{id}'
  },
  DIET_PLAN: {
    BASE: 'diet-plan',
    BY_ID: 'diet-plan/{id}'
  },
  ANNOUNCEMENTS: {
    BASE: 'announcements',
    MY_GYM: 'announcements/my-gym'
  },
  ANNOUNCEMENT_TEMPLATES: {
    BASE: 'announcement-templates'
  },
  USER_NOTIFICATIONS: {
    BASE: 'notifications'
  }
};
