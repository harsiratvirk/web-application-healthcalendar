import { useEffect, useMemo, useState } from 'react'
import type { Availability } from '../types/availability'
import type { Event } from '../types/event'
import '../styles/EventFormsBase.css'

// Form component for creating new patient events

// Props for configuring the new event form
type Props = {
  availableDays: string[]                                      // Array of ISO date strings for the week
  availability: Availability[]                                 // Worker's available time slots (pre-filtered)
  existingEvents: Event[]                                      // Patient's existing events for conflict detection
  onClose: () => void                                          // Callback to close the form
  onSave: (e: Omit<Event, 'eventId'>) => void | Promise<void> // Callback to save the new event
  error?: string | null                                        // Form-level error message
  onClearError?: () => void                                    // Callback to clear error message
}

export default function NewEventForm({ availableDays, availability, existingEvents, onClose, onSave, error, onClearError }: Props) {
  const [title, setTitle] = useState('')
  const [location, setLocation] = useState('')
  
  // Calculate today's date in ISO format (YYYY-MM-DD) for filtering past dates
  const todayISO = useMemo(() => {
    const now = new Date()
    const y = now.getFullYear()
    const m = String(now.getMonth() + 1).padStart(2, '0')
    const d = String(now.getDate()).padStart(2, '0')
    return `${y}-${m}-${d}`
  }, [])

  // Filter out past dates - only allow today and future dates
  const validDays = useMemo(() => availableDays.filter(d => d >= todayISO), [availableDays, todayISO])

  // Date and time state - initialize date to first valid day
  const [date, setDate] = useState(validDays[0] ?? '')
  const [startTime, setStartTime] = useState('')
  const [endTime, setEndTime] = useState('')
  
  // Validation error state for each field
  const [titleError, setTitleError] = useState<string | null>(null)
  const [locationError, setLocationError] = useState<string | null>(null)
  const [dateError, setDateError] = useState<string | null>(null)
  const [conflictError, setConflictError] = useState<string | null>(null)
  
  // UI state for save operation
  const [saving, setSaving] = useState(false)

  // Compute available start times for the selected date
  // Extracts unique start times from worker's availability for the chosen day
  const startTimeOptions = useMemo(() => {
    if (!date) return []
    
    const selectedDate = new Date(`${date}T12:00:00`)
    const dayName = selectedDate.toLocaleDateString('en-US', { weekday: 'long' })
    
    // Get all availability slots matching this day of week
    const daySlots = availability.filter(a => a.day === dayName)
    
    const startTimes = [...new Set(daySlots.map(a => a.startTime))].sort()
    return startTimes
  }, [date, availability])

  // Check if the selected date and time overlap with existing events
  const hasTimeConflict = useMemo(() => {
    if (!date || !startTime || !endTime) return false
    
    // Check if any existing event overlaps with the selected time slot
    return existingEvents.some(event => {
      if (event.date !== date) return false
      
      // Convert times to comparable format
      const newStart = startTime
      const newEnd = endTime
      const eventStart = event.startTime
      const eventEnd = event.endTime
      
      // Check for overlap: events overlap if one starts before the other ends
      return (newStart < eventEnd && newEnd > eventStart)
    })
  }, [date, startTime, endTime, existingEvents])

  // Compute available end times based on selected start time
  // Only shows contiguous time slots (stops at first gap in availability)
  const endTimeOptions = useMemo(() => {
    if (!date || !startTime) return []
    
    const selectedDate = new Date(`${date}T12:00:00`)
    const dayName = selectedDate.toLocaleDateString('en-US', { weekday: 'long' })
    
    const daySlots = availability
      .filter(a => a.day === dayName)
      .sort((a, b) => a.startTime.localeCompare(b.startTime))

    const endTimes: string[] = []
    let currentTime = startTime
    
    for (const slot of daySlots) {
      if (slot.startTime === currentTime) {
        // This slot continues from the previous one
        endTimes.push(slot.endTime)
        currentTime = slot.endTime
      } else if (slot.startTime > currentTime) {
        break
      }
    }
    
    return endTimes
  }, [date, startTime, availability])

  // Auto-select first start time when date changes or options become available
  useEffect(() => {
    if (startTimeOptions.length > 0 && !startTimeOptions.includes(startTime)) {
      setStartTime(startTimeOptions[0])
    }
  }, [startTimeOptions])

  // Auto-select first end time when start time changes or options become available
  useEffect(() => {
    if (endTimeOptions.length > 0 && !endTimeOptions.includes(endTime)) {
      setEndTime(endTimeOptions[0])
    }
  }, [endTimeOptions])

  // Reset date selection when week changes and current date is no longer valid
  // This ensures the date always falls within the current week view
  useEffect(() => {
    if (!validDays.includes(date)) {
      setDate(validDays[0] ?? '')
    }
  }, [validDays, date])

  // Show conflict error when time range overlaps with existing event
  useEffect(() => {
    if (hasTimeConflict) {
      setConflictError('This time slot is already booked by you. Please select a different time.')
    } else {
      setConflictError(null)
    }
  }, [hasTimeConflict])

  // Format ISO date for display in dropdown (e.g., "Monday 01-12-2025")
  const formatDateOption = (iso: string) => {
    const d = new Date(`${iso}T00:00:00Z`)
    const weekday = new Intl.DateTimeFormat('en-GB', { weekday: 'long', timeZone: 'UTC' }).format(d)
    const day = iso.slice(8, 10)
    const month = iso.slice(5, 7)
    const year = iso.slice(0, 4)
    return `${weekday} ${day}-${month}-${year}`
  }

  // Handle form submission with client-side validation
  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    // Clear previous validation errors
    setTitleError(null)
    setLocationError(null)
    setDateError(null)
    setConflictError(null)
    let hasError = false
    
    // Validate required fields
    if (!title) { setTitleError('Title is required.'); hasError = true }
    if (!location) { setLocationError('Location is required.'); hasError = true }
    if (!date) { setDateError('Please select a date.'); hasError = true }
    
    // Validate time selection (worker must be available)
    if (!startTime || !endTime) {
      setDateError('Your worker is not available on this day. Please select a different date.');
      hasError = true;
    }
    
    // Check for time conflicts with existing events
    if (hasTimeConflict) {
      setConflictError('This time slot is already booked by you. Please select a different time.');
      hasError = true;
    }
    
    if (hasError) return
    
    // Save event (triggers API call in parent component)
    setSaving(true)
    try {
      await onSave({ title, location, date, startTime, endTime, patientName: 'Alice' })
    } finally {
      setSaving(false)
    }
  }

  return (
    // Modal overlay with form for creating new events
    <div className="overlay" role="dialog" aria-modal="true">
      <div className="modal">
        {/* Header with title and close button */}
        <header className="modal__header">
          <h2>New Event</h2>
          <button className="icon-btn" onClick={onClose} aria-label="Close">
            <img src="/images/exit.png" alt="Close" />
          </button>
        </header>
        {/* Event creation form */}
        <form className="form form--new-event" onSubmit={submit}>
          {/* Event title input */}
          <label>
            Title
            <input
              value={title}
              onChange={e => {
                const v = e.target.value
                setTitle(v)
                // Clear error when user starts typing
                if (titleError && v.trim()) setTitleError(null)
              }}
              placeholder="e.g., Medication Reminder"
              aria-invalid={!!titleError}
            />
            {titleError && <small className="field-error">{titleError}</small>}
          </label>
          {/* Event location input */}
          <label>
            Location
            <input
              value={location}
              onChange={e => {
                const v = e.target.value
                setLocation(v)
                // Clear error when user starts typing
                if (locationError && v.trim()) setLocationError(null)
              }}
              placeholder="e.g., Home"
              aria-invalid={!!locationError}
            />
            {locationError && <small className="field-error">{locationError}</small>}
          </label>
          {/* Date and time selection row */}
          <div className="form__row">
            {/* Date dropdown - shows only valid future dates */}
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
            {/* Start time dropdown - based on worker availability */}
            <label>
              Start Time
              <select value={startTime} onChange={e => setStartTime(e.target.value)} disabled={startTimeOptions.length === 0}>
                {startTimeOptions.length === 0 ? (
                  <option value="">-</option>
                ) : (
                  startTimeOptions.map(t => (<option key={t} value={t}>{t}</option>))
                )}
              </select>
            </label>
            {/* End time dropdown - shows only contiguous time slots */}
            <label>
              End Time
              <select value={endTime} onChange={e => setEndTime(e.target.value)} disabled={endTimeOptions.length === 0}>
                {endTimeOptions.length === 0 ? (
                  <option value="">-</option>
                ) : (
                  endTimeOptions.map(t => (<option key={t} value={t}>{t}</option>))
                )}
              </select>
            </label>
          </div>
          {/* Warning message when worker is not available on selected date */}
          {(startTimeOptions.length === 0 || endTimeOptions.length === 0) && (
            <div className="info-message">
              ⚠️ Your worker is not available on this day. Please select a different date.
            </div>
          )}
          {/* Display conflict warning */}
          {conflictError && (
            <div className="info-message info-message--error">
              ⚠️ {conflictError}
            </div>
          )}
          {/* Display form-level errors */}
          {error && (
            <div className="info-message info-message--error">
              ⚠️ {error}
            </div>
          )}
          {/* Form action buttons */}
          <div className="form__actions">
            <button type="button" className="btn" onClick={onClose}>Cancel</button>
            <button type="submit" className="btn btn--primary" disabled={saving || validDays.length === 0 || startTimeOptions.length === 0 || endTimeOptions.length === 0 || hasTimeConflict}>Save</button>
          </div>
        </form>
      </div>
    </div>
  )
}
