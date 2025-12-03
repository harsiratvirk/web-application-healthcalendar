import type { Availability } from '../types/availability.ts';
// imports DTOs shared with other services
import type { UserDTO } from './sharedService.ts';
// Imports constants and functions shared with other services
import { API_BASE_URL, getHeaders, handleResponse, normalizeError, fromAvailabilityDTO } from './sharedService.ts'

interface AvailabilityDTO {
	AvailabilityId?: number;
	From: string;          // HH:MM:SS
	To: string;            // HH:MM:SS
	DayOfWeek: number;     // 0 = sunday, 1 = monday, etc
	Date?: string | null;  // YYYY-MM-DD
	UserId: string;
}

interface NewAvailabilityInput {
	startTime: string;     // HH:MM
	endTime: string;       // HH:MM
	dayOfWeek: number;     // 0-6
	date?: string | null;  // YYYY-MM-DD
}

// Helper to convert Availability to AvailabilityDTO
function toAvailabilityDTO(availability: NewAvailabilityInput | Availability, userId: string): AvailabilityDTO {
	const [fromHH, fromMM] = availability.startTime.split(':');
	const [toHH, toMM] = availability.endTime.split(':');
	
	let dayOfWeekNum: number;
	if ('day' in availability && typeof availability.day === 'string') {
		const dayMap: { [key: string]: number } = {
			'sunday': 0, 'monday': 1, 'tuesday': 2, 'wednesday': 3,
			'thursday': 4, 'friday': 5, 'saturday': 6
		};
		dayOfWeekNum = dayMap[availability.day] ?? 0;
	} else if ('dayOfWeek' in availability) {
		dayOfWeekNum = availability.dayOfWeek;
	} else {
		dayOfWeekNum = 0;
	}
	
	return {
		AvailabilityId: 'id' in availability ? availability.id : undefined,
		From: `${fromHH}:${fromMM}:00`,
		To: `${toHH}:${toMM}:00`,
		DayOfWeek: dayOfWeekNum,
		Date: ('date' in availability && availability.date) ? availability.date : null,
		UserId: userId
	};
}

