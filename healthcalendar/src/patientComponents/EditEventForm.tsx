import { useEffect, useMemo, useState } from 'react'
import type { Event, Availability } from '../types/event'
import '../styles/EventFormsBase.css'
import '../styles/ConfirmDialog.css'

type Props = {
  event: Event
  availableDays: string[]
  availability: Availability[]
  onClose: () => void
  onSave: (e: Event) => void | Promise<void>
  onDelete: (id: number) => void | Promise<void>
}

const times = (() => {
  const arr: string[] = []
  for (let h = 8; h <= 20; h++) {
    for (let m = 0; m < 60; m += 30) {
      const hh = String(h).padStart(2, '0')
      const mm = String(m).padStart(2, '0')
      arr.push(`${hh}:${mm}`)
    }
  }
  return arr
})()

export default function EditEventForm({ event, availableDays, availability, onClose, onSave, onDelete }: Props) {
  const [title, setTitle] = useState(event.title)
  const [location, setLocation] = useState(event.location)
  const [date, setDate] = useState(event.date)
  const [startTime, setStartTime] = useState(event.startTime)
  const [endTime, setEndTime] = useState(event.endTime)
  const [titleError, setTitleError] = useState<string | null>(null)
  const [locationError, setLocationError] = useState<string | null>(null)
  const [dateError, setDateError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)

  const endTimeOptions = useMemo(() => times.filter(t => t > startTime), [startTime])

  useEffect(() => {
    if (!endTimeOptions.includes(endTime)) {
      setEndTime(endTimeOptions[0] ?? '')
    }
  }, [startTime, endTimeOptions, endTime])

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    setTitleError(null)
    setLocationError(null)
    setDateError(null)
    let hasError = false
    if (!title) { setTitleError('Title is required.'); hasError = true }
    if (!location) { setLocationError('Location is required.'); hasError = true }
    if (!date) { setDateError('Please select a date.'); hasError = true }
    if (hasError) return
    
    setSaving(true)
    try {
      await onSave({ ...event, title, location, date, startTime, endTime })
    } finally {
      setSaving(false)
    }
  }

  const remove = async () => {
    try {
      setDeleting(true)
      await onDelete(event.eventId)
    } catch (err) {
      console.debug('Delete failed (suppressed UI error)', err)
    } finally {
      setDeleting(false)
      setShowConfirm(false)
    }
  }

  const formatDate = (iso: string) => {
    const d = new Date(`${iso}T00:00:00Z`)
    const weekday = new Intl.DateTimeFormat('en-GB', { weekday: 'long', timeZone: 'UTC' }).format(d)
    const dd = iso.slice(8, 10)
    const mm = iso.slice(5, 7)
    const yyyy = iso.slice(0, 4)
    return `${weekday} ${dd}-${mm}-${yyyy}`
  }

  return (
    <>
    {!showConfirm && (
      <div className="overlay" role="dialog" aria-modal="true" aria-labelledby="edit-event-title">
        <div className="modal">
          <header className="modal__header">
            <h2 id="edit-event-title">Edit Event</h2>
            <button className="icon-btn" onClick={onClose} aria-label="Close">
              <img src="/images/exit.png" alt="Close" />
            </button>
          </header>
          <form className="form form--edit-event" onSubmit={submit}>
            <label>
              Title
              <input
                value={title}
                onChange={e => {
                  const v = e.target.value
                  setTitle(v)
                  if (titleError && v.trim()) setTitleError(null)
                }}
                aria-invalid={!!titleError}
              />
              {titleError && <small className="field-error">{titleError}</small>}
            </label>
            <label>
              Location
              <input
                value={location}
                onChange={e => {
                  const v = e.target.value
                  setLocation(v)
                  if (locationError && v.trim()) setLocationError(null)
                }}
                aria-invalid={!!locationError}
              />
              {locationError && <small className="field-error">{locationError}</small>}
            </label>
            <div className="form__row">
              <label>
                Date
                <select
                  value={date}
                  onChange={e => {
                    const v = e.target.value
                    setDate(v)
                    if (dateError && v) setDateError(null)
                  }}
                  aria-invalid={!!dateError}
                >
                  {availableDays.map(d => (
                    <option key={d} value={d}>{formatDate(d)}</option>
                  ))}
                </select>
                {dateError && <small className="field-error">{dateError}</small>}
              </label>
              <label>
                Start Time
                <select value={startTime} onChange={e => setStartTime(e.target.value)}>
                  {times.map(t => (<option key={t} value={t}>{t}</option>))}
                </select>
              </label>
              <label>
                End Time
                <select value={endTime} onChange={e => setEndTime(e.target.value)}>
                  {endTimeOptions.map(t => (<option key={t} value={t}>{t}</option>))}
                </select>
              </label>
            </div>
            <div className="form__actions form__actions--edit">
              <button type="button" className="btn btn--danger" onClick={() => setShowConfirm(true)} disabled={deleting}>Delete</button>
              <div className="form__actions-spacer" />
              <button type="button" className="btn" onClick={onClose}>Cancel</button>
              <button type="submit" className="btn btn--primary" disabled={saving}>Save</button>
            </div>
          </form>
        </div>
      </div>
    )}
    {showConfirm && (
      <div className="overlay" role="dialog" aria-modal="true" aria-labelledby="confirm-delete-title" aria-describedby="confirm-delete-desc">
        <div className="modal confirm-modal">
          <header className="modal__header">
            <h2 id="confirm-delete-title">Confirm Delete</h2>
            <button className="icon-btn" onClick={() => setShowConfirm(false)} aria-label="Close confirmation">
              <img src="/images/exit.png" alt="Close" />
            </button>
          </header>
          <div id="confirm-delete-desc" className="confirm-body">
            Are you sure you want to delete this event?
            <ul className="confirm-details">
              <li><strong>Title:</strong> {event.title}</li>
              <li><strong>Date:</strong> {formatDate(event.date)}</li>
              <li><strong>Time:</strong> {event.startTime} – {event.endTime}</li>
              <li><strong>Location:</strong> {event.location}</li>
            </ul>
          </div>
          <div className="confirm-actions">
            <button type="button" className="btn" onClick={() => setShowConfirm(false)} disabled={deleting}>Cancel</button>
            <button type="button" className="btn btn--danger" onClick={remove} disabled={deleting}>Delete</button>
          </div>
        </div>
      </div>
    )}
  </>
  )
}
