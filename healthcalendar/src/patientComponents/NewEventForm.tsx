import { useEffect, useMemo, useState } from 'react'
import type { Event, Availability } from '../types/event'
import '../styles/EventFormsBase.css'

type Props = {
  availableDays: string[]
  availability: Availability[]
  onClose: () => void
  onSave: (e: Omit<Event, 'eventId'>) => void | Promise<void>
}

export default function NewEventForm({ availableDays, availability, onClose, onSave }: Props) {
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
  const [startTime, setStartTime] = useState('')
  const [endTime, setEndTime] = useState('')
  const [titleError, setTitleError] = useState<string | null>(null)
  const [locationError, setLocationError] = useState<string | null>(null)
  const [dateError, setDateError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  // Get available start times for the selected date
  const startTimeOptions = useMemo(() => {
    if (!date) return []
    
    // Get day of week for the selected date
    const selectedDate = new Date(`${date}T12:00:00`)
    const dayName = selectedDate.toLocaleDateString('en-US', { weekday: 'long' })
    
    // Filter availability slots for this day
    const daySlots = availability.filter(a => a.day === dayName)
    
    // Get unique start times and sort them
    const startTimes = [...new Set(daySlots.map(a => a.startTime))].sort()
    return startTimes
  }, [date, availability])

  // Get available end times based on selected start time
  const endTimeOptions = useMemo(() => {
    if (!date || !startTime) return []
    
    const selectedDate = new Date(`${date}T12:00:00`)
    const dayName = selectedDate.toLocaleDateString('en-US', { weekday: 'long' })
    
    // Get all slots for this day, sorted by start time
    const daySlots = availability
      .filter(a => a.day === dayName)
      .sort((a, b) => a.startTime.localeCompare(b.startTime))
    
    // Build contiguous end times starting from selected start time
    const endTimes: string[] = []
    let currentTime = startTime
    
    for (const slot of daySlots) {
      if (slot.startTime === currentTime) {
        endTimes.push(slot.endTime)
        currentTime = slot.endTime
      } else if (slot.startTime > currentTime) {
        // Gap in availability
        break
      }
    }
    
    return endTimes
  }, [date, startTime, availability])

  // Update start time when options change
  useEffect(() => {
    if (startTimeOptions.length > 0 && !startTimeOptions.includes(startTime)) {
      setStartTime(startTimeOptions[0])
    }
  }, [startTimeOptions])

  // Update end time when options change
  useEffect(() => {
    if (endTimeOptions.length > 0 && !endTimeOptions.includes(endTime)) {
      setEndTime(endTimeOptions[0])
    }
  }, [endTimeOptions])

  // Keep selected date within valid future/today range when week changes
  useEffect(() => {
    if (!validDays.includes(date)) {
      setDate(validDays[0] ?? '')
    }
  }, [validDays, date])

  const formatDateOption = (iso: string) => {
    const d = new Date(`${iso}T00:00:00Z`)
    const weekday = new Intl.DateTimeFormat('en-GB', { weekday: 'long', timeZone: 'UTC' }).format(d)
    const day = iso.slice(8, 10)
    const month = iso.slice(5, 7)
    const year = iso.slice(0, 4)
    return `${weekday} ${day}-${month}-${year}`
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
    
    setSaving(true)
    try {
      await onSave({ title, location, date, startTime, endTime, patientName: 'Alice' })
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
  <form className="form form--new-event" onSubmit={submit}>
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
              <select value={startTime} onChange={e => setStartTime(e.target.value)} disabled={startTimeOptions.length === 0}>
                {startTimeOptions.length === 0 ? (
                  <option value="">No times available</option>
                ) : (
                  startTimeOptions.map(t => (<option key={t} value={t}>{t}</option>))
                )}
              </select>
            </label>
            <label>
              End Time
              <select value={endTime} onChange={e => setEndTime(e.target.value)} disabled={endTimeOptions.length === 0}>
                {endTimeOptions.length === 0 ? (
                  <option value="">No times available</option>
                ) : (
                  endTimeOptions.map(t => (<option key={t} value={t}>{t}</option>))
                )}
              </select>
            </label>
          </div>
          <div className="form__actions">
            <button type="button" className="btn" onClick={onClose}>Cancel</button>
            <button type="submit" className="btn btn--primary" disabled={saving || validDays.length === 0 || startTimeOptions.length === 0 || endTimeOptions.length === 0}>Save</button>
          </div>
        </form>
      </div>
    </div>
  )
}
