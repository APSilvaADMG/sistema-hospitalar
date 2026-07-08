import type { ReactNode } from 'react';

type NavIconName =
  | 'menu' | 'dashboard' | 'bell' | 'users' | 'calendar' | 'siren' | 'stethoscope'
  | 'video' | 'sparkles' | 'user-circle' | 'bed' | 'heart-pulse' | 'utensils'
  | 'building' | 'ribbon' | 'droplets' | 'blood' | 'activity' | 'scalpel'
  | 'flask' | 'scan' | 'pill' | 'package' | 'shield' | 'wrench' | 'ambulance'
  | 'parking' | 'shirt' | 'lock' | 'wallet' | 'cart' | 'boxes' | 'briefcase'
  | 'file-text' | 'bar-chart' | 'clipboard' | 'plug' | 'log-out'
  | 'moon' | 'sun' | 'palette' | 'search' | 'mail';

const paths: Record<NavIconName, ReactNode> = {
  menu: <><path d="M4 6h16M4 12h16M4 18h16" /></>,
  dashboard: <><rect x="3" y="3" width="7" height="7" rx="1.5" /><rect x="14" y="3" width="7" height="7" rx="1.5" /><rect x="3" y="14" width="7" height="7" rx="1.5" /><rect x="14" y="14" width="7" height="7" rx="1.5" /></>,
  bell: <><path d="M18 8a6 6 0 10-12 0c0 7-3 7-3 7h18s-3 0-3-7" /><path d="M13.73 21a2 2 0 01-3.46 0" /></>,
  users: <><path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" /><circle cx="9" cy="7" r="4" /><path d="M23 21v-2a4 4 0 00-3-3.87M16 3.13a4 4 0 010 7.75" /></>,
  calendar: <><rect x="3" y="4" width="18" height="18" rx="2" /><path d="M16 2v4M8 2v4M3 10h18" /></>,
  siren: <><path d="M12 3v3M5.5 8.5l2 2M18.5 8.5l-2 2M3 14h3M18 14h3" /><path d="M12 22a8 8 0 100-16 8 8 0 000 16z" /><path d="M12 10v4l2 2" /></>,
  stethoscope: <><path d="M4.8 10.8a4 4 0 006.4 0M9 6V4a2 2 0 114 0v2" /><path d="M11 10v2a3 3 0 003 3h1a4 4 0 004-4v-1" /><circle cx="20" cy="10" r="2" /></>,
  video: <><path d="M23 7l-7 5 7 5V7z" /><rect x="1" y="5" width="15" height="14" rx="2" /></>,
  sparkles: <><path d="M12 3l1.5 4.5L18 9l-4.5 1.5L12 15l-1.5-4.5L6 9l4.5-1.5L12 3z" /><path d="M19 14l.75 2.25L22 17l-2.25.75L19 20l-.75-2.25L16 17l2.25-.75L19 14z" /></>,
  'user-circle': <><circle cx="12" cy="12" r="10" /><circle cx="12" cy="10" r="3" /><path d="M7 20.662V19a2 2 0 012-2h6a2 2 0 012 2v1.662" /></>,
  bed: <><path d="M2 4v16M2 8h18a4 4 0 014 4v1M2 17h20M6 8v9" /></>,
  'heart-pulse': <><path d="M19 14c1.49-1.46 3-3.21 3-5.5A5.5 5.5 0 0016.5 3c-1.76 0-3 .5-4.5 2-1.5-1.5-2.74-2-4.5-2A5.5 5.5 0 002 8.5c0 2.3 1.5 4.05 3 5.5l7 7 7-7z" /><path d="M3.5 12h5l1.5-3 2 6 1.5-3h5.5" /></>,
  utensils: <><path d="M3 2v7c0 1.1.9 2 2 2h0a2 2 0 002-2V2M7 2v20" /><path d="M21 15V2v0a5 5 0 00-5 5v6c0 1.1.9 2 2 2h3zm0 0v7" /></>,
  building: <><rect x="4" y="2" width="16" height="20" rx="2" /><path d="M9 22v-4h6v4M8 6h.01M16 6h.01M12 6h.01M8 10h.01M16 10h.01M12 10h.01M8 14h.01M16 14h.01M12 14h.01" /></>,
  ribbon: <><path d="M12 15l-2 5-2-5-5-1 4-3-1-5 5 3 5-3-1 5 4-3 5 1-2-5-2 5z" /></>,
  droplets: <><path d="M12 2.69l5.66 5.66a8 8 0 11-11.31 0L12 2.69z" /></>,
  blood: <><path d="M12 2.69l5.74 5.74a7.5 7.5 0 11-10.6 0L12 2.69z" /><path d="M12 11v6" /></>,
  activity: <><path d="M22 12h-4l-3 9L9 3l-3 9H2" /></>,
  scalpel: <><path d="M14.5 2.5l5 5L8 19l-5 1 1-5L14.5 2.5z" /><path d="M12 8l4 4" /></>,
  flask: <><path d="M9 3h6M10 3v6.76a6 6 0 11-2 0V3M14 3v6.76a6 6 0 102 0V3" /></>,
  scan: <><path d="M3 7V5a2 2 0 012-2h2M17 3h2a2 2 0 012 2v2M21 17v2a2 2 0 01-2 2h-2M7 21H5a2 2 0 01-2-2v-2" /><circle cx="12" cy="12" r="3" /></>,
  pill: <><path d="M10.5 20.5l-7-7a4.95 4.95 0 117-7l7 7a4.95 4.95 0 11-7 7z" /><path d="M8.5 8.5l7 7" /></>,
  package: <><path d="M16.5 9.4l-9-5.19M21 16V8a2 2 0 00-1-1.73l-7-4a2 2 0 00-2 0l-7 4A2 2 0 003 8v8a2 2 0 001 1.73l7 4a2 2 0 002 0l7-4A2 2 0 0021 16z" /><path d="M3.27 6.96L12 12.01l8.73-5.05M12 22.08V12" /></>,
  shield: <><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" /></>,
  wrench: <><path d="M14.7 6.3a1 1 0 000 1.4l1.6 1.6a1 1 0 001.4 0l3.77-3.77a6 6 0 01-7.94 7.94l-6.91 6.91a2.12 2.12 0 01-3-3l6.91-6.91a6 6 0 017.94-7.94l-3.76 3.76z" /></>,
  ambulance: <><path d="M10 10H6V6h10v8h-4" /><path d="M14 14h2l3-3V6h-4" /><circle cx="7.5" cy="17.5" r="2.5" /><circle cx="17.5" cy="17.5" r="2.5" /></>,
  parking: <><rect x="3" y="3" width="18" height="18" rx="2" /><path d="M9 17V7h4a3 3 0 010 6H9" /></>,
  shirt: <><path d="M20.38 3.46L16 2 12 5 8 2 3.62 3.46a2 2 0 00-1.34 2.23l.58 3.57a1 1 0 00.99.84H6v10c0 1.1.9 2 2 2h8a2 2 0 002-2V10h2.15a1 1 0 00.99-.84l.58-3.57a2 2 0 00-1.34-2.23z" /></>,
  lock: <><rect x="3" y="11" width="18" height="11" rx="2" /><path d="M7 11V7a5 5 0 0110 0v4" /></>,
  wallet: <><path d="M21 12V7H5a2 2 0 010-4h14v4" /><path d="M3 5v14a2 2 0 002 2h16v-5" /><path d="M18 12a2 2 0 100 4h4v-4h-4z" /></>,
  cart: <><circle cx="9" cy="21" r="1" /><circle cx="20" cy="21" r="1" /><path d="M1 1h4l2.68 13.39a2 2 0 002 1.61h9.72a2 2 0 002-1.61L23 6H6" /></>,
  boxes: <><path d="M21 16V8a2 2 0 00-1-1.73l-7-4a2 2 0 00-2 0l-7 4A2 2 0 003 8v8a2 2 0 001 1.73l7 4a2 2 0 002 0l7-4A2 2 0 0021 16z" /><path d="M3.27 6.96L12 12.01l8.73-5.05M12 22.08V12" /></>,
  briefcase: <><rect x="2" y="7" width="20" height="14" rx="2" /><path d="M16 7V5a2 2 0 00-2-2h-4a2 2 0 00-2 2v2" /></>,
  'file-text': <><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z" /><path d="M14 2v6h6M16 13H8M16 17H8M10 9H8" /></>,
  'bar-chart': <><path d="M12 20V10M18 20V4M6 20v-4" /></>,
  clipboard: <><path d="M16 4h2a2 2 0 012 2v14a2 2 0 01-2 2H6a2 2 0 01-2-2V6a2 2 0 012-2h2" /><rect x="8" y="2" width="8" height="4" rx="1" /></>,
  plug: <><path d="M12 22v-5M9 8V2M15 8V2M7 8h10v4a5 5 0 01-10 0V8z" /></>,
  'log-out': <><path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4M16 17l5-5-5-5M21 12H9" /></>,
  moon: <><path d="M21 12.79A9 9 0 1111.21 3 7 7 0 0021 12.79z" /></>,
  sun: <><circle cx="12" cy="12" r="5" /><path d="M12 1v2M12 21v2M4.22 4.22l1.42 1.42M18.36 18.36l1.42 1.42M1 12h2M21 12h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42" /></>,
  palette: <><path d="M12 22a10 10 0 100-20 10 10 0 000 20z" /><circle cx="8" cy="10" r="1.25" fill="currentColor" stroke="none" /><circle cx="12" cy="7" r="1.25" fill="currentColor" stroke="none" /><circle cx="16" cy="10" r="1.25" fill="currentColor" stroke="none" /><circle cx="15" cy="15" r="1.25" fill="currentColor" stroke="none" /></>,
  search: <><circle cx="11" cy="11" r="7" /><path d="M20 20l-3-3" /></>,
  mail: <><rect x="2" y="4" width="20" height="16" rx="2" /><path d="M22 6l-10 7L2 6" /></>,
};

export function NavIcon({ name }: { name: NavIconName }) {
  return (
    <svg className="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round" aria-hidden>
      {paths[name]}
    </svg>
  );
}

export type { NavIconName };
