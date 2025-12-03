import type { Event, NewEventInput, UpdateEventInput } from '../types/event.ts';
import type { Availability } from '../types/availability.ts';
// Imports functions shared with other services
import { API_BASE_URL, getHeaders, handleResponse, normalizeError, fromAvailabilityDTO } from './sharedService.ts'

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

// Public API surface
export const patientService = {
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

	// Get patient's events for a specific week
	// Used in EventCalendarPage to load patient's own events
	async getWeeksEventsByUserId(userId: string, monday: string): Promise<Event[]> {
		try {
			const response = await fetch(
				`${API_BASE_URL}/Event/getWeeksEventsByUserId?userId=${encodeURIComponent(userId)}&monday=${monday}`,
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

	// Get several patients events for a specific week
	// Used to detect conflicts and show other patients' bookings
	async getWeeksEventsByUserIds(userIds: string[], monday: string): Promise<Event[]> {
		try {
			const queryParams = new URLSearchParams();
			userIds.filter(id => id != null).forEach(id => queryParams.append('userIds', encodeURIComponent(id)));
			queryParams.append('monday', String(monday));
			const response = await fetch(
				`${API_BASE_URL}/Event/getWeeksEventsByUserIds?${queryParams.toString()}`,
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

	// Validate event for create operation
	// Step 1 of create workflow: checks for conflicts with other patients' events
	async validateEventForCreate(event: NewEventInput, userId: string, userIds: string[]): Promise<void> {
		try {
			const eventDTO = toEventDTO(event, userId);
			const queryParams = new URLSearchParams();
			// Pass all patient IDs for conflict checking
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
	// Step 2: finds which availability slots the event will occupy
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
	// Step 3: inserts event into database
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
			const result = await handleResponse<{ EventId: number }>(response);
			return {
				eventId: result.EventId,
				...input
			};
		} catch (err) {
			throw normalizeError(err);
		}
	},

	// Create schedules for an event
	// Step 4: links event to specific availability slots
	// Schedule rows mark which availability slots are booked by this event
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
	// Step 1 of update workflow: checks for conflicts excluding the event being updated
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
	// Step 2: compares old and new event times to determine schedule changes
	async checkAvailabilityForUpdate(
		updatedEvent: Event,
		oldDate: string,
		oldFrom: string,
		oldTo: string,
		workerId: string
	): Promise<{ forCreateSchedules: number[]; forDeleteSchedules: number[]; forUpdateSchedules: number[] }> {
		try {
			const patientId = (updatedEvent as any).userId || workerId;
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
	// Step 3: updates event record in database
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
	// Step 4: removes schedule links for availability slots no longer used
	async deleteSchedulesAfterEventUpdate(eventId: number, availabilityIds: number[]): Promise<void> {
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
	// Step 5: updates existing schedule links that remain valid
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
};

export type { Event, Availability };
