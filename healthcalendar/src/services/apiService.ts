// Mock API service layer for HealthCalendar MVP
// Provides async CRUD operations for Events and Availability with simulated latency
// and occasional random failures to exercise error handling paths.
// When backend endpoints become available, swap implementations but keep signatures.

import type { Event, Availability, NewEventInput, UpdateEventInput } from '../types/event';

// In-memory mock data stores
let events: Event[] = [
	{
		eventId: 1,
		title: 'Medication Reminder',
		location: 'Home',
		date: '2025-11-08',
		startTime: '09:00',
		endTime: '09:30',
		patientName: 'Alice'
	},
	{
		eventId: 2,
		title: 'Grocery Assistance',
		location: 'City Market',
		date: '2025-11-09',
		startTime: '14:00',
		endTime: '15:30',
		patientName: 'Alice'
	}
];

let availability: Availability[] = [
	{ id: 1, day: 'Monday', startTime: '08:00', endTime: '16:00' },
	{ id: 2, day: 'Tuesday', startTime: '10:00', endTime: '18:00' },
	{ id: 3, day: 'Wednesday', startTime: '08:00', endTime: '12:00' },
	{ id: 4, day: 'Thursday', startTime: '12:00', endTime: '20:00' },
	{ id: 5, day: 'Friday', startTime: '08:00', endTime: '16:00' }
];

// Configuration for simulated network
const MIN_DELAY_MS = 200;
const MAX_DELAY_MS = 600;
const RANDOM_ERROR_PROBABILITY = 0.08; // 8% chance to throw

function delay(): Promise<void> {
	const ms = Math.random() * (MAX_DELAY_MS - MIN_DELAY_MS) + MIN_DELAY_MS;
	return new Promise(res => setTimeout(res, ms));
}

function maybeThrowRandom(endpoint: string): void {
	if (Math.random() < RANDOM_ERROR_PROBABILITY) {
		throw new Error(`Temporary server issue on ${endpoint}. Please retry.`);
	}
}

// Utility to generate a new incremental ID
function nextEventId(): number {
	return events.length ? Math.max(...events.map(e => e.eventId)) + 1 : 1;
}

// Public API surface
export const apiService = {
	async getEvents(): Promise<Event[]> {
		try {
			await delay();
			maybeThrowRandom('getEvents');
			// Return shallow copy to prevent external mutation
			return [...events];
		} catch (err) {
			throw normalizeError(err);
		}
	},

	async createEvent(input: NewEventInput): Promise<Event> {
		try {
			await delay();
			maybeThrowRandom('createEvent');
			validateEventTimes(input.startTime, input.endTime);
			const newEvent: Event = { eventId: nextEventId(), ...input };
			events.push(newEvent);
			return newEvent;
		} catch (err) {
			throw normalizeError(err);
		}
	},

	async updateEvent(update: UpdateEventInput): Promise<Event> {
		try {
			await delay();
			maybeThrowRandom('updateEvent');
			validateEventTimes(update.startTime, update.endTime);
			const index = events.findIndex(e => e.eventId === update.eventId);
			if (index === -1) throw new Error('Event not found');
			events[index] = { ...update };
			return events[index];
		} catch (err) {
			throw normalizeError(err);
		}
	},

	async deleteEvent(eventId: number): Promise<void> {
		try {
			await delay();
			maybeThrowRandom('deleteEvent');
			events = events.filter(e => e.eventId !== eventId);
		} catch (err) {
			throw normalizeError(err);
		}
	},

	async getAvailability(): Promise<Availability[]> {
		try {
			await delay();
			maybeThrowRandom('getAvailability');
			return [...availability];
		} catch (err) {
			throw normalizeError(err);
		}
	},

	async updateAvailability(list: Availability[]): Promise<void> {
		try {
			await delay();
			maybeThrowRandom('updateAvailability');
			// Basic validation: end must be after start
			list.forEach(a => {
				if (!isTimeBefore(a.startTime, a.endTime)) {
					throw new Error(`Invalid availability window for ${a.day}: ${a.startTime} - ${a.endTime}`);
				}
			});
			availability = [...list];
		} catch (err) {
			throw normalizeError(err);
		}
	}
};

// ----- Validation Helpers -----
function isTimeBefore(start: string, end: string): boolean {
	return toMinutes(start) < toMinutes(end);
}

function toMinutes(t: string): number {
	const [h, m] = t.split(':').map(Number);
	return h * 60 + m;
}

function validateEventTimes(start: string, end: string) {
	if (!isTimeBefore(start, end)) {
		throw new Error(`Event time range invalid: ${start} - ${end}`);
	}
}

// Normalize unknown error types
function normalizeError(err: unknown): Error {
	if (err instanceof Error) return err;
	return new Error('Unknown error occurred');
}

export type { Event, Availability };
