import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import type { Event, Availability } from '../types/event'
import type { WorkerUser } from '../types/user'
import { workerService } from '../services/workerService.ts'
import WorkerCalendarGrid from './WorkerCalendarGrid'
import '../styles/EventCalendarPage.css'
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


	// Loads worker's assigned events and availability
	useEffect(() => {
		const load = async () => {
			if (!user?.nameid) return

			try {
				// Get worker's events from the worker's assigned users
				// TODO: flytte userList henting ut av useEffect?
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
		load()
	}, [user, weekStartISO, showError])

	// Week navigation handlers
	const gotoPrevWeek = () => setWeekStartISO(addDaysISO(weekStartISO, -7))
	const gotoNextWeek = () => setWeekStartISO(addDaysISO(weekStartISO, 7))

	return (
		<div className="event-page">
			<main className="event-main">
				<header className="event-header">
					<div className="event-header__left">
						<h1 className="event-title">{user?.name ? `${user.name}'s Event Calendar` : 'Event Calendar'}</h1>
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
							className="logout-btn"
							onClick={() => {
								logout();
								navigate('/worker/login', { replace: true });
							}}
						>
							<img src="/images/logout.png" alt="Logout" />
							<span>Log Out</span>
						</button>
						<button className="add-btn" onClick={() => showError('Unfinished')}>Change Availability</button>
					</div>
				</header>

				{loading && <div className="banner">Loading…</div>}

				<WorkerCalendarGrid
					events={events}
					availability={availability}
					weekStartISO={weekStartISO}
					onEdit={(e) => setViewing(e)}
				/>
			</main>

			{viewing && (
				// Show event details modal when an event is clicked
				<ViewEvent
					event={viewing}
					onClose={() => setViewing(null)}
				/>
			)}
		</div>
	)
}