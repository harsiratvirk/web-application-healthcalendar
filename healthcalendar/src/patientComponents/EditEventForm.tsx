import { useEffect, useMemo, useState } from 'react'
import type { Availability } from '../types/availability'
import type { Event } from '../types/event'
import '../styles/EventFormsBase.css'
import '../styles/ConfirmDialog.css'

// Form component for editing existing patient events, includes delete functionality

// Props for configuring the edit event form
type Props = {
  event: Event                                             // The event being edited
  availableDays: string[]                                  // Array of ISO date strings for the week
  availability: Availability[]                             // Worker's available time slots (pre-filtered)
  onClose: () => void                                      // Callback to close the form
  onSave: (e: Event) => void | Promise<void>              // Callback to save updated event
  onDelete: (id: number) => void | Promise<void>          // Callback to delete event
  error?: string | null                                    // Form-level error message
  onClearError?: () => void                                // Callback to clear error message
}

export default function EditEventForm({ event, availableDays, availability, onClose, onSave, onDelete, error, onClearError }: Props) {
  // Form field state - initialized with existing event values
  const [title, setTitle] = useState(event.title)
  const [location, setLocation] = useState(event.location)
  const [date, setDate] = useState(event.date)
  const [startTime, setStartTime] = useState(event.startTime)
  const [endTime, setEndTime] = useState(event.endTime)
  
  // Validation error state for each field
  const [titleError, setTitleError] = useState<string | null>(null)
  const [locationError, setLocationError] = useState<string | null>(null)
  const [dateError, setDateError] = useState<string | null>(null)
  const [conflictError, setConflictError] = useState<string | null>(null)
  
  // UI state for save/delete operations and confirmation dialog
  const [saving, setSaving] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)

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

  // Compute available end times based on selected start time
  const endTimeOptions = useMemo(() => {
    if (!date || !startTime) return []
    
    const selectedDate = new Date(`${date}T12:00:00`)
    const dayName = selectedDate.toLocaleDateString('en-US', { weekday: 'long' })
    
    // Get all availability slots for this day, sorted chronologically
    const daySlots = availability
      .filter(a => a.day === dayName)
      .sort((a, b) => a.startTime.localeCompare(b.startTime))
    
    // Build list of contiguous end times starting from selected start time
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

  // Auto-select first start time when date changes or current selection becomes invalid
  useEffect(() => {
    if (startTimeOptions.length > 0 && !startTimeOptions.includes(startTime)) {
      setStartTime(startTimeOptions[0])
    }
  }, [startTimeOptions, startTime])

  // Auto-select first end time when start time changes or current selection becomes invalid
  useEffect(() => {
    if (endTimeOptions.length > 0 && !endTimeOptions.includes(endTime)) {
      setEndTime(endTimeOptions[0])
    }
  }, [endTimeOptions, endTime])

  // Handle form submission with client-side validation
  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    // Clear previous validation errors
    setTitleError(null)
    setLocationError(null)
    setDateError(null)
    let hasError = false
    
    if (!title) { setTitleError('Title is required.'); hasError = true }
    if (!location) { setLocationError('Location is required.'); hasError = true }
    if (!date) { setDateError('Please select a date.'); hasError = true }
    
    if (!startTime || !endTime) {
      setDateError('Your worker is not available on this day. Please select a different date.');
      hasError = true;
    }
    
    if (hasError) return
    
    // Save updated event (triggers API call in parent component)
    setSaving(true)
    try {
      await onSave({ ...event, title, location, date, startTime, endTime })
    } finally {
      setSaving(false)
    }
  }

  // Handle event deletion after confirmation
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

  // Format ISO date for display (e.g., "Monday 01-12-2025")
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
    {/* Edit form modal */}
    {!showConfirm && (
      <div className="overlay" role="dialog" aria-modal="true" aria-labelledby="edit-event-title">
        <div className="modal">
          {/* Header with title and close button */}
          <header className="modal__header">
            <h2 id="edit-event-title">Edit Event</h2>
            <button className="icon-btn" onClick={onClose} aria-label="Close">
              <img src="/images/exit.png" alt="Close" />
            </button>
          </header>
          {/* Event edit form */}
          <form className="form form--edit-event" onSubmit={submit}>
            {/* Event title input */}
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
            {/* Event location input */}
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
            {/* Date and time selection row */}
            <div className="form__row">
              {/* Date dropdown - shows all days in current week */}
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
            {/* Display form-level errors */}
            {error && (
              <div className="info-message info-message--error">
                ⚠️ {error}
              </div>
            )}
            {/* Form action buttons - delete on left, cancel/save on right */}
            <div className="form__actions form__actions--edit">
              <button type="button" className="btn btn--danger" onClick={() => setShowConfirm(true)} disabled={deleting}>Delete</button>
              <div className="form__actions-spacer" />
              <button type="button" className="btn" onClick={onClose}>Cancel</button>
              <button type="submit" className="btn btn--primary" disabled={saving || startTimeOptions.length === 0 || endTimeOptions.length === 0}>Save</button>
            </div>
          </form>
        </div>
      </div>
    )}
    {/* Delete confirmation dialog */}
    {showConfirm && (
      <div className="overlay" role="dialog" aria-modal="true" aria-labelledby="confirm-delete-title" aria-describedby="confirm-delete-desc">
        <div className="modal confirm-modal">
          <header className="modal__header">
            <h2 id="confirm-delete-title">Confirm Delete</h2>
            <button className="icon-btn" onClick={() => setShowConfirm(false)} aria-label="Close confirmation">
              <img src="/images/exit.png" alt="Close" />
            </button>
          </header>
          {/* Display event details for user to verify before deletion */}
          <div id="confirm-delete-desc" className="confirm-body">
            Are you sure you want to delete this event?
            <ul className="confirm-details">
              <li><strong>Title:</strong> {event.title}</li>
              <li><strong>Date:</strong> {formatDate(event.date)}</li>
              <li><strong>Time:</strong> {event.startTime} – {event.endTime}</li>
              <li><strong>Location:</strong> {event.location}</li>
            </ul>
          </div>
          {/* Confirmation action buttons */}
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
