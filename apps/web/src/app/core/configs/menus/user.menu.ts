import { NavItem } from '../../models/nav-Item.model';

export const UserMenu: NavItem[] = [
    // {
    //     label: 'Main',
    //     isHeading: true,
    //     roles: ['User'],
    //     icon: ''
    // },
    {
        label: 'Dashboard',
        icon: 'fa-solid fa-house',
        route: '/user/dashboard',
        roles: ['User']
    },
    // {
    //     label: 'Winter Arc (beta)',
    //     icon: 'fa-solid fa-snowflake',
    //     route: '/user/winter-arc',
    //     roles: ['User']
    // },
    {
        label: 'Fitness',
        isHeading: true,
        roles: ['User'],
        icon: ''
    },
    {
        label: 'Track Performance',
        icon: 'fa-solid fa-chart-line',
        route: '/user/performance',
        roles: ['User']
    },
    {
        label: 'Workout Planner',
        icon: 'fa-solid fa-calendar-check',
        route: '/user/workout-planner',
        roles: ['User']
    },
    {
        label: 'Workout Calendar',
        icon: 'fa-solid fa-calendar-days',
        route: '/user/workout-calendar',
        roles: ['User']
    },
    {
        label: 'Exercise Progress',
        icon: 'fa-solid fa-arrow-trend-up',
        route: '/user/exercise-progress',
        roles: ['User']
    },
    {
        label: 'Diet Planner',
        icon: 'fa-solid fa-utensils',
        route: '/user/diet-planner',
        roles: ['User']
    },
    {
        label: 'Diet Tracker',
        icon: 'fa-solid fa-clipboard-list',
        route: '/user/diet-tracker',
        roles: ['User']
    },
    {
        label: 'Health Tracker',
        icon: 'fa-solid fa-heart-pulse',
        route: '/user/health-tracker',
        roles: ['User']
    },
    {
        label: 'Account',
        isHeading: true,
        roles: ['User'],
        icon: ''
    },
    {
        label: 'My Account',
        icon: 'fa-solid fa-user-circle',
        route: '/user/profile',
        roles: ['User']
    },
    {
        label: 'Billing & Payments',
        icon: 'fa-solid fa-file-invoice-dollar',
        route: '/user/billing',
        roles: ['User']
    },
    {
        label: 'Settings',
        icon: 'fa-solid fa-gear',
        route: '/user/settings',
        roles: ['User']
    }
];
