import { DateRange, SchedulableDay } from '../models/workout-plan.model';

const WEEKDAY_NAMES = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

/** Monday 00:00 of the week containing `date`. */
export function startOfWeek(date: Date): Date {
  const monday = new Date(date);
  monday.setHours(0, 0, 0, 0);
  monday.setDate(monday.getDate() - ((monday.getDay() + 6) % 7));
  return monday;
}

/** Number of sets an exercise contributes (plans store sets as a number or numeric string; 3 when unset). */
export function exerciseSetCount(sets: number | string | undefined): number {
  return typeof sets === 'number' ? sets : parseInt(sets ?? '', 10) || 3;
}

/** Formats a date as YYYY-MM-DD in local time (avoids UTC offset shifting the day). */
export function toDateKey(date: Date): string {
  const yyyy = date.getFullYear();
  const mm = String(date.getMonth() + 1).padStart(2, '0');
  const dd = String(date.getDate()).padStart(2, '0');
  return `${yyyy}-${mm}-${dd}`;
}

/** Monday–Sunday of the week containing `date`, as YYYY-MM-DD keys. */
export function weekRange(date: Date): DateRange {
  const monday = startOfWeek(date);
  const sunday = new Date(monday);
  sunday.setDate(monday.getDate() + 6);
  return { from: toDateKey(monday), to: toDateKey(sunday) };
}

/**
 * How a session log counts for its day. "Planned" logs are one-off schedule overrides
 * saved ahead of time, so they are not a completed session.
 */
export function loggedState(status: string | undefined): 'done' | 'rest' | null {
  if (!status || status === PLANNED_STATUS) return null;
  return status === 'RestDay' ? 'rest' : 'done';
}

export const PLANNED_STATUS = 'Planned';

const SPLIT_WEEKDAYS = ['Monday', 'Wednesday', 'Friday'];

const namesWeekday = (dayName: string | undefined) =>
  WEEKDAY_NAMES.some(w => (dayName || '').toLowerCase().includes(w.toLowerCase()));

/**
 * Picks the plan day scheduled for a given date; mirrors WorkoutScheduleResolver in the API.
 * A day named after the weekday wins (weekly plans, and weekday overrides on any plan); otherwise the
 * unnamed split days (Day 1, Day 2…) fall on Monday / Wednesday / Friday. Returns null on rest days.
 */
export function resolveScheduledDay<T extends SchedulableDay>(days: T[], date: Date): T | null {
  if (!days || days.length === 0) return null;

  const weekday = WEEKDAY_NAMES[date.getDay()];
  const weekdayMatch = days.find(d => d.dayName && d.dayName.toLowerCase().includes(weekday.toLowerCase()));
  if (weekdayMatch) return weekdayMatch;

  const splitDays = days.filter(d => !namesWeekday(d.dayName));
  const slot = SPLIT_WEEKDAYS.indexOf(weekday);
  return slot >= 0 && slot < splitDays.length ? splitDays[slot] : null;
}
