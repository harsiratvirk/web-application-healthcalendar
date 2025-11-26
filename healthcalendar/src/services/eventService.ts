// API service layer for HealthCalendar - Patient CRUD operations
// Provides async operations for Events, Availability, and Schedules

import type { Event, Availability, NewEventInput, UpdateEventInput } from '../types/event';

// Base API URL
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5080/api';

// Helper to get auth token from localStorage
function getAuthToken(): string | null {
	return localStorage.getItem('hc_token');
}

// Helper to create headers with auth token
function getHeaders(): HeadersInit {
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
async function handleResponse<T>(response: Response): Promise<T> {
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

// Convert frontend Event format to backend EventDTO format
function toEventDTO(event: Event | NewEventInput, userId: string): any {
	const [fromHH, fromMM] = event.startTime.split(':');
	const [toHH, toMM] = event.endTime.split(':');
	
	return {
		EventId: 'eventId' in event ? event.eventId : 0,
		From: `${fromHH}:${fromMM}:00`,
		To: `${toHH}:${toMM}:00`,
		Date: event.date,
		Title: event.title,
		Location: event.location,
		UserId: userId
	};
}

// Convert backend EventDTO to frontend Event format
function fromEventDTO(dto: any): Event {
	return {
		eventId: dto.EventId || dto.eventId,
		title: dto.Title || dto.title,
		location: dto.Location || dto.location,
		date: dto.Date || dto.date,
		startTime: (dto.From || dto.from).substring(0, 5), // "HH:MM:SS" -> "HH:MM"
		endTime: (dto.To || dto.to).substring(0, 5),
		patientName: dto.OwnerName || dto.ownerName
	};
}

// Convert backend AvailabilityDTO to frontend Availability format
function fromAvailabilityDTO(dto: any): Availability {
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
		day: dayOfWeekMap[dto.DayOfWeek] || dto.dayOfWeek,
		startTime: (dto.From || dto.from).substring(0, 5),
		endTime: (dto.To || dto.to).substring(0, 5)
	};
}

// Public API surface
export const apiService = {
	// Get patient's events for a specific week
	async getWeeksEventsForPatient(userId: string, monday: string): Promise<Event[]> {
		try {
			const response = await fetch(
				`${API_BASE_URL}/Event/getWeeksEventsForPatient?userId=${encodeURIComponent(userId)}&monday=${monday}`,
				{
					method: 'GET',
					headers: getHeaders()
				}
			);
			const dtos = await handleResponse<any[]>(response);
			return dtos.map(fromEventDTO);
		} catch (err) {
			throw normalizeError(err);
		}
	},

	// Get worker's availability for a specific week (excluding overlapping ones)
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

	// Get a specific event by ID
	async getEvent(eventId: number): Promise<Event> {
		try {
			const response = await fetch(
				`${API_BASE_URL}/Event/getEvent/${eventId}`,
				{
					method: 'GET',
					headers: getHeaders()
				}
			);
			const dto = await handleResponse<any>(response);
			return fromEventDTO(dto);
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


	// Validate event for create operation
	async validateEventForCreate(event: NewEventInput, userId: string, userIds: string[]): Promise<void> {
		try {
			const eventDTO = toEventDTO(event, userId);
			const queryParams = new URLSearchParams();
			userIds.filter(id => id != null).forEach(id => queryParams.append('userIds', encodeURIComponent(id)));
			const response = await fetch(
				`${API_BASE_URL}/Event/validateEventForCreate?${queryParams.toString()}`,
				{
					method: 'POST',
					headers: getHeaders(),
					body: JSON.stringify(eventDTO)
				}
			);
			await handleResponse<any>(response);
		} catch (err) {
			throw normalizeError(err);
		}
	},

	// Check availability for create operation - returns list of availability IDs
	async checkAvailabilityForCreate(event: NewEventInput, userId: string): Promise<number[]> {
		try {
			const eventDTO = toEventDTO(event, userId);
			// Debug log: print payload before sending
			console.debug('checkAvailabilityForCreate payload:', eventDTO);
			const response = await fetch(
				`${API_BASE_URL}/Availability/checkAvailabilityForCreate?userId=${encodeURIComponent(userId)}`,
				{
					method: 'POST',
					headers: getHeaders(),
					body: JSON.stringify(eventDTO)
				}
			);
			const availabilityIds = await handleResponse<number[]>(response);
			return availabilityIds;
		} catch (err) {
			throw normalizeError(err);
		}
	},

	// Create a new event
	async createEvent(input: NewEventInput, userId: string): Promise<Event> {
		try {
			const eventDTO = toEventDTO(input, userId);
			const response = await fetch(
				`${API_BASE_URL}/Event/createEvent`,
				{
					method: 'POST',
					headers: getHeaders(),
					body: JSON.stringify(eventDTO)
				}
			);
			// Backend returns { EventId: number } with capital E
			const result = await handleResponse<{ EventId: number }>(response);
			// Backend returns the created event's ID
			return {
				eventId: result.EventId,
				...input
			};
		} catch (err) {
			throw normalizeError(err);
		}
	},

	// Create schedules for an event
	async createSchedules(eventId: number, date: string, availabilityIds: number[]): Promise<void> {
		try {
			const queryParams = new URLSearchParams();
			availabilityIds.filter(id => id != null).forEach(id => queryParams.append('availabilityIds', String(id)));
			queryParams.append('eventId', String(eventId));
			queryParams.append('date', String(date));

			const response = await fetch(
				`${API_BASE_URL}/Schedule/createSchedules?${queryParams.toString()}`,
				{
					method: 'POST',
					headers: getHeaders()
				}
			);
			await handleResponse<any>(response);
		} catch (err) {
			throw normalizeError(err);
		}
	},

	// Validate event for update operation
	async validateEventForUpdate(event: Event, userId: string, userIds: string[]): Promise<void> {
		try {
			const eventDTO = toEventDTO(event, userId);
			const queryParams = new URLSearchParams();
			userIds.filter(id => id != null).forEach(id => queryParams.append('userIds', encodeURIComponent(id)));
			const response = await fetch(
				`${API_BASE_URL}/Event/validateEventForUpdate?${queryParams.toString()}`,
				{
					method: 'POST',
					headers: getHeaders(),
					body: JSON.stringify(eventDTO)
				}
			);
			await handleResponse<any>(response);
		} catch (err) {
			throw normalizeError(err);
		}
	},

	// Check availability for update operation - returns three lists of availability IDs
	async checkAvailabilityForUpdate(
		updatedEvent: Event,
		oldDate: string,
		oldFrom: string,
		oldTo: string,
		workerId: string
	): Promise<{ forCreateSchedules: number[]; forDeleteSchedules: number[]; forUpdateSchedules: number[] }> {
		try {
			// Get patientId from the event (for DTO), but use workerId for availability check
			const patientId = (updatedEvent as any).userId || workerId; // fallback to workerId if userId not on event
			const eventDTO = toEventDTO(updatedEvent, patientId);
			const response = await fetch(
				`${API_BASE_URL}/Availability/checkAvailabilityForUpdate?oldDate=${oldDate}&oldFrom=${oldFrom}:00&oldTo=${oldTo}:00&userId=${workerId}`,
				{
					method: 'POST',
					headers: getHeaders(),
					body: JSON.stringify(eventDTO)
				}
			);
			const result = await handleResponse<any>(response);
			return {
				forCreateSchedules: result.ForCreateSchedules || result.forCreateSchedules || [],
				forDeleteSchedules: result.ForDeleteSchedules || result.forDeleteSchedules || [],
				forUpdateSchedules: result.ForUpdateSchedules || result.forUpdateSchedules || []
			};
		} catch (err) {
			throw normalizeError(err);
		}
	},

	// Update an existing event
	async updateEvent(update: UpdateEventInput, userId: string): Promise<Event> {
		try {
			const eventDTO = toEventDTO(update, userId);
			const response = await fetch(
				`${API_BASE_URL}/Event/updateEvent`,
				{
					method: 'PUT',
					headers: getHeaders(),
					body: JSON.stringify(eventDTO)
				}
			);
			await handleResponse<any>(response);
			return update as Event;
		} catch (err) {
			throw normalizeError(err);
		}
	},

	// Delete schedules by availability IDs after event update
	async deleteSchedulesByAvailabilityIds(eventId: number, availabilityIds: number[]): Promise<void> {
		try {
			const queryParams = new URLSearchParams();
			queryParams.append('eventId', eventId.toString());
			availabilityIds.forEach(id => queryParams.append('availabilityIds', id.toString()));

			const response = await fetch(
				`${API_BASE_URL}/Schedule/deleteSchedulesAfterEventUpdate?${queryParams.toString()}`,
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

	// Update schedules with new event
	async updateScheduledEvent(eventId: number, availabilityIds: number[]): Promise<void> {
		try {
			const queryParams = new URLSearchParams();
			queryParams.append('eventId', eventId.toString());
			availabilityIds.forEach(id => queryParams.append('availabilityIds', id.toString()));

			const response = await fetch(
				`${API_BASE_URL}/Schedule/updateScheduledEvent?${queryParams.toString()}`,
				{
					method: 'PUT',
					headers: getHeaders()
				}
			);
			await handleResponse<any>(response);
		} catch (err) {
			throw normalizeError(err);
		}
	},

	// Delete schedules by event ID
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

	// Delete an event
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

	// Legacy mock methods for backward compatibility
	async getEvents(): Promise<Event[]> {
		// This is deprecated - use getWeeksEventsForPatient instead
		console.warn('getEvents() is deprecated. Use getWeeksEventsForPatient() instead.');
		return [];
	},

	async getAvailability(): Promise<Availability[]> {
		// This is deprecated - use getWeeksAvailabilityProper instead
		console.warn('getAvailability() is deprecated. Use getWeeksAvailabilityProper() instead.');
		return [];
	},

	async updateAvailability(_list: Availability[]): Promise<void> {
		// Worker functionality - not implemented for patient
		console.warn('updateAvailability() is for worker components only.');
	}
};

// Normalize unknown error types
function normalizeError(err: unknown): Error {
	if (err instanceof Error) return err;
	return new Error('Unknown error occurred');
}

export type { Event, Availability };
