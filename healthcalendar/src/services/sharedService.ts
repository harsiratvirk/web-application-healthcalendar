import type { Availability } from '../types/event';

export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5080/api';

// Helper to get auth token from localStorage
export function getAuthToken(): string | null {
  return localStorage.getItem('hc_token');
}

// Helper to create headers with auth token
// Adds Authorization header with Bearer token for authenticated requests
export function getHeaders(): HeadersInit {
  const token = getAuthToken();
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
  };
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  return headers;
}

// Helper to handle API responses
// Throws descriptive errors for HTTP failures, parses JSON responses
export async function handleResponse<T>(response: Response): Promise<T> {
	if (!response.ok) {
		const errorText = await response.text();
		if (response.status === 406) {
			throw new Error('Request not acceptable: ' + errorText);
		} else if (response.status === 500) {
			throw new Error('Server error: ' + errorText);
		} else if (response.status === 401) {
			throw new Error('Unauthorized. Please log in again.');
		} else {
			throw new Error(`HTTP ${response.status}: ${errorText}`);
		}
	}
	const contentType = response.headers.get('content-type');
	if (contentType && contentType.includes('application/json')) {
		return await response.json();
	}
	return {} as T;
}

// Normalize unknown error types
export function normalizeError(err: unknown): Error {
	if (err instanceof Error) return err;
	return new Error('Unknown error occurred');
}


// Backend DTO to JS format
export function fromAvailabilityDTO(dto: any): Availability {
	const dayOfWeekMap: { [key: number]: string } = {
		0: 'Sunday',
		1: 'Monday',
		2: 'Tuesday',
		3: 'Wednesday',
		4: 'Thursday',
		5: 'Friday',
		6: 'Saturday'
	};
	
	return {
		id: dto.AvailabilityId || dto.availabilityId,
		day: dayOfWeekMap[dto.DayOfWeek ?? dto.dayOfWeek] || 'Monday',
		startTime: (dto.From || dto.from).substring(0, 5), // "HH:MM:SS" -> "HH:MM"
		endTime: (dto.To || dto.to).substring(0, 5),
		date: dto.Date || dto.date || undefined
	};
}

// Represents a user (Patient, Worker, or Admin) in the system
export interface UserDTO {
  Id: string;             
  UserName: string;       
  Name: string;            
  Role: string;            // User role: "Patient", "Worker", or "Admin"
  WorkerId?: string;       // ID of assigned worker (only for patients)
}


// public API service
export const sharedService = {

    // Get patients assigned to a specific worker
    async getUsersByWorkerId(workerId: string): Promise<UserDTO[]> {
        try {
			const response = await fetch(`${API_BASE_URL}/User/getUsersByWorkerId?workerId=${encodeURIComponent(workerId)}`, {
			method: 'GET',
			headers: getHeaders(),
			});
			return handleResponse<UserDTO[]>(response);
		} catch (err) {
            throw normalizeError(err);
        }
    },
    
    // Get Users Ids by their WorkerId
    async getIdsByWorkerId(workerId: string): Promise<string[]> {
        try {
            const response = await fetch(
                `${API_BASE_URL}/User/getIdsByWorkerId?workerId=${encodeURIComponent(workerId)}`,
                {
                    method: 'GET',
                    headers: getHeaders()
                }
            )
            const userIds = await handleResponse<string[]>(response);
            return userIds;
        } catch (err) {
            throw normalizeError(err);
        }
    },
    
    // Get worker's availability for a specific week (excluding overlapping ones)
    // Used in EventCalendarPage to show available time slots for booking
    async getWeeksAvailabilityProper(workerId: string, monday: string): Promise<Availability[]> {
        try {
            const response = await fetch(
                `${API_BASE_URL}/Availability/getWeeksAvailabilityProper?userId=${encodeURIComponent(workerId)}&monday=${monday}`,
                {
                    method: 'GET',
                    headers: getHeaders()
                }
            );
            const dtos = await handleResponse<any[]>(response);
            return dtos.map(fromAvailabilityDTO);
        } catch (err) {
            throw normalizeError(err);
        }
    },

    // Delete an event
	// Step 2 of delete event workflow: removes event record from database
	// Must delete schedules first to avoid foreign key constraint violation
	async deleteEvent(eventId: number): Promise<void> {
		try {
			const response = await fetch(
				`${API_BASE_URL}/Event/deleteEvent/${eventId}`,
				{
					method: 'DELETE',
					headers: getHeaders()
				}
			);
			await handleResponse<any>(response);
		} catch (err) {
			throw normalizeError(err);
		}
	},

    // Delete Events by list of EventIds
	async deleteEventsByIds(eventIds: number[]): Promise<void> {
		try {
			const queryParams = new URLSearchParams();
			eventIds.forEach(id => queryParams.append('eventIds', id.toString()));
			
			const response = await fetch(
				`${API_BASE_URL}/Event/deleteEventsByIds?${queryParams.toString()}`,
				{
					method: 'DELETE',
					headers: getHeaders()
				}
			);
			await handleResponse<any>(response);
		} catch (err) {
			throw normalizeError(err);
		}
	},

    // Delete schedules by event ID
	// Step 1 of delete event workflow: removes all schedule links for this event
	// Frees up the availability slots so other patients can book them
	async deleteSchedulesByEventId(eventId: number): Promise<void> {
		try {
			const response = await fetch(
				`${API_BASE_URL}/Schedule/deleteSchedulesByEventId?eventId=${eventId}`,
				{
					method: 'DELETE',
					headers: getHeaders()
				}
			);
			await handleResponse<any>(response);
		} catch (err) {
			throw normalizeError(err);
		}
	},

    // Delete Schedules by list of EventIds
	async deleteSchedulesByEventIds(eventIds: number[]): Promise<void> {
		try {
			const queryParams = new URLSearchParams();
			eventIds.forEach(id => queryParams.append('eventIds', id.toString()));
			
			const response = await fetch(
				`${API_BASE_URL}/Schedule/deleteSchedulesByEventIds?${queryParams.toString()}`,
				{
					method: 'DELETE',
					headers: getHeaders()
				}
			);
			await handleResponse<any>(response);
		} catch (err) {
			throw normalizeError(err);
		}
	},
}