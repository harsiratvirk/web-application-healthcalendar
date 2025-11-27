export type Role = 'Patient' | 'Worker' | 'Usermanager';

/**
 * Common fields for all user types.
 */
export interface BaseUser {
  sub: string;        // UserName/email (from backend JwtRegisteredClaimNames.Sub)
  name: string;       // Display name (from JwtRegisteredClaimNames.Name)
  nameid: string;     // User ID (ClaimTypes.NameIdentifier)
  role: Role;         // Role claim (ClaimTypes.Role normalized)
  jti: string;        // Token ID
  iat: number;        // Issued-at timestamp (Unix seconds)
  exp?: number;       // Optional: expiration timestamp if backend includes it
}

/**
 * Patient tokens always include an extra field: WorkerId.
 */
export interface PatientUser extends BaseUser {
  role: 'Patient';
  WorkerId: string;   // Related worker's ID ("-1" if none)
}

/**
 * Worker and UserManager tokens do not include WorkerId.
 */
export interface WorkerUser extends BaseUser {
  role: 'Worker' | 'Usermanager';
}

/**
 * Union type that covers all possible JWT users.
 */
export type JwtUser = PatientUser | WorkerUser;