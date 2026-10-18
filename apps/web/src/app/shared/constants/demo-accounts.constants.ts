export const DEMO_ACCOUNTS = {
  owner: { email: 'demo@gymcrest.com', password: 'GymOwner@19' },
  member: { email: 'demouser@gymcrest.com', password: 'DemoUser@19' }
} as const;

export type DemoAccountKey = keyof typeof DEMO_ACCOUNTS;
