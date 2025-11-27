// Type definitions for Events and Availability within the HealthCalendar MVP
// These types are client-side representations and can later be aligned with backend DTOs.

export type ISODate = string;      // YYYY-MM-DD
export type ISOTime = string;      // HH:mm

export interface Event {
  eventId: number;
  title: string;
  location: string;
  date: ISODate;        // Day of event
  startTime: ISOTime;   // Inclusive start
  endTime: ISOTime;     // Exclusive end (convention)
  patientName?: string; // Added when viewed by worker
}

export interface Availability {
  id: number;
  day: string;       // Monday, Tuesday, etc.
  startTime: ISOTime;
  endTime: ISOTime;
}

export type UserRole = 'patient' | 'worker' | 'usermanager';

// Basic user type kept minimal for MVP forms and role-based rendering.
export interface User {
  id: number;
  name: string;
  email: string;
  role: UserRole;
  token?: string; // Mock auth token placeholder
}

// Utility type for creating a new event from a form (without id yet)
export type NewEventInput = Omit<Event, 'eventId'>;

// Update payload keeps full Event in this MVP
export type UpdateEventInput = Event;
