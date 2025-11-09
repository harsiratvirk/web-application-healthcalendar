import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import type { Event } from '../types/event'
import { apiService } from '../services/apiService'
import CalendarGrid from '../components/CalendarGrid'
import '../styles/EventCalendar.css'
import { useToast } from '../shared/toastContext'
import NewEventForm from './NewEventForm'
import EditEventForm from './EditEventForm'

function toLocalISO(date: Date) {
  const y = date.getFullYear()
  const m = String(date.getMonth() + 1).padStart(2, '0')
  const d = String(date.getDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}

function startOfWeekMondayISO(d: Date) {
  const date = new Date(d.getFullYear(), d.getMonth(), d.getDate()) // local midnight
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
  const [events, setEvents] = useState<Event[]>([])
  const [loading, setLoading] = useState(false)
  const [weekStartISO, setWeekStartISO] = useState(startOfWeekMondayISO(new Date()))
  const [showNew, setShowNew] = useState(false)
  const [editing, setEditing] = useState<Event | null>(null)
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

  useEffect(() => {
    const load = async () => {
      try {
        setLoading(true)
        // First: load from localStorage if exists
        const stored = localStorage.getItem('hc_events')
        if (stored) {
          setEvents(JSON.parse(stored))
        } else {
          const data = await apiService.getEvents()
          setEvents(data)
        }
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Failed to load events'
        showError(message)
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [showError])

  const onSaveNew = async (e: Omit<Event, 'eventId'>) => {
    try {
      const created = await apiService.createEvent(e)
      setEvents(prev => {
        const next = [...prev, created]
        localStorage.setItem('hc_events', JSON.stringify(next))
        return next
      })
      setShowNew(false)
      showSuccess('Event created')
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to create event'
      showError(message)
      throw err
    }
  }

  const onSaveEdit = async (e: Event) => {
    try {
      const updated = await apiService.updateEvent(e)
      setEvents(prev => {
        const next = prev.map(p => p.eventId === updated.eventId ? updated : p)
        localStorage.setItem('hc_events', JSON.stringify(next))
        return next
      })
      setEditing(null)
      showSuccess('Event updated')
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to update event'
      showError(message)
      throw err
    }
  }

  const onDelete = async (id: number) => {
    try {
      await apiService.deleteEvent(id)
      setEvents(prev => {
        const next = prev.filter(p => p.eventId !== id)
        localStorage.setItem('hc_events', JSON.stringify(next))
        return next
      })
      setEditing(null)
      showSuccess('Event deleted')
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
            <h1 className="event-title">Alice’s Event Calendar</h1>
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
            <button className="logout-btn" onClick={() => navigate('/login')}>
              <img src="/images/logout.png" alt="Logout" />
              <span>Log Out</span>
            </button>
            <button className="add-btn" onClick={() => setShowNew(true)}>+ Add New Event</button>
          </div>
        </header>

        {loading && <div className="banner">Loading…</div>}

        <CalendarGrid
          events={events}
          weekStartISO={weekStartISO}
          onEdit={(e) => setEditing(e)}
        />
      </main>

      {showNew && (
        <NewEventForm
          availableDays={availableDays}
          onClose={() => setShowNew(false)}
          onSave={onSaveNew}
        />
      )}

      {editing && (
        <EditEventForm
          event={editing}
          onClose={() => setEditing(null)}
          onSave={onSaveEdit}
          onDelete={onDelete}
        />
      )}
    </div>
  )
}
