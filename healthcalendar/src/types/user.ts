// Type definitions for Users within the HealthCalendar MVP
// These types are client-side representations and can later be aligned with backend DTOs.

export type Role = 'Patient' | 'Worker' | 'Admin';

// Common fields for all user types
export interface BaseUser {
  sub: string;       
  name: string;   
  nameid: string; 
  role: Role;        
  jti: string; 
  iat: number;     
  exp?: number;       
}

export interface PatientUser extends BaseUser {
  role: 'Patient';
  WorkerId: string;
}

/**
 * Worker and Admin tokens do not include WorkerId.
 */
export interface WorkerUser extends BaseUser {
  role: 'Worker' | 'Admin';
}

// Union type that covers all JWT users
export type JwtUser = PatientUser | WorkerUser;