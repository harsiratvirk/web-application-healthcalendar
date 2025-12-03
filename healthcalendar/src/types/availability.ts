// Type definitions for Availability within the HealthCalendar MVP
// These types are client-side representations and can later be aligned with backend DTOs.

export type ISODate = string;      // YYYY-MM-DD
export type ISOTime = string;      // HH:mm

export interface Availability {
  id: number;
  day: string;     
  startTime: ISOTime;
  endTime: ISOTime;
  date?: ISODate;    // Specific date for non-continuous availability
}