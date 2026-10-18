export const CONSTANTS = {

    //#region Common

    COMMON_ERROR_MESSAGE: 'Something went wrong!',
    COMMON_SUCCESS_MESSAGE: 'Operation successful!',
    COMMON_DELETE_SUCCESS_MESSAGE: 'Deleted successfully!',
    COMMON_DELETE_ERROR_MESSAGE: 'Failed to delete!',
    COMMON_UPDATE_SUCCESS_MESSAGE: 'Updated successfully!',
    COMMON_UPDATE_ERROR_MESSAGE: 'Failed to update!',
    COMMON_LOAD_ERROR_MESSAGE: 'Failed to load!',

    ACTIONS: {
        EDIT: 'edit',
        VIEW: 'view',
        DELETE: 'delete',
        INVITE: 'invite',
        RE_INVITE: 're-invite',
        UPDATE: 'update',
        LOAD: 'load',
        ROW_CLICK: 'row-click',
    },

    COMMON_SUCCESS_TITLE: 'Success',
    COMMON_ERROR_TITLE: 'Error',
    COMMON_WARNING_TITLE: 'Warning',
    COMMON_INFO_TITLE: 'Info',

    //#endregion

    //#region Auth
    AUTH: {
        LOGIN_SUCCESS: 'Login successful!',
        LOGIN_FAILED: 'Authentication failed.',
        LOGIN_ERROR: 'Login failed. Please try again.',
        REGISTER_SUCCESS: 'Registration successful! Please sign in.',
        REGISTER_FAILED: 'Registration failed. Please try again.',
        PASSWORD_SET_SUCCESS: 'Password set successfully! You can now log in.',
        PASSWORD_RESET_LINK_SENT: 'Password reset link has been sent to your email.'
    },
    //#endregion

    //#region Gym Owner

    GYM_OWNER_DELETE_VALIDATION_MESSAGE: 'Gym Owner cannot be deleted as they own gyms!',
    GYM_OWNER_DELETE_SUCCESS_MESSAGE: 'Gym Owner deleted successfully!',
    GYM_OWNER_DELETE_ERROR_MESSAGE: 'Failed to delete Gym Owner!',
    GYM_OWNER_INVITE_SUCCESS_MESSAGE: 'Gym Owner invited successfully!',
    GYM_OWNER_INVITE_ERROR_MESSAGE: 'Failed to invite Gym Owner!',
    GYM_OWNER_RE_INVITE_SUCCESS_MESSAGE: 'Gym Owner re-invited successfully!',
    GYM_OWNER_RE_INVITE_ERROR_MESSAGE: 'Failed to re-invite Gym Owner!',
    GYM_OWNER_UPDATE_SUCCESS_MESSAGE: 'Gym Owner updated successfully!',
    GYM_OWNER_UPDATE_ERROR_MESSAGE: 'Failed to update Gym Owner!',
    GYM_OWNER_LOAD_ERROR_MESSAGE: 'Failed to load Gym Owners!',

    //#endregion

    //#region UI Labels
    UI_LABELS: {
        STATUS: 'Status',
        ACTIVE: 'Active',
        INACTIVE: 'Inactive',
        SEARCH: 'Search',
    },
    //#endregion

    //#region Confirmations
    CONFIRMATIONS: {
        DELETE_GYM_TITLE: 'Delete Gym',
        DELETE_GYM_MESSAGE: 'Are you sure you want to delete "{name}"? This action cannot be undone.',
        DELETE_PLAN_TITLE: 'Delete Plan',
        DELETE_PLAN_MESSAGE: 'Are you sure you want to delete this plan? This action cannot be undone.',
        DELETE_OWNER_TITLE: 'Delete Gym Owner',
        DELETE_OWNER_MESSAGE: 'Are you sure you want to delete this gym owner? This action cannot be undone.',
        DELETE_PRODUCT_CONFIRM: 'Are you sure you want to delete this product?',
    },
    //#endregion

    //#region Pricing
    PLAN_CREATE_SUCCESS_MESSAGE: 'Plan created successfully',
    PLAN_DELETE_SUCCESS_MESSAGE: 'Plan deleted successfully!',
    PLAN_DELETE_ERROR_MESSAGE: 'Failed to delete plan!',
    PLAN_DELETE_VALIDATION_MESSAGE: 'Plan cannot be deleted as it is associated with gyms!',
    //#endregion

    //#region Gym
    GYM_ONBOARD_SUCCESS_MESSAGE: 'Gym onboarded successfully!',
    GYM_ONBOARD_ERROR_MESSAGE: 'Failed to onboard gym!',
    GYM_INVITE_SUCCESS_MESSAGE: 'Invited successfully!',
    GYM_INVITE_ERROR_MESSAGE: 'Failed to invite!',
    GYM_RE_INVITE_SUCCESS_MESSAGE: 'Re-invited successfully!',
    GYM_RE_INVITE_ERROR_MESSAGE: 'Failed to re-invite!',
    GYM_UPDATE_SUCCESS_MESSAGE: 'Gym updated successfully!',
    GYM_UPDATE_ERROR_MESSAGE: 'Failed to update gym!',
    GYM_DELETE_SUCCESS_MESSAGE: 'Gym deleted successfully!',
    GYM_DELETE_VALIDATION_MESSAGE: 'Gym cannot be deleted as they own branches!',
    GYM_DELETE_ERROR_MESSAGE: 'Failed to delete gym!',
    GYM_LOAD_ERROR_MESSAGE: 'Failed to load gyms!',
    //#endregion

    //#region Profile
    PROFILE_UPDATE_SUCCESS_MESSAGE: 'Profile updated successfully!',
    PROFILE_UPDATE_ERROR_MESSAGE: 'Failed to update profile!',
    PROFILE_PICTURE_UPLOAD_SUCCESS: 'Profile picture updated successfully!',
    PROFILE_PICTURE_UPLOAD_ERROR: 'Failed to upload profile picture!',
    //#endregion

    //#region Dashboard
    DASHBOARD: {
        METRIC_SUBTEXTS: {
            TOTAL_REVENUE: 'Total platform-wide earnings',
            SUBSCRIPTIONS: 'Currently active paid plans',
            TOTAL_GYMS: 'Total gyms onboarded'
        },
        LOADING: 'Loading...',
        MRR_SUBTEXT: 'Monthly Recurring Revenue',
        ARR_SUBTEXT: 'Annual Recurring Revenue',
        LOCALE: 'en-IN',
        CURRENCY: 'INR',
        REPORT_FILENAME_PREFIX: 'report',
        SPARKLINE: {
            DEFAULT_POINTS: "0,30 100,30",
            HEIGHT: 30,
            RANGE_OFFSET: 25
        },
        LOAD_STATS_ERROR: 'Failed to load dashboard statistics.',
        RENEWAL_REMINDER_SUCCESS: 'Renewal reminder sent to {name} successfully!'
    },
    //#endregion

    //#region Payment
    PAYMENT: {
        RAZORPAY: {
            KEY_ID: 'rzp_test_SgEiEY7pGSwfkM',
            CURRENCY: 'INR',
            COMPANY_NAME: 'GymCrest',
            FLOW_DESCRIPTION: 'Testing 0 to 100 Payment Flow',
            THEME_COLOR: '#0f172a'
        },
        MESSAGES: {
            CONFIG_UPDATE_SUCCESS: 'Configuration updated successfully',
            CONFIG_UPDATE_ERROR: 'Failed to update configuration',
            SELECTION_REQUIRED: 'Please select a Gym and a Plan to test the flow.',
            VERIFICATION_SUCCESS: 'Payment verified and Subscription activated.',
            VERIFICATION_ERROR: 'Payment verification failed!'
        }
    },
    //#endregion

    //#region Settings
    SETTINGS: {
        UPDATE_SUCCESS: 'Platform strategy updated successfully!',
        UPDATE_ERROR: 'Failed to update platform strategy.'
    },
    //#endregion

    //#region Auth Accept Invitation
    AUTH_ACCEPT_INVITATION: {
        TITLE: 'Setup Account',
        SUBTITLE: 'Complete your profile by setting a secure password.',
        HERO_TITLE: 'Forge Your Future. Set Your Password.',
        HERO_SUBTITLE: "You're one step away from joining the GymCrest ecosystem. Master your management from day one.",
        VALIDATING: 'Validating invitation...',
        INVALID_TOKEN: 'Invalid invitation link. No token provided.',
        EXPIRED_TOKEN: 'This invitation link has expired or is no longer valid.',
        VALIDATION_ERROR: 'An error occurred while validating your invitation. Please try again later.',
        SET_PASSWORD_ERROR: 'Failed to set password. Please try again.',
    },
    //#endregion

    //#region Gym Plans
    GYM_PLANS_MODULE: {
        LOAD_ERROR: 'Failed to load plans',
        CONTEXT_ERROR: 'User context not found. Please refresh.',
        UPDATE_SUCCESS: 'Plan updated successfully',
        UPDATE_ERROR: 'Failed to update plan',
        CREATE_SUCCESS: 'Plan created successfully',
        CREATE_ERROR: 'Failed to create plan',
        DELETE_TITLE: 'Delete Plan',
        DELETE_CONFIRM: 'Are you sure you want to delete the plan "{name}"?',
        STATUS_UPDATE_SUCCESS: 'Plan {status} successfully',
        STATUS_UPDATE_ERROR: 'Failed to update plan status',
        GYM_PLAN_DELETE_VALIDATION_MESSAGE: 'Cannot delete plan as it is currently associated with active members. Please reassign members to a different plan first.',

    },
    //#endregion

    //#region Members
    MEMBERS_MODULE: {
        LOAD_ERROR: 'Failed to load members',
        DETAIL_LOAD_ERROR_PREFIX: 'Failed to load member details: ',
        DETAIL_LOAD_ERROR: 'Failed to load member details',
        DELETE_SUCCESS: 'Member removed successfully',
        DELETE_ERROR: 'Failed to remove member',
        ONBOARD_SUCCESS: 'Member onboarded successfully!',
        ONBOARD_ERROR: 'Failed to onboard member',
        UPDATE_SUCCESS: 'Member profile updated!',
        UPDATE_ERROR: 'Failed to update member',
        FREEZE_SUCCESS: 'Member frozen',
        FREEZE_ERROR: 'Failed to freeze member',
        UNFREEZE_SUCCESS: 'Member unfrozen',
        UNFREEZE_ERROR: 'Failed to unfreeze member',
        STATUS_UPDATE_SUCCESS: 'Member status updated',
        STATUS_UPDATE_ERROR: 'Failed to update status',
        RENEW_SUCCESS: 'Subscription renewed!',
        RENEW_ERROR: 'Failed to renew subscription',
        EXPORT_SUCCESS: 'Export started',
        EXPORT_ERROR: 'Failed to export members',
        DELETE_CONFIRM_TITLE: 'Remove Member',
        DELETE_CONFIRM_MSG: 'Are you sure you want to remove {name}? This action cannot be undone.',
        ALERT_SUCCESS: 'Notification alert successfully sent to {name}!',
        REMOVE_MEMBER: 'Remove Member',
    },
    //#endregion

    //#region Staff
    STAFF_MODULE: {
        SESSION_ERROR: 'Unable to identify gym session.',
        LOAD_ERROR: 'Failed to load staff directory.',
        DELETE_SUCCESS: 'Staff member removed successfully.',
        DELETE_ERROR: 'Failed to remove staff.',
        EXPORT_INFO: 'Export functionality coming soon.',
        DELETE_CONFIRM_MSG: 'Are you sure you want to remove {name} from your gym? This action cannot be undone.',
        DELETE_CONFIRM_TITLE: 'Remove Staff Member?',
        SESSION_EXPIRED: 'Session expired. Please login again.',
        ASSIGN_MEMBER_SUCCESS: 'Member assigned to trainer successfully!',
        DEALLOCATE_MEMBER_SUCCESS: 'Member deallocated successfully!',
        STATUS_TOGGLE_INFO: 'Status toggle functionality coming soon.',
        DUTY_ON_SUCCESS: '{name} is now On-Duty!',
        DUTY_OFF_SUCCESS: '{name} is now Off-Duty.',
        GENERATE_INVOICE_CONFIRM_TITLE: 'Generate PT Invoice?',
        GENERATE_INVOICE_CONFIRM_MSG: 'Member assigned successfully! Would you like to generate a billing invoice for this training contract now?',
        GENERATE_INVOICE_CONFIRM_TEXT: 'Yes, Create Invoice',
        END_PT_ASSIGNMENT_CONFIRM_TITLE: 'End PT Assignment?',
        END_PT_ASSIGNMENT_CONFIRM_MSG: 'Are you sure you want to end this personal training assignment?',
        END_PT_ASSIGNMENT_CONFIRM_TEXT: 'End Assignment',
        SHIFT_CHECKIN_CONFIRM_TITLE: 'Trainer Shift Check-In',
        SHIFT_CHECKIN_CONFIRM_MSG: 'Are you sure you want to Check-In {name} for their shift?',
        SHIFT_CHECKOUT_CONFIRM_TITLE: 'Trainer Shift Check-Out',
        SHIFT_CHECKOUT_CONFIRM_MSG: 'Are you sure you want to Check-Out {name} and end their shift?',
    },
    //#endregion

    //#region Gym Owner Settings
    GYM_OWNER_SETTINGS: {
        LOAD_ERROR: 'Failed to load settings.',
        VALIDATION_ERROR: 'Please fix the validation errors before saving.',
        UPDATE_SUCCESS: 'Operations & Role Rights settings updated successfully!',
        UPDATE_ERROR: 'Failed to save settings.',
        HOLIDAY_VALIDATION_ERROR: 'Please enter a holiday name and select a date.',
        HOLIDAY_CREATE_SUCCESS: 'Holiday closure scheduled successfully!',
        HOLIDAY_CREATE_ERROR: 'Failed to add holiday closure.',
        HOLIDAY_DELETE_SUCCESS: 'Scheduled closure removed.',
        HOLIDAY_DELETE_ERROR: 'Failed to remove scheduled closure.'
    },
    //#endregion

    //#region Attendance
    ATTENDANCE_MODULE: {
        LOAD_OCCUPANCY_ERROR: 'Failed to load occupancy data.',
        LOAD_SHIFT_LOGS_ERROR: 'Failed to load staff shift logs.',
        LOAD_ATTENDANCE_LOGS_ERROR: 'Failed to load attendance logs.',
        SEARCH_MEMBERS_ERROR: 'Failed to search members.',
        SEARCH_STAFF_ERROR: 'Failed to search staff.',
        LOAD_ON_DUTY_STAFF_ERROR: 'Failed to load on-duty staff.',
        ACCESS_GRANTED_MEMBER: 'Access Granted: {name}',
        MEMBER_CHECKOUT_SUCCESS: '{name} checked out successfully.',
        ACCESS_GRANTED_STAFF: 'Access Granted: {name}',
        STAFF_CHECKOUT_SUCCESS: '{name} checked out successfully.'
    },
    //#endregion

    //#region Billing
    BILLING_MODULE: {
        LOAD_SETTINGS_ERROR: 'Failed to load gym billing settings.',
        LOAD_MEMBER_LOGS_ERROR: 'Failed to load live member billing logs.',
        LOAD_PAYROLL_ERROR: 'Failed to load staff payroll overview.',
        PAYOUT_SUCCESS: 'Payout of ₹{amount} processed successfully for {name}!',
        PAYOUT_RELEASE_SUCCESS: 'Payout of ₹{amount} released successfully for {name}!',
        PAYOUT_RELEASE_ERROR: 'Failed to release staff payout.',
        PAYSLIP_DOWNLOAD_SUCCESS: 'Payslip PDF for {name} downloaded successfully!',
        LOAD_PLANS_ERROR: 'Could not load subscription plans.',
        MERCHANT_GATEWAY_SUCCESS: 'Merchant gateway settings saved! Member online payments are fully verified.',
        WHATSAPP_EMAIL_REMINDER_SUCCESS: 'WhatsApp & Email reminder dispatched to {name} successfully.',
        INVOICE_PAID_SUCCESS: 'Invoice {id} marked as Paid!',
        INVOICE_PAID_SUCCESS_FULL: 'Invoice {id} successfully marked as Paid!',
        INVOICE_PAID_ERROR: 'Failed to update invoice payment status.',
        RECORD_PAYMENT_VALIDATION_ERROR: 'Please enter a valid payment amount and method.',
        RECORD_PAYMENT_SUCCESS: 'Payment recorded successfully for invoice {id}!',
        RECORD_PAYMENT_ERROR: 'Failed to record payment.',
        TAX_CONFIG_SUCCESS: 'Tax rules & legal billing configuration saved successfully.',
        TAX_CONFIG_ERROR: 'Failed to save billing configuration to server.',
        VALIDATION_ERROR: 'Please resolve validation errors in the settings form.',
        RECEIPT_TEMPLATE_ERROR: 'Failed to locate print receipt template.',
        RECEIPT_PDF_SUCCESS: 'PDF receipt generated for {name}.',
        CREATE_INVOICE_VALIDATION_ERROR: 'Please enter valid invoice details.',
        CREATE_INVOICE_INVALID_MEMBER: 'Selected member is invalid.',
        CREATE_INVOICE_SUCCESS: 'Custom invoice generated and recorded successfully!',
        CREATE_INVOICE_ERROR: 'Failed to register custom invoice.',
        PAYROLL_RULES_SUCCESS: 'Payroll rules updated successfully for {name}!',
        PAYROLL_RULES_ERROR: 'Failed to update payroll rules on the server.',
        UPI_TIMEOUT: 'UPI payment request timed out. Please try again.',
        UPI_AUTHORIZING: 'Authorizing payment with bank UPI gateway...',
        UPI_SUCCESS: 'Payment of ₹{amount} processed successfully! Plan updated to {plan}.',
        UPI_ERROR: 'Failed to process payment verification on server.'
    },
    //#endregion

    //#region Inventory
    INVENTORY_MODULE: {
        LOAD_PRODUCTS_ERROR: 'Failed to load products',
        EDIT_COMPLETED_LOGS_INFO: 'Completed logs cannot be edited for audit integrity.',
        PRODUCT_DELETE_SUCCESS: 'Product deleted successfully',
        UPLOAD_IMAGE_ERROR: 'Failed to upload image',
        EQUIPMENT_SAVE_SUCCESS: 'Equipment {status} successfully',
        EQUIPMENT_SAVE_ERROR: 'Failed to save equipment',
        MAINTENANCE_SAVE_SUCCESS: 'Maintenance log {status} successfully',
        MAINTENANCE_SAVE_ERROR: 'Failed to {status} maintenance log',
        PRODUCT_SAVE_SUCCESS: 'Product {status} successfully',
        PRODUCT_SAVE_ERROR: 'Operation failed',
        SALE_RECORD_SUCCESS: 'Sale transaction recorded successfully',
        RECEIPT_PRINTER_INFO: 'Sending receipt to printer...',
        RECEIPT_EMAIL_SUCCESS: 'Receipt emailed to {name}'
    },
    //#endregion

    //#region Gym Module
    GYM_MODULE: {
        LOGO_UPLOAD_SUCCESS: 'Logo uploaded successfully. Save profile changes to persist.',
        LOGO_UPLOAD_ERROR: 'Failed to upload logo.',
        LOAD_DETAILS_ERROR: 'Failed to load gym details',
        LOAD_BRANCHES_ERROR: 'Failed to load branches',
        UPDATE_SUCCESS: 'Gym profile updated successfully',
        UPDATE_ERROR: 'Failed to update gym profile',
        LOAD_MANAGERS_ERROR: 'Failed to load active managers for this gym.',
        DETAILS_NOT_LOADED: 'Gym details not loaded.',
        INIT_SUBSCRIPTION_ERROR: 'Failed to initiate subscription order.',
        PAYMENT_UPGRADE_SUCCESS: 'Payment verified! SaaS Subscription successfully upgraded.',
        BRANCH_ADD_SUCCESS: 'Branch added successfully!',
        BRANCH_UPDATE_SUCCESS: 'Branch updated successfully',
        BRANCH_UPDATE_ERROR: 'Failed to update branch',
        BRANCH_ADD_ERROR: 'Failed to add branch'
    },
    //#endregion

    //#region Subscription Expired
    SUBSCRIPTION_EXPIRED: {
        ACCESS_RESTORED: 'Access restored! Successfully subscribed to the {name}.'
    },
    //#endregion

    //#region Workout Planner
    WORKOUT_PLANNER_MODULE: {

        LOAD_ERROR: 'Failed to load workout plans.',

        // Split Planner Creator
        SPLIT_INVALID_NAME: 'Please enter a valid split name (min 3 characters).',
        SPLIT_DESCRIPTION_REQUIRED: 'Description is required.',
        SPLIT_INVALID_FORM: 'Please fill in all required fields (e.g. Split Name and Exercise choices) before saving.',
        SPLIT_COPY_SUCCESS: 'Workout split copied to clipboard!',
        SPLIT_PASTE_SUCCESS: 'Workout split pasted successfully!',
        SPLIT_PASTE_ERROR: 'Error pasting workout split.',
        SPLIT_NO_CLIPBOARD: 'No copied workout split found on clipboard.',
        SPLIT_MIN_CATEGORY: 'At least one target muscle category must be selected.',
        SPLIT_CREATE_SUCCESS: 'Workout Split Planner template created successfully!',
        SPLIT_CREATE_ERROR: 'Failed to create Workout Split Planner.',
        SPLIT_UPDATE_SUCCESS: 'Workout Split Planner template updated successfully!',
        SPLIT_UPDATE_ERROR: 'Failed to update Workout Split Planner.',

        // Weekly Planner Creator
        WEEKLY_INVALID_FORM: 'Please fill in all required fields (e.g. Schedule Name and Exercise choices) before saving.',
        WEEKLY_PENDING_CONFIRM: 'You have pending workouts on the following days: {days}. Are you sure you want to save?',
        WEEKLY_COPY_SUCCESS: 'Workout plan from {day} copied!',
        WEEKLY_PASTE_SUCCESS: 'Workout plan pasted to {day}!',
        WEEKLY_CREATE_SUCCESS: 'Weekly Calendar Planner template created successfully!',
        WEEKLY_CREATE_ERROR: 'Failed to create Weekly Calendar Planner.',
        WEEKLY_UPDATE_SUCCESS: 'Weekly Calendar Planner template updated successfully!',
        WEEKLY_UPDATE_ERROR: 'Failed to update Weekly Calendar Planner.',

        // Daily Planner Creator
        DAILY_INVALID_NAME: 'Please enter a valid workout planner name (min 3 characters).',
        DAILY_INVALID_FORM: 'Form is invalid. Please check all fields.',
        DAILY_COPY_SUCCESS: 'Workout copied to clipboard!',
        DAILY_PASTE_SUCCESS: 'Workout split pasted successfully!',
        DAILY_PASTE_ERROR: 'Error pasting workout.',
        DAILY_NO_CLIPBOARD: 'No copied workout split found on clipboard.',
        DAILY_MIN_CATEGORY: 'At least one target muscle category must be selected.',
        DAILY_MIN_EXERCISE: 'Workout split must have at least one exercise.',
        DAILY_CREATE_SUCCESS: 'Standalone Workout template created successfully!',
        DAILY_CREATE_ERROR: 'Failed to create Standalone Workout template.',
        DAILY_UPDATE_SUCCESS: 'Standalone Workout template updated successfully!',
        DAILY_UPDATE_ERROR: 'Failed to update Standalone Workout template.',

        // Shared / Timetable
        DELETE_SUCCESS: 'Planner template deleted.',
        DELETE_ERROR: 'Failed to delete planner template.',
        DELETE_CONFIRM: 'Are you sure you want to delete this workout planner template?',
        PLAN_SAVE_ERROR: 'Failed to save updated plan order.',
        SELECT_EXERCISE_WARNING: 'Please select an exercise for all rows.',
    },
    //#endregion

    //#region Member Detail Module
    MEMBER_DETAIL_MODULE: {
        SAVE_RECURRING_OVERRIDE_ERROR: 'Failed to save recurring override schedule.',
        SAVE_OVERRIDE_ERROR: 'Failed to save override schedule to backend.',
        LOG_WORKOUT_READY_PREFIX: 'Ready to log workout for: ',
        EDIT_WORKOUT_PREFIX: 'Editing workout session for: ',
        
        PROGRESS_RECORD_ADDED: 'Progress record added!',
        PROGRESS_RECORD_ERROR: 'Failed to save progress record.',
        MIN_COMPLETED_SET_WARNING: 'Please log at least one completed set before logging the session.',
        LOG_WORKOUT_SUCCESS: 'Successfully logged workout session!',
        LOG_WORKOUT_ERROR: 'Failed to log workout session to database.',
        CANNOT_ASSIGN_CUSTOM_PLAN_WARNING: 'Cannot assign custom string plans in dynamic mode.',
        ASSIGN_PLAN_SUCCESS_PREFIX: 'Assigned ',
        ASSIGN_PLAN_SUCCESS_SUFFIX: ' to member!',
        ASSIGN_PLAN_ERROR: 'Failed to assign plan on backend.',
        INVALID_PLAN_ID_ERROR: 'Invalid Workout Plan ID.',
        ASSIGN_DIET_SUCCESS_PREFIX: 'Assigned ',
        ASSIGN_DIET_SUCCESS_MID: ' diet template to ',
        ASSIGN_DIET_SUCCESS_SUFFIX: '!',
        ASSIGN_DIET_ERROR: 'Failed to assign diet plan. Please try again.',
        
        TRACK_PERFORMANCE: {
            SWITCH_WORKOUT_DAY_PREFIX: 'Switched workout day to: ',
            ADDED_NEW_SET: 'Added new set.',
            MIN_SET_WARNING: 'An exercise must have at least one set.',
            SET_REMOVED: 'Set removed.',
            MARKED_SKIPPED_SUFFIX: ' marked as skipped.',
            MARKED_ACTIVE_SUFFIX: ' marked as active.',
            INVALID_EXERCISE_NAME: 'Please enter a valid exercise name.',
            ADDED_EXERCISE_PREFIX: 'Added ',
            ADDED_EXERCISE_SUFFIX: " to today's workout tracker!",
            SWITCHED_MODE_PREFIX: 'Switched ',
            SWITCHED_MODE_MID: ' to ',
            SWITCHED_MODE_SUFFIX: ' tracking mode.',
            INVALID_SWAP_EXERCISE_NAME: 'Please enter a valid replacement exercise name.',
            SWAPPED_EXERCISE_PREFIX: 'Swapped ',
            SWAPPED_EXERCISE_MID: ' for ',
            SWAPPED_EXERCISE_SUFFIX: '. Sets have been reset for the new exercise.',

            CARDIO_KEYWORDS: [
                'treadmill', 'cycling', 'cardio', 'elliptical', 'jog', 'swimming', 'rowing', 'aerobic', 'hiit', 'spin class'
            ],
            CARDIO_NAME_REGEXP: /\b(run|running|runs|walk|walking|walks|bike|biking|bikes|cycle|cycling|cycles|swim|swimming|swims|rowing|jog|jogging|jogs)\b/,
            CARDIO_TARGET_REGEXP: /\b\d*\s*(min|minute|sec|second|km|kilometer|mile|meter|mtr|hr|hour)s?\b/
        },
        
        WORKOUT_CALENDAR: {
            CANNOT_OVERRIDE_PAST: 'Cannot override workouts in the past.'
        }
    },
    //#endregion

    //#region Diet Planner
    DIET_PLANNER_MODULE: {
        LOAD_ERROR: 'Failed to load diet templates.',
        CREATE_SUCCESS: 'Diet template created successfully in your library!',
        CREATE_ERROR: 'Failed to create diet template.',
        UPDATE_SUCCESS: 'Diet template updated successfully!',
        UPDATE_ERROR: 'Failed to update diet template.',
        DELETE_CONFIRM: 'Are you sure you want to delete this diet template?',
        DELETE_SUCCESS: 'Template deleted successfully.',
        DELETE_ERROR: 'Failed to delete diet template.',
        MIN_MEAL_WARNING: 'A diet template must have at least one meal.'
    },
    //#endregion

    //#region Health Tracker
    HEALTH_TRACKER_MODULE: {
        LOAD_LOGS_ERROR: 'Failed to load health tracking logs',
        RECORD_SUCCESS: 'Health measurement recorded successfully!'
    },
    //#endregion

    //#region Trainer Settings
    TRAINER_SETTINGS: {
        SAVE_SUCCESS: 'Preferences saved successfully!'
    }
    //#endregion
}