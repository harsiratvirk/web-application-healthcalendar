import { useEffect, useMemo, useState } from 'react'
import type { Event } from '../types/event'
import '../styles/EditEventForm.css'

type Props = {
  event: Event
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

export default function EditEventForm({ event, onClose, onSave, onDelete }: Props) {
  const [title, setTitle] = useState(event.title)
  const [location, setLocation] = useState(event.location)
  const [startTime, setStartTime] = useState(event.startTime)
  const [endTime, setEndTime] = useState(event.endTime)
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [deleting, setDeleting] = useState(false)

  const endTimeOptions = useMemo(() => times.filter(t => t > startTime), [startTime])

  // Keep end time valid if start time moves past it
  useEffect(() => {
    if (!endTimeOptions.includes(endTime)) {
      setEndTime(endTimeOptions[0] ?? '')
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [startTime, endTimeOptions])

  // (Reserved) Date formatting helper removed to avoid unused variable lint error.

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    if (!title || !location || !startTime || !endTime) {
      setError('All fields are required.')
      return
    }
    try {
      setSaving(true)
      await onSave({ ...event, title, location, startTime, endTime })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update event')
    } finally {
      setSaving(false)
    }
  }

  const remove = async () => {
    setError(null)
    try {
      setDeleting(true)
      await onDelete(event.eventId)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete event')
    } finally {
      setDeleting(false)
    }
  }

  return (
    <div className="overlay" role="dialog" aria-modal="true">
      <div className="modal">
        <header className="modal__header">
          <h2>Edit Event</h2>
          <button className="icon-btn" onClick={onClose} aria-label="Close">
            <img src="/images/exit.png" alt="Close" />
          </button>
        </header>
        {error && <div className="banner banner--error">{error}</div>}
        <form className="form" onSubmit={submit}>
          <label>
            Title
            <input value={title} onChange={e => setTitle(e.target.value)} />
          </label>
          <label>
            Location
            <input value={location} onChange={e => setLocation(e.target.value)} />
          </label>
          <div className="form__row">
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
            <button type="button" className="btn btn--danger" onClick={remove} disabled={deleting}>{deleting ? 'Deleting…' : 'Delete'}</button>
            <div className="form__actions-spacer" />
            <button type="button" className="btn" onClick={onClose}>Cancel</button>
            <button type="submit" className="btn btn--primary" disabled={saving}>{saving ? 'Saving…' : 'Save'}</button>
          </div>
        </form>
      </div>
    </div>
  )
}
