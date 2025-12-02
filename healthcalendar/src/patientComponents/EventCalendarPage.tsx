import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import type { Event, Availability } from '../types/event'
import type { PatientUser } from '../types/user'
import { sharedService } from '../services/sharedService'
import { patientService } from '../services/patientService'
import CalendarGrid from '../components/CalendarGrid'
import '../styles/EventCalendarPage.css'
import { useToast } from '../shared/toastContext'
import NewEventForm from './NewEventForm'
import EditEventForm from './EditEventForm'
import { useAuth } from '../auth/AuthContext'

// Patient event calendar page - displays weekly calendar with patient's events and worker availability

// Convert Date object to YYYY-MM-DD ISO string in local timezone
function toLocalISO(date: Date) {
  const y = date.getFullYear()
  const m = String(date.getMonth() + 1).padStart(2, '0')
  const d = String(date.getDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}

// Get Monday of the week for a given date as ISO string
function startOfWeekMondayISO(d: Date) {
  const date = new Date(d.getFullYear(), d.getMonth(), d.getDate())
  const day = (date.getDay() + 6) % 7 // Convert to Monday=0 format
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

// Convert ISO date string to day name
function convertISOtoDay(iso: string) {
  const dayOfWeekMap: { [key: number]: string } = {
		0: 'Sunday',
		1: 'Monday',
		2: 'Tuesday',
		3: 'Wednesday',
		4: 'Thursday',
		5: 'Friday',
		6: 'Saturday'
	};
  const y = Number(iso.slice(0, 4))
  const m = Number(iso.slice(5, 7)) - 1
  const d = Number(iso.slice(8, 10))
  const dayOfWeek = new Date(y, m, d).getDay()
  return dayOfWeekMap[dayOfWeek]
}

export default function EventCalendarPage() {
  // Toast notifications and authentication context
  const { showSuccess, showError } = useToast()
  const { logout, user } = useAuth()
  
  // Calendar data state
  const [events, setEvents] = useState<Event[]>([])                      // Patient's events
  const [availability, setAvailability] = useState<Availability[]>([])   // Worker's available slots
  const [loading, setLoading] = useState(false)
  const [weekStartISO, setWeekStartISO] = useState(startOfWeekMondayISO(new Date()))
  
  // UI state for modals and forms
  const [showNew, setShowNew] = useState(false)
  const [editing, setEditing] = useState<Event | null>(null) 
  const [showLogoutConfirm, setShowLogoutConfirm] = useState(false)
  
  // Form-specific error messages
  const [newFormError, setNewFormError] = useState<string | null>(null)
  const [editFormError, setEditFormError] = useState<string | null>(null)
  
  const navigate = useNavigate()

  // Format week range text for display
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

  const availableDays = useMemo(() => (
    Array.from({ length: 7 }, (_, i) => addDaysISO(weekStartISO, i))
  ), [weekStartISO])

  // Load patient's events and worker's available time slots for the current week
  // Availability is filtered to exclude slots already booked by other patients
  useEffect(() => {
    const load = async () => {
      if (!user?.nameid) return
      
      try {
        // Step 1: Fetch this patient's events for the week
        const patientsEventsData = await patientService.getWeeksEventsByUserId(user.nameid, weekStartISO)
        setEvents(patientsEventsData)
        
        // Step 2: Fetch worker's availability for the week
        const workerId = (user as PatientUser).WorkerId
        let availabilityData = await patientService.getWeeksAvailabilityProper(workerId, weekStartISO)
        
        // Step 3: Get all patient IDs assigned to this worker
        const userIds = await sharedService.getIdsByWorkerId(workerId)
        const othersUserIds = userIds.filter(id => id !== user.nameid)
        
        // Step 4: Fetch events of other patients assigned to the same worker
        const othersEventsData = await patientService.getWeeksEventsByUserIds(othersUserIds, weekStartISO)
        
        // Step 5: Filter availability to exclude time slots occupied by other patients
        // Only show slots that are available for this patient to book
        othersEventsData.forEach(event => {
          const eventDay = convertISOtoDay(event.date)
          availabilityData = availabilityData.filter(av => 
                                  av.day !== eventDay ||
                                  av.endTime <= event.startTime ||
                                  av.startTime >= event.endTime)
        })
        setAvailability(availabilityData)
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Failed to load calendar data'
        showError(message)
      }
    }
    load()
  }, [user, weekStartISO, showError])

  // Handle creating a new event
  const onSaveNew = async (e: Omit<Event, 'eventId'>) => {
    if (!user?.nameid) {
      showError('User not authenticated')
      return
    }

    try {
      // Step 1: Get all patient IDs assigned to this worker for validation
      const workerId = (user as PatientUser).WorkerId
      const userIds = await sharedService.getIdsByWorkerId(workerId)

      // Step 2: Validate event doesn't overlap with other patients' events
      await patientService.validateEventForCreate(e, user.nameid, userIds)
      
      // Step 3: Check worker availability and get matching availability IDs
      // Throws error if worker is not available during requested time
      const availabilityIds = await patientService.checkAvailabilityForCreate(e, workerId)
      
      // Step 4: Create the event in the database
      const created = await patientService.createEvent(e, user.nameid)
      
      // Step 5: Create schedules to link event with availability blocks
      if (availabilityIds.length > 0) {
        await patientService.createSchedules(created.eventId, e.date, availabilityIds)
      }
      
      // Step 6: Update local state with the new event for immediate UI feedback
      setEvents(prev => [...prev, created])
      
      // Step 7: Close form and show success message
      setShowNew(false)
      setNewFormError(null)
      showSuccess('Event created successfully')
    } catch (err) {
      // Handle specific error cases
      if (err instanceof Error) {
        if (err.message.includes('Not Acceptable') || err.message.includes('not acceptable')) {
          setEditFormError('This time slot is not available. Please choose a different time.')
        } else {
          showError(err.message)
        }
      } else {
        showError('Failed to create event')
      }
    }
  }

  // Handle updating an existing event
  const onSaveEdit = async (updatedEvent: Event, originalEvent: Event) => {
    if (!user?.nameid) {
      showError('User not authenticated')
      return
    }

    try {
      // Step 1: Get all patient IDs for validation
      const workerId = (user as PatientUser).WorkerId
      const userIds = await sharedService.getIdsByWorkerId(workerId)
      
      // Step 2: Validate updated event doesn't overlap with other patients
      await patientService.validateEventForUpdate(updatedEvent, user.nameid, userIds)
      
      // Step 3: Compare original and updated event to determine schedule changes needed
      // Returns lists of availability IDs for create, delete, and update operations
      const availabilityLists = await patientService.checkAvailabilityForUpdate(
        updatedEvent,
        originalEvent.date,
        originalEvent.startTime,
        originalEvent.endTime,
        workerId
      )
      
      // Step 4: Update the event in the database
      await patientService.updateEvent(updatedEvent, user.nameid)
      
      // Step 5: Create new schedules for newly covered availability blocks
      if (availabilityLists.forCreateSchedules.length > 0) {
        await patientService.createSchedules(
          updatedEvent.eventId,
          updatedEvent.date,
          availabilityLists.forCreateSchedules
        )
      }
      
      // Step 6: Delete schedules for availability blocks no longer covered
      if (availabilityLists.forDeleteSchedules.length > 0) {
        await patientService.deleteSchedulesAfterEventUpdate(
          updatedEvent.eventId,
          availabilityLists.forDeleteSchedules
        )
      }
      
      // Step 7: Update existing schedules with new event details
      if (availabilityLists.forUpdateSchedules.length > 0) {
        await patientService.updateScheduledEvent(
          updatedEvent.eventId,
          availabilityLists.forUpdateSchedules
        )
      }
      
      // Step 8: Update local state for immediate UI feedback
      setEvents(prev => prev.map(evt => 
        evt.eventId === updatedEvent.eventId ? updatedEvent : evt
      ))
      
      setEditing(null)
      setEditFormError(null)
      showSuccess('Event created successfully')
    } catch (err) {
      // Handle specific error cases
      if (err instanceof Error) {
        // 406 Not Acceptable - time slot unavailable or conflicts with other patient
        if (err.message.includes('Not Acceptable') || err.message.includes('not acceptable')) {
          setEditFormError('This time slot is not available. Please choose a different time.')
        } else {
          showError(err.message)
        }
      } else {
        showError('Failed to create event')
      }
    }
  }

  // Handle deleting an event
  const onDelete = async (id: number) => {
    if (!user?.nameid) {
      showError('User not authenticated')
      return
    }

    try {
      // Step 1: Delete all schedules linked to this event
      await patientService.deleteSchedulesByEventId(id)
      
      // Step 2: Delete the event from the database
      await sharedService.deleteEvent(id)
      
      // Step 3: Remove event from local state for immediate UI feedback
      setEvents(prev => prev.filter(evt => evt.eventId !== id))
      
      setEditing(null)
      showSuccess('Event deleted successfully')
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to delete event'
      showError(message)
      throw err
    }
  }

  // Navigate to previous and next week
  const gotoPrevWeek = () => setWeekStartISO(addDaysISO(weekStartISO, -7))
  const gotoNextWeek = () => setWeekStartISO(addDaysISO(weekStartISO, 7))

  return (
    <div className="event-page">
      <main className="event-main">
        <header className="event-header">
          <div className="event-header__left">
            <h1 className="event-title">{user?.name ? `${user.name}’s Event Calendar` : 'Event Calendar'}</h1>
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
              onClick={() => setShowLogoutConfirm(true)}
            >
              <img src="/images/logout.png" alt="Logout" />
              <span>Log Out</span>
            </button>
            <button className="add-btn" onClick={() => setShowNew(true)}>+ Add New Event</button>
          </div>
        </header>

        {loading && <div className="banner">Loading…</div>}

        {/* Main calendar grid showing events and worker availability */}
        <CalendarGrid
          events={events}
          availability={availability}
          weekStartISO={weekStartISO}
          onEdit={(e) => setEditing(e)}
        />
      </main>

      {/* New event form modal */}
      {showNew && (
        <NewEventForm
          availableDays={availableDays}
          availability={availability}
          onClose={() => {
            setShowNew(false)
            setNewFormError(null)
          }}
          onSave={onSaveNew}
          error={newFormError}
          onClearError={() => setNewFormError(null)}
        />
      )}

      {/* Edit event form modal */}
      {editing && (
          <EditEventForm
            event={editing}
            availableDays={availableDays}
            availability={availability}
            onClose={() => {
              setEditing(null)
              setEditFormError(null)
            }}
            onSave={(updatedEvent) => onSaveEdit(updatedEvent, editing)}
            onDelete={onDelete}
            error={editFormError}
            onClearError={() => setEditFormError(null)}
          />
      )}

      {/* Logout confirmation modal */}
      {showLogoutConfirm && (
        <div className="overlay" role="dialog" aria-modal="true" aria-labelledby="logout-confirm-title" aria-describedby="logout-confirm-desc">
          <div className="modal confirm-modal">
            <header className="modal__header">
              <h2 id="logout-confirm-title">Confirm Logout</h2>
              <button className="icon-btn" onClick={() => setShowLogoutConfirm(false)} aria-label="Close confirmation">
                <img src="/images/exit.png" alt="Close" />
              </button>
            </header>
            <div id="logout-confirm-desc" className="confirm-body">
              Are you sure you want to log out?
            </div>
            <div className="confirm-actions">
              <button type="button" className="btn" onClick={() => setShowLogoutConfirm(false)}>Cancel</button>
              <button 
                type="button" 
                className="btn btn--primary" 
                onClick={() => {
                  logout();
                  window.location.href = '/';
                }}
              >
                Confirm
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}