import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import type { Event, Availability } from '../types/event'
import type { WorkerUser } from '../types/user'
import { workerService } from '../services/workerService.ts'
import WorkerCalendarGrid from './WorkerCalendarGrid'
import '../styles/WorkerCalendar.css'
import { useToast } from '../shared/toastContext'
import { useAuth } from '../auth/AuthContext'
import ViewEvent from './ViewEvent'

function toLocalISO(date: Date) {
	const y = date.getFullYear()
	const m = String(date.getMonth() + 1).padStart(2, '0')
	const d = String(date.getDate()).padStart(2, '0')
	return `${y}-${m}-${d}`
}

function startOfWeekMondayISO(d: Date) {
	const date = new Date(d.getFullYear(), d.getMonth(), d.getDate())
	const day = (date.getDay() + 6) % 7 // 0=Mon
	date.setDate(date.getDate() - day)
	return toLocalISO(date)
}

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
	const [showNew, setShowNew] = useState(false)
	const [events, setEvents] = useState<Event[]>([])
	const [availability, setAvailability] = useState<Availability[]>([])
	const [loading, setLoading] = useState(false)
	const [weekStartISO, setWeekStartISO] = useState(startOfWeekMondayISO(new Date()))
	const [viewing, setViewing] = useState<Event | null>(null)
	const [isAvailabilityMode, setIsAvailabilityMode] = useState(false)
	const navigate = useNavigate()

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
		if (!user?.nameid) return

		try {
			// Get worker's events from the worker's assigned users
			const userList = await workerService.getUsersByWorkerId(user.nameid)
			const eventsData = await workerService.getWeeksEventsForWorker(userList, weekStartISO)
			setEvents(eventsData)

			// Get worker's availability
			const workerId = (user as WorkerUser).nameid
			const availabilityData = await workerService.getWeeksAvailabilityProper(workerId, weekStartISO)
			setAvailability(availabilityData)
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
	}

	const handleSlotClick = async (date: string, time: number, dayName: string) => {
		if (!user?.nameid) return

		const h = Math.floor(time / 60)
		const m = time % 60
		const timeStr = `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`

		// Check if this slot is covered by an existing availability
		const found = availability.find(a => {
			if (a.day !== dayName) return false
			const startMins = Number(a.startTime.split(':')[0]) * 60 + Number(a.startTime.split(':')[1])
			const endMins = Number(a.endTime.split(':')[0]) * 60 + Number(a.endTime.split(':')[1])
			return time >= startMins && time < endMins
		})

		try {
			if (found) {
				// Remove availability
				await workerService.deleteAvailability(found.id)
			} else {
				// Add availability (30 min slot)
				// Convert dayName to dayOfWeek number (0=Sunday, 1=Monday)
				const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']
				const dayOfWeek = days.indexOf(dayName)

				if (dayOfWeek === -1) {
					console.error('Invalid day name:', dayName)
					return
				}

				// Calculate end time (30 mins later)
				let endH = h
				let endM = m + 30
				if (endM >= 60) {
					endH++
					endM -= 60
				}
				const endTimeStr = `${String(endH).padStart(2, '0')}:${String(endM).padStart(2, '0')}`

				await workerService.createAvailability({
					startTime: timeStr,
					endTime: endTimeStr,
					dayOfWeek,
					date: date, // date
				}, user.nameid)
			}
			// Refresh data
			await loadData()
		} catch (e) {
			const message = e instanceof Error ? e.message : 'Failed to update availability.'
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
					events={events}
					availability={availability}
					weekStartISO={weekStartISO}
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
				<div className="availability-notification">
					Press timeboxes to change your availability
				</div>
			)}
		</div>
	)
}