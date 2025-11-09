import type { Event } from '../types/event'
import '../styles/CalendarGrid.css'

export type CalendarGridProps = {
  events: Event[]
  weekStartISO: string // Monday of the week in YYYY-MM-DD
  startHour?: number // default 8
  endHour?: number // default 20
  slotMinutes?: number // default 30
  onEdit?: (e: Event) => void
}

// Helper to convert time strings to minutes from midnight
const toMinutes = (t: string) => {
  const [h, m] = t.split(':').map(Number)
  return h * 60 + m
}

const formatTimeLabel = (mins: number) => {
  const h = Math.floor(mins / 60)
  const m = mins % 60
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`
}

const toLocalISO = (date: Date) => {
  const y = date.getFullYear()
  const m = String(date.getMonth() + 1).padStart(2, '0')
  const d = String(date.getDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}

const addDays = (iso: string, days: number) => {
  const y = Number(iso.slice(0, 4))
  const m = Number(iso.slice(5, 7)) - 1
  const d = Number(iso.slice(8, 10))
  const date = new Date(y, m, d)
  date.setDate(date.getDate() + days)
  return toLocalISO(date)
}

// Weekday labels will be localized (Norwegian) per date below

export default function CalendarGrid({
  events,
  weekStartISO,
  startHour = 8,
  endHour = 20,
  slotMinutes = 30,
  onEdit
}: CalendarGridProps) {
  const startMins = startHour * 60
  const endMins = endHour * 60
  const totalSlots = Math.floor((endMins - startMins) / slotMinutes)

  const timeLabels = Array.from({ length: totalSlots + 1 }, (_, i) => startMins + i * slotMinutes)

  const days = Array.from({ length: 7 }, (_, i) => addDays(weekStartISO, i))
  const now = new Date()
  const todayISO = toLocalISO(new Date(now.getFullYear(), now.getMonth(), now.getDate()))

  const eventsByDay = days.map(d => events.filter(e => e.date === d))

  return (
    <div className="cal-grid">
      <div className="cal-grid__header">
        <div className="cal-grid__corner" />
        {days.map((d) => {
          const y = Number(d.slice(0, 4))
          const m = Number(d.slice(5, 7)) - 1
          const day = Number(d.slice(8, 10))
          const dateObj = new Date(y, m, day)
          const weekday = new Intl.DateTimeFormat('en-GB', { weekday: 'short' }).format(dateObj)
          const dayLabel = String(dateObj.getDate()).padStart(2, '0')
          return (
            <div className={`cal-grid__day${d === todayISO ? ' cal-grid__day--today' : ''}`} key={d}>
              <div className="cal-grid__dayname">{weekday} {dayLabel}</div>
            </div>
          )
        })}
      </div>
      <div className="cal-grid__body">
        <div className="cal-grid__times">
          {timeLabels.map((m) => (
            <div className="cal-grid__time" key={m}>
              {formatTimeLabel(m)}
            </div>
          ))}
        </div>
        <div className="cal-grid__days">
          {days.map((d, idx) => {
            const evs = eventsByDay[idx]
            return (
              <div className={`cal-grid__col${d === todayISO ? ' cal-grid__col--today' : ''}`} key={d}>
                {/* slots background */}
                {timeLabels.map((m) => (
                  <div className="cal-grid__slot" key={m + d} />
                ))}
                {/* events */}
                {evs.map(e => {
                  const top = ((toMinutes(e.startTime) - startMins) / (endMins - startMins)) * 100
                  const height = ((toMinutes(e.endTime) - toMinutes(e.startTime)) / (endMins - startMins)) * 100
                  return (
                    <div
                      key={e.eventId}
                      className="cal-grid__event"
                      style={{ top: `${top}%`, height: `${height}%` }}
                      onClick={() => onEdit?.(e)}
                      title={`${e.title} @ ${e.location}\n${e.startTime} - ${e.endTime}`}
                    >
                      <div className="cal-grid__event-title">{e.title}</div>
                      <div className="cal-grid__event-location">{e.location}</div>
                      <div className="cal-grid__event-meta">{e.startTime} - {e.endTime}</div>
                      <button className="cal-grid__event-edit" aria-label="Edit event" onClick={(ev) => { ev.stopPropagation(); onEdit?.(e) }}>
                        <img src="/images/edit.png" alt="Edit" />
                      </button>
                    </div>
                  )
                })}
              </div>
            )
          })}
        </div>
      </div>
    </div>
  )
}
