import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import type { Event, Availability } from '../types/event'
import type { PatientUser } from '../types/user'
import { apiService } from '../services/eventService'
import CalendarGrid from '../components/CalendarGrid'
import '../styles/EventCalendar.css'
import { useToast } from '../shared/toastContext'
import NewEventForm from './NewEventForm'
import EditEventForm from './EditEventForm'
import { useAuth } from '../auth/AuthContext'

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
  const { showSuccess, showError } = useToast()
  const { logout, user } = useAuth()
  const [events, setEvents] = useState<Event[]>([])
  const [availability, setAvailability] = useState<Availability[]>([])
  const [loading, setLoading] = useState(false)
  const [weekStartISO, setWeekStartISO] = useState(startOfWeekMondayISO(new Date()))
  const [showNew, setShowNew] = useState(false)
  const [editing, setEditing] = useState<Event | null>(null)
  const [showLogoutConfirm, setShowLogoutConfirm] = useState(false)
  const [newFormError, setNewFormError] = useState<string | null>(null)
  const [editFormError, setEditFormError] = useState<string | null>(null)
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

  const availableDays = useMemo(() => (
    Array.from({ length: 7 }, (_, i) => addDaysISO(weekStartISO, i))
  ), [weekStartISO])

  // Load patient's events and worker's availability for the current week
  useEffect(() => {
    const load = async () => {
      if (!user?.nameid) return
      
      try {
        // Step 1: Call getWeeksEventsForPatient() to retrieve patient's events
        const eventsData = await apiService.getWeeksEventsForPatient(user.nameid, weekStartISO)
        setEvents(eventsData)
        // Step 2: Call getWeeksAvailabilityProper() to retrieve worker's availability
        // Get workerId from patient's JWT token (WorkerId field)
        const workerId = (user as PatientUser).WorkerId
        const availabilityData = await apiService.getWeeksAvailabilityProper(workerId, weekStartISO)
        setAvailability(availabilityData)
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Failed to load calendar data'
        showError(message)
      }
    }
    load()
  }, [user, weekStartISO, showError])

  // Complete create event workflow as per specification
  const onSaveNew = async (e: Omit<Event, 'eventId'>) => {
    if (!user?.nameid) {
      showError('User not authenticated')
      return
    }

    try {
      // Step 1: Call validateEventForCreate()
      await apiService.validateEventForCreate(e, user.nameid)
      
      // Step 2: Check if worker has availability for the requested time
      // Events can only be created when worker is available
      const workerId = (user as PatientUser).WorkerId
      const availabilityIds = await apiService.checkAvailabilityForCreate(e, workerId)
      
      // Step 3: Call createEvent()
      const created = await apiService.createEvent(e, user.nameid)
      
      // Step 4: Call createSchedules() with eventId and availabilityIds (if any)
      if (availabilityIds.length > 0) {
        await apiService.createSchedules(created.eventId, e.date, availabilityIds)
      }
      
      // Step 5: Add the new event to state immediately
      setEvents(prev => [...prev, created])
      
      // Step 6: Close form and show success
      setShowNew(false)
      setNewFormError(null)
      showSuccess('Event created successfully')
    } catch (err) {
      if (err instanceof Error) {
        if (err.message.includes('Not Acceptable') || err.message.includes('not acceptable')) {
          setNewFormError('This time slot is already taken. Please choose a different time.')
        } else {
          showError(err.message)
        }
      } else {
        showError('Failed to create event')
      }
    }
  }

  // Complete update event workflow as per specification
  const onSaveEdit = async (updatedEvent: Event, originalEvent: Event) => {
    if (!user?.nameid) {
      showError('User not authenticated')
      return
    }

    try {
      // Step 1: Call validateEventForUpdate()
      await apiService.validateEventForUpdate(updatedEvent, user.nameid)
      
      // Step 2: Call checkAvailabilityForUpdate()
      const workerId = (user as PatientUser).WorkerId
      const availabilityLists = await apiService.checkAvailabilityForUpdate(
        updatedEvent,
        originalEvent.date,
        originalEvent.startTime,
        originalEvent.endTime,
        workerId
      )
      
      // Step 3: Call updateEvent()
      await apiService.updateEvent(updatedEvent, user.nameid)
      
      // Step 4: Call createSchedules() for new schedules
      if (availabilityLists.forCreateSchedules.length > 0) {
        await apiService.createSchedules(
          updatedEvent.eventId,
          updatedEvent.date,
          availabilityLists.forCreateSchedules
        )
      }
      
      // Step 5: Call deleteSchedulesByAvailabilityIds() for removed schedules
      if (availabilityLists.forDeleteSchedules.length > 0) {
        await apiService.deleteSchedulesByAvailabilityIds(
          updatedEvent.eventId,
          availabilityLists.forDeleteSchedules
        )
      }
      
      // Step 6: Call updateScheduledEvent() for updated schedules
      if (availabilityLists.forUpdateSchedules.length > 0) {
        await apiService.updateScheduledEvent(
          updatedEvent.eventId,
          availabilityLists.forUpdateSchedules
        )
      }
      
      // Step 7: Update the event in state immediately
      setEvents(prev => prev.map(evt => 
        evt.eventId === updatedEvent.eventId ? updatedEvent : evt
      ))
      
      setEditing(null)
      setEditFormError(null)
      showSuccess('Event updated successfully')
    } catch (err) {
      if (err instanceof Error) {
        if (err.message.includes('Not Acceptable') || err.message.includes('not acceptable')) {
          setEditFormError('This time slot is already taken. Please choose a different time.')
        } else {
          showError(err.message)
        }
      } else {
        showError('Failed to update event')
      }
    }
  }

  // Complete delete event workflow as per specification
  const onDelete = async (id: number) => {
    if (!user?.nameid) {
      showError('User not authenticated')
      return
    }

    try {
      // Step 1: Call deleteSchedulesByEventId()
      await apiService.deleteSchedulesByEventId(id)
      
      // Step 2: Call deleteEvent()
      await apiService.deleteEvent(id)
      
      // Step 3: Remove the event from state immediately
      setEvents(prev => prev.filter(evt => evt.eventId !== id))
      
      setEditing(null)
      showSuccess('Event deleted successfully')
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to delete event'
      showError(message)
      throw err
    }
  }

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

        <CalendarGrid
          events={events}
          availability={availability}
          weekStartISO={weekStartISO}
          onEdit={(e) => setEditing(e)}
        />
      </main>

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