export interface PricingPlan {
    id: string;
    name: string;
    description?: string;
    price: number;
    durationInDays: number;
    maxBranches?: number;
    maxMembers?: number;
    features?: string[];
    isTrial: boolean;
    isActive: boolean;
    createdAt?: string;
}
export interface PricingPlanCreateRequest {
    name: string;
    description: string;
    price: number | null;
    durationInDays: number | null;
    maxBranches: number | null;
    maxMembers: number | null;
    features: string[];
    isTrial: boolean;
    isActive: boolean;
}

// Plan features are edited as one-per-line text in the super-admin forms
export const featuresToText = (features?: string[] | null): string => (features ?? []).join('\n');
export const textToFeatures = (text?: string | null): string[] =>
    (text ?? '').split('\n').map(f => f.trim()).filter(Boolean);
