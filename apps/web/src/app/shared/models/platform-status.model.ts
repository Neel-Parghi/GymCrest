/** Public platform settings from /public/platform-status: safe for signed-out visitors. */
export interface PlatformStatus {
  supportEmail: string | null;
  supportPhone: string | null;
  gracePeriodDays: number;
  maintenanceActive: boolean;
  maintenanceStartTime: string | null;
  maintenanceEndTime: string | null;
}
