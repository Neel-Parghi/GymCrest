export type DayPart = 'morning' | 'afternoon' | 'evening';

/** One rule for every greeting in the app: morning until 12, afternoon until 6 PM, evening after. */
export function dayPart(hour = new Date().getHours()): DayPart {
  if (hour < 12) return 'morning';
  if (hour < 18) return 'afternoon';
  return 'evening';
}

export function greetingFor(hour = new Date().getHours()): string {
  return `Good ${dayPart(hour)}`;
}
