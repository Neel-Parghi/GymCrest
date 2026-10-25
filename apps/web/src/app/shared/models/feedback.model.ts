export type FeedbackCategory = 'Bug' | 'Idea' | 'Other';

export interface FeedbackItem {
  id: string;
  category: FeedbackCategory;
  message: string;
  pageUrl?: string;
  userAgent?: string;
  userEmail?: string;
  userRole?: string;
  isResolved: boolean;
  createdOn: string;
}