export const workerService = {
	// Get week's events for worker (all events from assigned patients)
	async getWeeksEventsForWorker(patients: UserDTO[], monday: string): Promise<any[]> {
		try {
			const response = await fetch(
				`${API_BASE_URL}/Event/getWeeksEventsForWorker?monday=${monday}`,
				{
					method: 'POST',
					headers: getHeaders(),
					body: JSON.stringify(patients)
				}
			);
			const eventDTOs = await handleResponse<any[]>(response);
			return eventDTOs.map(dto => ({
				eventId: dto.EventId || dto.eventId,
				title: dto.Title || dto.title,
				location: dto.Location || dto.location,
				date: dto.Date || dto.date,
				startTime: (dto.From || dto.from).substring(0, 5),
				endTime: (dto.To || dto.to).substring(0, 5),
				patientName: dto.OwnerName || dto.ownerName
			}));
		} catch (err) {
			throw normalizeError(err);
		}
	},
    
	// Create availability
	async createAvailability(availability: NewAvailabilityInput, userId: string): Promise<void> {
		try {
			const availabilityDTO = toAvailabilityDTO(availability, userId);
			const response = await fetch(
				`${API_BASE_URL}/Availability/createAvailability`,
				{
					method: 'POST',
					headers: getHeaders(),
					body: JSON.stringify(availabilityDTO)
				}
			);
			await handleResponse<any>(response);
		} catch (err) {
			throw normalizeError(err);
		}
	},

	// Get all availability for a week (including overlaps)
	async getAllWeeksAvailability(workerId: string, monday: string): Promise<Availability[]> {
		try {
			const response = await fetch(
				`${API_BASE_URL}/Availability/getAllWeeksAvailability?userId=${workerId}&monday=${monday}`,
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

	// Get availability for a week (overlaps removed)
	async getWeeksAvailabilityProper(workerId: string, monday: string): Promise<Availability[]> {
		try {
			const response = await fetch(
				`${API_BASE_URL}/Availability/getWeeksAvailabilityProper?userId=${workerId}&monday=${monday}`,
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

	// Delete availability by ID
	async deleteAvailability(availabilityId: number): Promise<void> {
		try {
			const response = await fetch(
				`${API_BASE_URL}/Availability/deleteAvailability/${availabilityId}`,
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

	// Delete availability by day of week and from
	async deleteAvailabilityByDoW(userId: string, dayOfWeek: number, from: string): Promise<void> {
		try {
			// Fixes HH:MM:SS format
			const fromTime = from.length === 5 ? `${from}:00` : from;
			
			const response = await fetch(
				`${API_BASE_URL}/Availability/deleteAvailabilityByDoW?userId=${encodeURIComponent(userId)}&dayOfWeek=${dayOfWeek}&from=${encodeURIComponent(fromTime)}`,
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

	// Find scheduled eventID for an availability on a specific date
	async findScheduledEventId(availabilityId: number, date: string): Promise<number> {
		try {
			const response = await fetch(
				`${API_BASE_URL}/Schedule/findScheduledEventId?availabilityId=${availabilityId}&date=${date}`,
				{
					method: 'GET',
					headers: getHeaders()
				}
			);
			return await handleResponse<number>(response);
		} catch (err) {
			throw normalizeError(err);
		}
	},

	// Gets Ids of all Availability in a specific timeslot on a specific day of the week
	async getAvailabilityIdsByDoW(userId: string, dayOfWeek: number, from: string): Promise<number[]> {
		try {
			// Fixes HH:MM:SS format
			const fromTime = from.length === 5 ? `${from}:00` : from;
			
			const response = await fetch(
				`${API_BASE_URL}/Availability/getAvailabilityIdsByDoW?userId=${encodeURIComponent(userId)}&dayOfWeek=${dayOfWeek}&from=${encodeURIComponent(fromTime)}`,
				{
					method: 'GET',
					headers: getHeaders()
				}
			);
			const availabilityIds = await handleResponse<number[]>(response);
			return availabilityIds;
		} catch (err) {
			throw normalizeError(err);
		}
	},

	// Gets Ids of all Availability in a specific timeslot on a specific day of the week
	async getAvailabilityIdByDoW(userId: string, dayOfWeek: number, from: string): Promise<number> {
		try {
			// Fixes HH:MM:SS format
			const fromTime = from.length === 5 ? `${from}:00` : from;
			
			const response = await fetch(
				`${API_BASE_URL}/Availability/getAvailabilityIdByDoW?userId=${encodeURIComponent(userId)}&dayOfWeek=${dayOfWeek}&from=${encodeURIComponent(fromTime)}`,
				{
					method: 'GET',
					headers: getHeaders()
				}
			);
			const availabilityIds = await handleResponse<number>(response);
			return availabilityIds;
		} catch (err) {
			throw normalizeError(err);
		}
	},

	// Update schedules with new availability
	async updateScheduledAvailability(oldAvailabilityIds: number[], newAvailabilityId: number): Promise<void> {
		try {
			const queryParams = new URLSearchParams();
			oldAvailabilityIds.forEach(id => queryParams.append('oldAvailabilityIds', id.toString()));
			queryParams.append('newAvailabilityId', newAvailabilityId.toString());

			const response = await fetch(
				`${API_BASE_URL}/Schedule/updateScheduledAvailability?${queryParams.toString()}`,
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

	// Delete availability by list of AvailabilityIds
	async deleteAvailabilityByIds(availabilityIds: number[]): Promise<void> {
		try {
			const queryParams = new URLSearchParams();
			availabilityIds.forEach(id => queryParams.append('availabilityIds', id.toString()));
			
			const response = await fetch(
				`${API_BASE_URL}/Availability/deleteAvailabilityByIds?${queryParams.toString()}`,
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

	// Gets EventIds of events related to specific availability
	async getScheduledEventIds(availabilityId: number): Promise<number[]> {
		try {
			const response = await fetch(
				`${API_BASE_URL}/Schedule/getScheduledEventIds?availabilityId=${availabilityId}`,
				{
					method: 'GET',
					headers: getHeaders()
				}
			);
			const eventIds = await handleResponse<number[]>(response);
			return eventIds;
		} catch (err) {
			throw normalizeError(err);
		}
	},

};

export type { Availability, NewAvailabilityInput, UserDTO };