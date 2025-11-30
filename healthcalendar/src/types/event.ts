// Type definitions for Events and Availability within the HealthCalendar MVP
// These types are client-side representations and can later be aligned with backend DTOs.

export type ISODate = string;      // YYYY-MM-DD
export type ISOTime = string;      // HH:mm

export interface Event {
  eventId: number;
  title: string;
  location: string;
  date: ISODate;      
  startTime: ISOTime;   
  endTime: ISOTime;    
  patientName?: string; // Added when viewed by worker
}

export interface Availability {
  id: number;
  day: string;     
  startTime: ISOTime;
  endTime: ISOTime;
}

export type UserRole = 'patient' | 'worker' | 'admin';

export interface User {
  id: number;
  name: string;
  email: string;
  role: UserRole;
  token?: string; // Mock auth token placeholder
}

// Utility type for creating a new event from a form
export type NewEventInput = Omit<Event, 'eventId'>;

// Update payload keeps full Event
export type UpdateEventInput = Event;
