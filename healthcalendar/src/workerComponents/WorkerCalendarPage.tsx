import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import type { Event, Availability } from '../types/event'
import type { WorkerUser } from '../types/user'
import { sharedService } from '../services/sharedService.ts'
import { workerService } from '../services/workerService.ts'
import WorkerCalendarGrid from './WorkerCalendarGrid'
import '../styles/WorkerCalendar.css'
import '../styles/EventCalendarPage.css'
import { useToast } from '../shared/toastContext'
import { useAuth } from '../auth/AuthContext'
import ViewEvent from './ViewEvent'
import ConfirmationModal from './ConfirmationModal'

// Helper function to convert Date to YYYY-MM-DD format
function toLocalISO(date: Date) {
	const y = date.getFullYear()
	const m = String(date.getMonth() + 1).padStart(2, '0')
	const d = String(date.getDate()).padStart(2, '0')
	return `${y}-${m}-${d}`
}

// Helper function to get the first Monday of the week
function startOfWeekMondayISO(d: Date) {
	const date = new Date(d.getFullYear(), d.getMonth(), d.getDate())
	const day = (date.getDay() + 6) % 7 // 0=Mon
	date.setDate(date.getDate() - day)
	return toLocalISO(date)
}

// Helper function to add days to a date
function addDaysISO(iso: string, days: number) {
	const y = Number(iso.slice(0, 4))
	const m = Number(iso.slice(5, 7)) - 1
	const d = Number(iso.slice(8, 10))
	const date = new Date(y, m, d)
	date.setDate(date.getDate() + days)
	return toLocalISO(date)
}

