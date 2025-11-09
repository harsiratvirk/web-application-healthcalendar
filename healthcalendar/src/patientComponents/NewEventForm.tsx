import { useEffect, useMemo, useState } from 'react'
import type { Event } from '../types/event'
import '../styles/NewEventForm.css'

type Props = {
  availableDays: string[]
  onClose: () => void
  onSave: (e: Omit<Event, 'eventId'>) => void | Promise<void>
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

export default function NewEventForm({ availableDays, onClose, onSave }: Props) {
  const [title, setTitle] = useState('')
  const [location, setLocation] = useState('')
  // Derive today's local ISO (YYYY-MM-DD) to compare with provided ISO dates safely
  const todayISO = useMemo(() => {
    const now = new Date()
    const y = now.getFullYear()
    const m = String(now.getMonth() + 1).padStart(2, '0')
    const d = String(now.getDate()).padStart(2, '0')
    return `${y}-${m}-${d}`
  }, [])

  const validDays = useMemo(() => availableDays.filter(d => d >= todayISO), [availableDays, todayISO])

  const [date, setDate] = useState(validDays[0] ?? '')
  const [startTime, setStartTime] = useState('09:00')
  const [endTime, setEndTime] = useState('09:30')
  const [titleError, setTitleError] = useState<string | null>(null)
  const [locationError, setLocationError] = useState<string | null>(null)
  const [dateError, setDateError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  const endTimeOptions = useMemo(() => times.filter(t => t > startTime), [startTime])

  // Ensure end time stays valid when start time changes
  useEffect(() => {
    if (!endTimeOptions.includes(endTime)) {
      setEndTime(endTimeOptions[0] ?? '')
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [startTime, endTimeOptions])

  // Keep selected date within valid future/today range when week changes
  useEffect(() => {
    if (!validDays.includes(date)) {
      setDate(validDays[0] ?? '')
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [validDays])

  const formatDateOption = (iso: string) => {
    // Use English weekday and dd-MM-yyyy numeric format, timezone-safe
    const d = new Date(`${iso}T00:00:00Z`)
  // Use full weekday name for clarity
  const weekday = new Intl.DateTimeFormat('en-GB', { weekday: 'long', timeZone: 'UTC' }).format(d)
  const day = iso.slice(8, 10)
    const month = iso.slice(5, 7)
    const year = iso.slice(0, 4)
    const ddMMyyyy = `${day}-${month}-${year}`
    return `${weekday} ${ddMMyyyy}`
  }

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    // Clear field errors
    setTitleError(null)
    setLocationError(null)
    setDateError(null)
    let hasError = false
    if (!title) { setTitleError('Title is required.'); hasError = true }
    if (!location) { setLocationError('Location is required.'); hasError = true }
    if (!date) { setDateError('Please select a date.'); hasError = true }
    if (hasError) return
    try {
      setSaving(true)
      await onSave({ title, location, date, startTime, endTime, patientName: 'Alice' })
    } catch (err) {
      // Intentionally avoid top banners; could add a small message near the button if desired
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="overlay" role="dialog" aria-modal="true">
      <div className="modal">
        <header className="modal__header">
          <h2>New Event</h2>
          <button className="icon-btn" onClick={onClose} aria-label="Close">
            <img src="/images/exit.png" alt="Close" />
          </button>
        </header>
        <form className="form" onSubmit={submit}>
          <label>
            Title
            <input
              value={title}
              onChange={e => {
                const v = e.target.value
                setTitle(v)
                if (titleError && v.trim()) setTitleError(null)
              }}
              placeholder="e.g., Medication Reminder"
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
              placeholder="e.g., Home"
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
                {validDays.map(d => (
                  <option key={d} value={d}>{formatDateOption(d)}</option>
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
          <div className="form__actions">
            <button type="button" className="btn" onClick={onClose}>Cancel</button>
            <button type="submit" className="btn btn--primary" disabled={saving || validDays.length === 0}>Save</button>
          </div>
        </form>
      </div>
    </div>
  )
}
