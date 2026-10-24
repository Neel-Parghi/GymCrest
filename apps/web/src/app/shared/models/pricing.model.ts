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

/** "/month", "/year", "/3 months" or "/45 days", from the plan's length. */
export function planPeriodLabel(plan: Pick<PricingPlan, 'durationInDays'>): string {
    const days = plan.durationInDays;
    if (days >= 360 && days <= 366) return '/year';
    if (days >= 28 && days <= 31) return '/month';
    if (days % 30 === 0) return `/${days / 30} months`;
    return `/${days} days`;
}

/** "Up to 1,500 members · 5 branches"; empty when the plan has no limits set. */
export function planLimitsLabel(plan: Pick<PricingPlan, 'maxMembers' | 'maxBranches'>): string {
    const parts: string[] = [];
    if (plan.maxMembers) parts.push(`Up to ${plan.maxMembers.toLocaleString('en-IN')} members`);
    if (plan.maxBranches) parts.push(`${plan.maxBranches} ${plan.maxBranches === 1 ? 'branch' : 'branches'}`);
    return parts.join(' · ');
}
