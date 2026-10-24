import { BuddyFacts } from '../../../../../core/models/user-dashboard.model';
import { dayPart } from '../../../../../shared/utils/greeting';

/** One thing the buddy says, with the Font Awesome icon shown beside it. */
export interface BuddyLine {
  text: string;
  icon: string;
}

function hello(name: string, hour: number): string {
  const part = dayPart(hour);
  if (part === 'morning') return `Morning, ${name}!`;
  if (part === 'evening') return `Evening, ${name}!`;
  return `Hey ${name}!`;
}

/** What the buddy says about today's workout. */
function todayLine(f: BuddyFacts): BuddyLine {
  switch (f.state) {
    case 'ready': return { text: `${f.workoutTitle} day! Let's get it.`, icon: 'fa-dumbbell' };
    case 'done': return { text: `${f.workoutTitle} done. So proud of you!`, icon: 'fa-medal' };
    case 'rest': return { text: 'Rest day. Recover like a pro.', icon: 'fa-bed' };
    case 'none': return { text: "Let's pick a plan together!", icon: 'fa-clipboard-list' };
    default: return { text: 'Warming up...', icon: 'fa-person-running' };
  }
}

/** Little things the buddy "knows" about the member, shown one per tap. */
function factLines(f: BuddyFacts): BuddyLine[] {
  const lines: BuddyLine[] = [];

  if (f.streakAtRisk && f.streak > 0) lines.push({ text: `Your ${f.streak}-day streak needs you today!`, icon: 'fa-fire' });
  else if (f.streak >= 2) lines.push({ text: `${f.streak}-day streak. Keep it alive!`, icon: 'fa-fire' });

  if (f.monthlyTarget > 0) {
    const left = f.monthlyTarget - f.monthlyCount;
    lines.push({
      text: left > 0
        ? `${f.monthlyCount}/${f.monthlyTarget} sessions this month. ${left} to go!`
        : `Monthly goal smashed: ${f.monthlyCount} sessions!`,
      icon: 'fa-calendar-check'
    });
  }

  if (f.topRecord) lines.push({ text: `Your ${f.topRecord.name} PR is ${f.topRecord.weight}. Beat it?`, icon: 'fa-trophy' });
  if (f.caloriesLeft !== null && f.caloriesLeft > 0) {
    lines.push({ text: `${Math.round(f.caloriesLeft)} kcal left to eat today.`, icon: 'fa-utensils' });
  }
  if (f.readyMuscle) lines.push({ text: `${f.readyMuscle} is fully recovered and ready.`, icon: 'fa-heart-pulse' });
  if (f.goalTitle) lines.push({ text: `${f.goalTitle} mode: on. I see you!`, icon: 'fa-bullseye' });

  lines.push(
    { text: 'Water break? Your muscles say yes.', icon: 'fa-droplet' },
    { text: 'Tap me anytime for a pep talk!', icon: 'fa-face-smile-wink' }
  );
  return lines;
}

/** The buddy's script: hello, today's plan, then personal facts for each tap. */
export function buddyLines(f: BuddyFacts, hour = new Date().getHours()): BuddyLine[] {
  const greeting = f.isFirstTime ? `Hi ${f.firstName}! I'm Forgie, your gym buddy.` : hello(f.firstName, hour);
  return [{ text: greeting, icon: 'fa-hand' }, todayLine(f), ...factLines(f)];
}
