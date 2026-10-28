import { NavItem } from "../../models/nav-Item.model";

export const GymOwnerMenu: NavItem[] = [
    {
        label: 'Dashboard',
        icon: 'fa-solid fa-house',
        route: '/gym-owner/dashboard',
        roles: ['GymOwner', 'Staff']
    },
    {
        label: 'MANAGEMENT',
        icon: '',
        roles: ['GymOwner', 'Staff'],
        isHeading: true
    },
    {
        label: 'Members',
        icon: 'fa-solid fa-users',
        route: '/gym-owner/members',
        roles: ['GymOwner', 'Staff']
    },
    {
        label: 'Attendance',
        icon: 'fa-solid fa-calendar-check',
        route: '/gym-owner/attendance',
        roles: ['GymOwner', 'Staff']
    },
    {
        label: 'Staff & Trainers',
        icon: 'fa-solid fa-user-tie',
        route: '/gym-owner/staff',
        roles: ['GymOwner', 'Staff']
    },
    {
        label: 'Gym Plans',
        icon: 'fa-solid fa-receipt',
        route: '/gym-owner/plans',
        roles: ['GymOwner']
    },
    {
        label: 'Announcements',
        icon: 'fa-solid fa-bullhorn',
        route: '/gym-owner/announcements',
        roles: ['GymOwner', 'Staff']
    },
    {
        label: 'RESOURCES',
        icon: '',
        roles: ['GymOwner', 'Staff'],
        isHeading: true
    },
    {
        label: 'Inventory',
        icon: 'fa-solid fa-boxes-stacked',
        route: '/gym-owner/inventory',
        roles: ['GymOwner', 'Staff']
    },
    {
        label: 'My Gyms',
        icon: 'fa-solid fa-building',
        route: '/gym-owner/my-gyms',
        roles: ['GymOwner'] 
    },
    {
        label: 'Workout Library',
        icon: 'fa-solid fa-dumbbell',
        route: '/gym-owner/resources/workout-library',
        roles: ['GymOwner', 'Staff']
    },
    {
        label: 'Diet Library',
        icon: 'fa-solid fa-utensils',
        route: '/gym-owner/resources/diet-library',
        roles: ['GymOwner', 'Staff']
    },
    {
        label: 'FINANCES',
        icon: '',
        roles: ['GymOwner', 'Staff'],
        isHeading: true
    },
    {
        label: 'Billing & Invoices',
        icon: 'fa-solid fa-file-invoice-dollar',
        route: '/gym-owner/billing',
        roles: ['GymOwner', 'Staff']
    },
    {
        label: 'ACCOUNT',
        icon: '',
        roles: ['GymOwner', 'Staff'],
        isHeading: true
    },
    {
        label: 'My Account',
        icon: 'fa-solid fa-user-circle',
        route: '/gym-owner/profile',
        roles: ['GymOwner', 'Staff']
    },
    {
        label: 'Settings',
        icon: 'fa-solid fa-gear',
        route: '/gym-owner/settings',
        roles: ['GymOwner', 'Staff']
    }
];
