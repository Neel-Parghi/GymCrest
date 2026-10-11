/**
 * Single source of truth for form field limits and patterns.
 * Use these in both TS validators (Validators.maxLength) and template `maxlength` bindings
 * so the limits cannot drift apart between screens.
 */
export const FIELD_LIMITS = {
  PERSON_NAME: 50,
  GYM_NAME: 50,
  BRAND_NAME: 50,
  BRANCH_NAME: 50,
  PLAN_NAME: 50,
  PLATFORM_NAME: 50,
  EMAIL: 100,
  PHONE: 20,
  URL: 200,
  ADDRESS_LINE: 100,
  CITY: 50,
  STATE: 50,
  COUNTRY: 50,
  POSTAL_CODE: 10,
  GST_NUMBER: 15,
  REGISTRATION_NUMBER: 50,
  DESCRIPTION: 300,
  ADDRESS_BLOCK: 300,
  NOTES: 500,
  EXERCISE_NOTES: 100,
  LONG_TEXT: 1000,
  SHORT_TEXT: 100,
  TAGS: 200,
  SEARCH: 100,
  FOOD_SEARCH: 200,
  API_KEY: 100,
  PASSWORD_MIN: 8,
  PASSWORD_MAX: 30
} as const;

export const FIELD_PATTERNS = {
  /** 10-15 digits; spaces, dashes, brackets and a leading + are allowed for formatting. */
  PHONE: /^(?=(?:\D*\d){10,15}\D*$)\+?[\d\s()-]+$/,
  /** Optional scheme, dotted host, optional port and path. Linear-time (no nested quantifiers). */
  URL: /^(https?:\/\/)?([a-z\d-]+\.)+[a-z]{2,}(:\d{1,5})?(\/\S*)?$/i,
  /** Indian GSTIN: 15 characters. */
  GST_NUMBER: /^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$/i,
  REGISTRATION_NUMBER: /^[a-zA-Z0-9\/-]+$/,
  POSTAL_CODE: /^[A-Za-z0-9 -]{3,10}$/,
  /** At least one lowercase, uppercase, digit and special character. */
  PASSWORD_STRENGTH: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).*$/
} as const;