export default function EventCalendar() {
	const { showError } = useToast()
	const { logout, user } = useAuth()
	const [events, setEvents] = useState<Event[]>([])
	const [availability, setAvailability] = useState<Availability[]>([])
	const [loading] = useState(false)
	const [weekStartISO, setWeekStartISO] = useState(startOfWeekMondayISO(new Date()))
	const [viewing, setViewing] = useState<Event | null>(null)
	const [isAvailabilityMode, setIsAvailabilityMode] = useState(false)
	const [isContinuousMode, setIsContinuousMode] = useState(false)
	const navigate = useNavigate()

	// Helper function to get the week range text
	const weekRangeText = useMemo(() => {
		const startDate = new Date(weekStartISO)
		const endDate = new Date(addDaysISO(weekStartISO, 6))
		const startDay = String(startDate.getDate()).padStart(2, '0')
		const endDay = String(endDate.getDate()).padStart(2, '0')
		const monthFormatter = new Intl.DateTimeFormat('en-GB', { month: 'long' })
		const month = monthFormatter.format(startDate)
		const year = startDate.getFullYear()
		return `${startDay} – ${endDay} ${month} ${year}`
	}, [weekStartISO])


	const loadData = async () => {
		if (!user?.nameid) return // User login check

		try {
			// Get worker's events from the worker's assigned users
			const userList = await workerService.getUsersByWorkerId(user.nameid)
			const eventsData = await workerService.getWeeksEventsForWorker(userList, weekStartISO)
			setEvents(eventsData)

			// Get worker's availability (all records including overlaps)
			const workerId = (user as WorkerUser).nameid
			const allAvailability = await workerService.getAllWeeksAvailability(workerId, weekStartISO)

			// Process availability to handle cancellations
			// Db logic: if date availability overlaps with continuous availability, counts as cancelled.

			const processedAvailability: Availability[] = []

			// Filters continuous vs specific availability
			const continuous = allAvailability.filter(a => !a.date) // date = null
			const specific = allAvailability.filter(a => a.date) // date != null

			// Check continuous availabilities (overlapping = unavailable)
			continuous.forEach(c => {
				// Checks if continuous availability is cancelled by any specific availability
				const isCancelled = specific.some(s =>
					s.day === c.day &&
					s.startTime === c.startTime &&
					s.endTime === c.endTime
				)

				if (!isCancelled) {
					processedAvailability.push(c)
				}
			})

			// Check specific availabilities (overlapping = unavailable)
			specific.forEach(s => {
				// Checks if specific availability is cancelling a continuous one
				const isCancelling = continuous.some(c =>
					c.day === s.day &&
					c.startTime === s.startTime &&
					c.endTime === s.endTime
				)

				// If NOT cancelling a continuous one, it's a specific availability
				if (!isCancelling) {
					processedAvailability.push(s)
				}
			})

			setAvailability(processedAvailability)
		} catch (e) {
			const message = e instanceof Error ? e.message : 'Failed to load calendar data.'
			showError(message)
		}
	}

	// Loads worker's assigned events and availability
	useEffect(() => {
		loadData()
	}, [user, weekStartISO, showError])

	// Week navigation handlers
	const gotoPrevWeek = () => setWeekStartISO(addDaysISO(weekStartISO, -7))
	const gotoNextWeek = () => setWeekStartISO(addDaysISO(weekStartISO, 7))

	const handleAvailabilityToggle = () => {
		setIsAvailabilityMode(!isAvailabilityMode)
		// Reset continuous mode when exiting availability mode
		if (isAvailabilityMode) setIsContinuousMode(false)
	}

	const handleContinuousToggle = () => {
		setIsContinuousMode(!isContinuousMode)
	}

	// Confirmation modal state
	const [showConfirmModal, setShowConfirmModal] = useState(false)
	const [pendingDeletion, setPendingDeletion] = useState<{
		availabilityId?: number,
		eventIds: number[],
		action: 'deleteSpecific' | 'mask' | 'deleteContinuous',
		workerId?: string,
		date?: string,
		startTime?: string,
		endTime?: string,
		dayOfWeek?: number
	} | null>(null)

	// Time slot click handler
	const handleSlotClick = async (date: string, time: number, dayName: string) => {
		if (!user?.nameid) return

		try {
			// Gets relevant parameters
			const [workerId, dayOfWeek, timeStr, endTimeStr] = await getAndValidateParameters(date, time, dayName)

			// New data to ensure latest state
			const allAvailability = await workerService.getAllWeeksAvailability(workerId, weekStartISO)

			const continuous = allAvailability.filter(a => !a.date)
			const specific = allAvailability.filter(a => a.date)

			// Check for existing time slots matching this slot
			const matchingContinuous = continuous.find(a =>
				a.day === dayName &&
				// Time string in HH:MM format
				Number(a.startTime.split(':')[0]) * 60 + Number(a.startTime.split(':')[1]) === time
			)

			// Check for existing time slots matching this slot
			const matchingSpecific = specific.find(a =>
				a.date === date &&
				// Time string in HH:MM format
				Number(a.startTime.split(':')[0]) * 60 + Number(a.startTime.split(':')[1]) === time
			)

			// Logic for Continuous mode 
			if (isContinuousMode) {
				if (matchingContinuous) {
					// Delete continuous availability
					await deleteContinuous(matchingContinuous.id, workerId, dayOfWeek, timeStr)		
				} else {
					// Create continuous availability
					await createContinuous(workerId, dayOfWeek, timeStr, endTimeStr)
				}
			}
			// Specific date
			// If slot is currently displayed as available (continuous): 
			// create specific availability to mask it (make unavailable)
			else if (matchingContinuous && !matchingSpecific) {
				await createMask(matchingContinuous.id, workerId, dayOfWeek, date, timeStr, endTimeStr)
			}
			// If slot is currently displayed as available (specific): 
			// delete the specific record
			else if (!matchingContinuous && matchingSpecific) {
				await deleteSingular(matchingSpecific.id, date)
			}
			// If unavailable (cancelled - continuous + specific overlap): 
			// delete specific record to unmask the continuous one (make available)
			else if (matchingContinuous && matchingSpecific) {
				await deleteMasked(matchingSpecific.id)
			}
			// If slot is unavailable (empty): 
			// create specific record (make available)
			else {
				await createSingular(workerId, dayOfWeek, date, timeStr, endTimeStr)
			}
		} catch (e) {
			const message = e instanceof Error ? e.message : 'Failed to update availability.'
			showError(message)
		}
	}

	// calculates Parameters for create or delete operation
	const getAndValidateParameters = async (date: string, time: number, dayName: string): 
									   Promise<[string, number, string, string]> => {
		
		// Retreives Worker's Id
		const workerId = (user as WorkerUser).nameid
		
		// Convert dayName to dayOfWeek number
		const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']
		const dayOfWeek = days.indexOf(dayName)

		// Calculates hours and minutes
		const h = Math.floor(time / 60)
		const m = time % 60
		// Time string in HH:MM format
		const timeStr = `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`

		// Calculate end time (30 mins later)
		let endH = h
		let endM = m + 30
		if (endM >= 60) {
			endH++
			endM -= 60
		}
		// End time string in HH:MM format
		const endTimeStr = `${String(endH).padStart(2, '0')}:${String(endM).padStart(2, '0')}`

		// Validation check
		const currentDate = toLocalISO(new Date)
		if (dayOfWeek === -1 || time % 30 !== 0 || date < currentDate) {
			throw new Error('Request not acceptable.');
		}

		return [workerId, dayOfWeek, timeStr, endTimeStr]
	}

	// Workflow for properly deleting continuous availability
	const deleteContinuous = async (avId: number, workerId: string, dayOfWeek: number, timeStr: string) => {

		// Check for any linked events across all dates
		const eventIds = await workerService.getScheduledEventIds(avId)
		if (eventIds.length > 0) {
			// Warn user if events are linked
			setPendingDeletion({
				eventIds: eventIds,
				workerId: workerId,
				dayOfWeek: dayOfWeek,
				startTime: timeStr,
				action: 'deleteContinuous'
			})
			setShowConfirmModal(true)
			return
		}

		// No linked events: Delete continuous availability
		await workerService.deleteAvailabilityByDoW(workerId, dayOfWeek, timeStr);
		await loadData()
	}

	// Workflow for properly creating masked specific availability
	const createMask = async (avId: number, workerId: string, dayOfWeek: number, 
							  date: string, timeStr: string, endTimeStr: string) => {
		// Check for linked events, if any, ask worker for confirmation
		const eventId = await workerService.findScheduledEventId(avId, date)
		if (eventId) {
			// Delete the event + create a specific availability to mask the continuous one
			setPendingDeletion({
				availabilityId: avId,
				eventIds: [eventId],
				action: 'mask',
				date: date,
				startTime: timeStr,
				endTime: endTimeStr,
				dayOfWeek: dayOfWeek
			})
			setShowConfirmModal(true)
			return
		}

		// Create specific availability to mask the continuous one
		await workerService.createAvailability({
			startTime: timeStr,
			endTime: endTimeStr,
			dayOfWeek,
			date: date,
		}, workerId)
		await loadData()
	}

	// Workflow for properly deleting specific availability
	const deleteSingular = async (avId: number, date: string) => {
		// Check for linked events, if any, ask worker for confirmation
		const eventId = await workerService.findScheduledEventId(avId, date)
		if (eventId) {
			setPendingDeletion({
				availabilityId: avId,
				eventIds: [eventId],
				action: 'deleteSpecific'
			})
			setShowConfirmModal(true)
			return
		}

		// Delete the specific record
		await workerService.deleteAvailability(avId)
		await loadData()
	}

	// Workflow for properly creating continuous availability
	const createContinuous = async (workerId: string, dayOfWeek: number, timeStr: string, endTimeStr: string) => {
		// Gets list of availability for relevant slot
		const oldAvailabilityIds = await workerService.getAvailabilityIdsByDoW(workerId, dayOfWeek, timeStr)
					
		// Create continuous availability (date: null)
		await workerService.createAvailability({
			startTime: timeStr,
			endTime: endTimeStr,
			dayOfWeek,
			date: null, // Continuous
		}, workerId)

		// If availability exists in relevant slot, they are to be be removed
		if (oldAvailabilityIds.length > 0) {
			const newAvailabilityId = await workerService.getAvailabilityIdByDoW(workerId, dayOfWeek, timeStr)
			await workerService.updateScheduledAvailability(oldAvailabilityIds, newAvailabilityId)
			await workerService.deleteAvailabilityByIds(oldAvailabilityIds)
		}
		await loadData()
	}

	// Workflow for deleting masked specific availability
	const deleteMasked = async (avId: number) => {
		await workerService.deleteAvailability(avId)
		await loadData()
	}

	// Workflow for creating specific availability
	const createSingular = async (workerId: string, dayOfWeek: number, date: string, 
								  timeStr: string, endTimeStr: string) => {
		await workerService.createAvailability({
			startTime: timeStr,
			endTime: endTimeStr,
			dayOfWeek,
			date: date,
		}, workerId)
		await loadData()
	}

	// Confirmation model handlers
	const handleConfirmDeletion = async () => {
		if (!pendingDeletion) return

		try {
			await sharedService.deleteSchedulesByEventIds(pendingDeletion.eventIds);
			await sharedService.deleteEventsByIds(pendingDeletion.eventIds)

			// If deleting singular availability
			if (pendingDeletion.action === 'deleteSpecific') {
				await workerService.deleteAvailability(pendingDeletion.availabilityId!)
			}
			// If masking, create masking availability
			else if (pendingDeletion.action === 'mask') {
				await workerService.createAvailability({
					startTime: pendingDeletion.startTime!,
					endTime: pendingDeletion.endTime!,
					dayOfWeek: pendingDeletion.dayOfWeek!,
					date: pendingDeletion.date,
				}, user!.nameid)
			}
			// If deleting continuous availability, delete all availability in relevant slot
			else {
				await workerService.deleteAvailabilityByDoW(pendingDeletion.workerId!, pendingDeletion.dayOfWeek!, pendingDeletion.startTime!);
			}

			// Reset state
			setShowConfirmModal(false)
			setPendingDeletion(null)
			await loadData()
		} catch (e) {
			const message = e instanceof Error ? e.message : 'Failed to delete event and availability.'
			showError(message)
		}
	}

	return (
		<div className="event-page">
			<main className="event-main">
				<header className="event-header">
					<div className="event-header__left">
						<h1 className="event-title">{user?.name ? `${user.name}'s Work Calendar` : 'Event Calendar'}</h1>
						<div className="event-week">
							<button className="icon-btn" onClick={gotoPrevWeek} aria-label="Previous week">
								<img src="/images/backarrow.png" alt="Previous week" />
							</button>
							<span className="event-week__range">{weekRangeText}</span>
							<button className="icon-btn" onClick={gotoNextWeek} aria-label="Next week">
								<img src="/images/forwardarrow.png" alt="Next week" />
							</button>
						</div>
					</div>
					<div className="event-header__right">
						<button
							className="btn-logout"
							onClick={() => {
								logout();
								navigate('/worker/login', { replace: true });
							}}
						>
							<img src="/images/logout.png" alt="Logout" />
							<span>Log Out</span>
						</button>

						<div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', justifyContent: 'flex-end' }}>
							{isAvailabilityMode && (
								<>
									<button
										className={isContinuousMode ? 'btn-static-g' : 'btn-toggle'}
										onClick={handleContinuousToggle}
										title="Toggle between continuous and specific date availability"
									>
										{isContinuousMode ? 'Weekly' : 'Daily'}
									</button>
								</>
							)}
							<button
								className={isAvailabilityMode ? 'btn-static-g' : 'btn-static'}
								onClick={handleAvailabilityToggle}
							>
								{isAvailabilityMode ? 'Done' : 'Change Availability'}
							</button>
						</div>

					</div>
				</header>

				{loading && <div className="banner">Loading…</div>}

				<WorkerCalendarGrid
					// Main calendar grid with worker's patient's events and worker's availability
					events={events}
					availability={availability}
					weekStartISO={weekStartISO}
					endHour={20}
					isAvailabilityMode={isAvailabilityMode}
					onEdit={(e) => setViewing(e)}
					onSlotClick={isAvailabilityMode ? handleSlotClick : undefined}
				/>
			</main>

			{viewing && (
				// Show event details modal when an event is clicked
				<ViewEvent
					event={viewing}
					onClose={() => setViewing(null)}
				/>
			)}

			{isAvailabilityMode && (
				// Notification alert in bottom right for notifying worker on how to change availability
				<div className="availability-notification">
					Press timeboxes to change your availability
				</div>
			)}
			<ConfirmationModal
				// Modal for confirming when a removed availability will delete an event
				isOpen={showConfirmModal}
				title="Are you sure?"
				message="There are one or more events scheduled during this time. Removing your availability will delete the connected event."
				onConfirm={handleConfirmDeletion}
				onCancel={() => {
					setShowConfirmModal(false)
					setPendingDeletion(null)
				}}
			/>
		</div>
	)
}