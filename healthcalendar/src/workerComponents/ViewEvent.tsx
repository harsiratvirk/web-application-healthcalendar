import type { Event } from '../types/event'
import '../styles/WorkerViewEvent.css'

// Modal for workers to view event details
type Props = {
  event: Event
  onClose: () => void
}

export default function ViewEvent({ event, onClose }: Props) {
  // Format ISO date (YYYY-MM-DD) to readable format
  const formatDate = (iso: string) => {
    const d = new Date(`${iso}T00:00:00Z`)
    const weekday = new Intl.DateTimeFormat('en-GB', { weekday: 'long', timeZone: 'UTC' }).format(d)
    const dd = iso.slice(8, 10)
    const mm = iso.slice(5, 7)
    const yyyy = iso.slice(0, 4)
    return `${weekday} ${dd}-${mm}-${yyyy}`
  }

  return (
    <div className="worker-overlay" role="dialog" aria-modal="true" aria-labelledby="view-event-title">
      <div className="worker-modal">
        <header className="worker-modal__header">
          <h2 id="view-event-title">View Event</h2>
          <button className="icon-btn" onClick={onClose} aria-label="Close">
            <img src="/images/exit.png" alt="Close" />
          </button>
        </header>

        <div className="view-event__content">
          <div className="view-field">
            <label>Patient:</label>
            <p>{event.patientName}</p>
          </div>
          <div className="view-field">
            <label>Title:</label>
            <p>{event.title}</p>
          </div>
          <div className="view-field">
            <label>Location:</label>
            <p>{event.location}</p>
          </div>
          <div className="view-field">
            <label>Date:</label>
            <p>{formatDate(event.date)}</p>
          </div>
          <div className="view-event__row">
            <div className="view-field">
              <label>Start Time</label>
              <p>{event.startTime}</p>
            </div>
            <div className="view-field">
              <label>End Time</label>
              <p>{event.endTime}</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
