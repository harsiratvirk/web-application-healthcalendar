import type { Availability } from '../types/event';
// Imports functions shared with other services
import { API_BASE_URL, getHeaders, handleResponse } from './sharedService.ts'

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

// Backend DTO to JS format
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
		day: dayOfWeekMap[dto.DayOfWeek ?? dto.dayOfWeek] || 'Monday',
		startTime: (dto.From || dto.from).substring(0, 5), // "HH:MM:SS" -> "HH:MM"
		endTime: (dto.To || dto.to).substring(0, 5),
		date: dto.Date || dto.date || undefined
	};
}

// UserDTO interface matching backend structure
interface UserDTO {
	Id: string;
	UserName: string;
	Name: string;
	Role: string;
	WorkerId?: string;
}

export const workerService = {
	// Get patients assigned to a worker
	async getUsersByWorkerId(workerId: string): Promise<UserDTO[]> {
		try {
			const response = await fetch(
				`${API_BASE_URL}/User/getUsersByWorkerId?workerId=${encodeURIComponent(workerId)}`,
				{
					method: 'GET',
					headers: getHeaders()
				}
			);
			const users = await handleResponse<UserDTO[]>(response);
			return users;
		} catch (e) {
			throw e as Error;
		}
	},

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
		} catch (e) {
			throw e as Error;
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
		} catch (e) {
			throw e as Error;
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
		} catch (e) {
			throw e as Error;
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
		} catch (e) {
			throw e as Error;
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
		} catch (e) {
			throw e as Error;
		}
	},

	// Delete availability by day of week
	async deleteAvailabilityByDoW(dayOfWeek: number, from: string): Promise<void> {
		try {
			// Fixes HH:MM:SS format
			const fromTime = from.length === 5 ? `${from}:00` : from;
			
			const response = await fetch(
				`${API_BASE_URL}/Availability/deleteAvailabilityByDoW?dayOfWeek=${dayOfWeek}&from=${encodeURIComponent(fromTime)}`,
				{
					method: 'DELETE',
					headers: getHeaders()
				}
			);
			await handleResponse<any>(response);
		} catch (e) {
			throw e as Error;
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
		} catch (e) {
			throw e as Error;
		}
	},

	// Delete event by ID
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
		} catch (e) {
			throw e as Error;
		}
	}
};

export type { Availability, NewAvailabilityInput, UserDTO };