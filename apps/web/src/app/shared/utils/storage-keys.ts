/** Browser storage keys. */
export const STORAGE_KEYS = {
  OWNER_WIZARD_DRAFT: 'gymcrest_ownerWizardDraft',
  USER_WIZARD_DRAFT: 'gymcrest_userWizardDraft_v2',
  COPIED_WORKOUT_SPLIT: 'gymcrest_copied_workout_split',
  /** Plan an owner picked on the landing pricing cards, preselected at the onboarding plan step. */
  INTENDED_PLAN: 'gymcrest_intendedPlan'
} as const;

/** Keys written before the rebrand, mapped to their new names. */
const PRE_REBRAND_KEYS: Record<string, string> = {
  gymForge_ownerWizardDraft: STORAGE_KEYS.OWNER_WIZARD_DRAFT,
  gymForge_userWizardDraft_v2: STORAGE_KEYS.USER_WIZARD_DRAFT,
  gymforge_copied_workout_split: STORAGE_KEYS.COPIED_WORKOUT_SPLIT,
  elevatyon_ownerWizardDraft: STORAGE_KEYS.OWNER_WIZARD_DRAFT,
  elevatyon_userWizardDraft_v2: STORAGE_KEYS.USER_WIZARD_DRAFT,
  elevatyon_copied_workout_split: STORAGE_KEYS.COPIED_WORKOUT_SPLIT
};

/** Moves values saved under the old GymForge/Elevatyon keys so drafts survive the renames. Runs once per browser. */
export function migratePreRebrandStorage(): void {
  try {
    for (const [oldKey, newKey] of Object.entries(PRE_REBRAND_KEYS)) {
      const value = localStorage.getItem(oldKey);
      if (value === null) continue;
      if (localStorage.getItem(newKey) === null) {
        localStorage.setItem(newKey, value);
      }
      localStorage.removeItem(oldKey);
    }
  } catch {
    // Storage can be unavailable (private mode, blocked site data); nothing to migrate then.
  }
}
