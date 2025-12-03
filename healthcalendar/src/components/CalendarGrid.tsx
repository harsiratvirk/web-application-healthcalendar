import { useRef, useState, useLayoutEffect } from 'react';
import type { Event } from '../types/event';
import type { Availability } from '../types/availability'
import '../styles/CalendarGrid.css';

// Calendar grid component that displays events in a weekly view with time slots
// Supports availability highlighting and dynamic event positioning

// Props for configuring the calendar grid display
export type CalendarGridProps = {
  events: Event[]                      // Array of events to display
  availability?: Availability[]        // Worker's availability for the week
  weekStartISO: string                 // Monday of the week in YYYY-MM-DD format
  startHour?: number                   // Start hour of day (default 8)
  endHour?: number                     // End hour of day (default 20)
  slotMinutes?: number                 // Duration of each time slot (default 30)
  onEdit?: (e: Event) => void          // Callback when event is clicked for editing
}

// Convert time string (HH:MM) to total minutes since midnight
const toMinutes = (t: string) => {
  const [h, m] = t.split(':').map(Number)
  return h * 60 + m
}

// Format minutes since midnight as HH:MM time label
const formatTimeLabel = (mins: number) => {
  const h = Math.floor(mins / 60)
  const m = mins % 60
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`
}

// Convert Date object to YYYY-MM-DD ISO string in local timezone
const toLocalISO = (date: Date) => {
  const y = date.getFullYear()
  const m = String(date.getMonth() + 1).padStart(2, '0')
  const d = String(date.getDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}

// Add specified number of days to an ISO date string
const addDays = (iso: string, days: number) => {
  const y = Number(iso.slice(0, 4))
  const m = Number(iso.slice(5, 7)) - 1
  const d = Number(iso.slice(8, 10))
  const date = new Date(y, m, d)
  date.setDate(date.getDate() + days)
  return toLocalISO(date)
}

export default function CalendarGrid({
  events,
  availability = [],
  weekStartISO,
  startHour = 8,
  endHour = 20,
  slotMinutes = 30,
  onEdit
}: CalendarGridProps) {
  // Convert hours to minutes for easier time calculations
  const startMins = startHour * 60;
  const endMins = endHour * 60;
  const totalSlots = Math.floor((endMins - startMins) / slotMinutes);

  // Generate time labels for each slot (e.g., 08:00, 08:30...)
  const timeLabels = Array.from({ length: totalSlots }, (_, i) => startMins + i * slotMinutes);

  // Generate array of 7 days starting from weekStartISO (Monday)
  const days = Array.from({ length: 7 }, (_, i) => addDays(weekStartISO, i));

  const now = new Date();
  const todayISO = toLocalISO(new Date(now.getFullYear(), now.getMonth(), now.getDate()));

  const dayNamesMap = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

  // Check if a specific time slot is available for a given day based on worker availability
  const isSlotAvailable = (dayISO: string, slotStartMins: number, slotEndMins: number): boolean => {
    const dateObj = new Date(dayISO + 'T00:00:00');
    const dayName = dayNamesMap[dateObj.getDay()];

    return availability.some(a => {
      if (a.day !== dayName) return false;
      const availStart = toMinutes(a.startTime);
      const availEnd = toMinutes(a.endTime);
      return slotStartMins >= availStart && slotEndMins <= availEnd;
    });
  };

  // Ref to the container holding all day columns for measuring dimensions
  const columnContainerRef = useRef<HTMLDivElement | null>(null);
  const firstSlotRef = useRef<HTMLDivElement | null>(null);

  const [eventRects, setEventRects] = useState<Record<string, { top: number; height: number }>>({});

  const eventsByDay = days.map(d => events.filter(e => e.date === d));

  const computeEventRects = () => {
    const cols = columnContainerRef.current?.querySelectorAll<HTMLDivElement>('.cal-grid__col');
    if (!cols) return {};

    // Object to store calculated positions for each event
    const rects: Record<string, { top: number; height: number }> = {};

    // Process each day column
    for (let colIdx = 0; colIdx < cols.length; colIdx++) {
      const col = cols[colIdx];
      const colRect = col.getBoundingClientRect();

      const slotEls = Array.from(col.querySelectorAll<HTMLDivElement>('.cal-grid__slot'));
      if (slotEls.length === 0) continue;

      // Calculate top position of each slot relative to column top
      const slotTops = slotEls.map(s => {
        const r = s.getBoundingClientRect();
        return r.top - colRect.top;
      });

      // Calculate bottom boundary of column for events extending past last slot
      const lastSlotRect = slotEls[slotEls.length - 1].getBoundingClientRect();
      const colBottom = lastSlotRect.bottom - colRect.top + (parseFloat(getComputedStyle(slotEls[0]).borderBottomWidth || '0') || 0);

      // Process all events in this day column
      const colEvents = eventsByDay[colIdx] || [];
      for (const e of colEvents) {
        // Calculate which slot indices the event spans
        const startSlotIdx = Math.floor((toMinutes(e.startTime) - startMins) / slotMinutes);
        const maybeEndSlot = Math.ceil((toMinutes(e.endTime) - startMins) / slotMinutes);

        // Clamp slot indices to valid ranges
        const startSlot = Math.max(0, Math.min(slotEls.length - 1, startSlotIdx));
        const endSlot = Math.max(0, Math.min(slotEls.length, maybeEndSlot));

        // Calculate event top position from start slot and bottom
        const top = slotTops[startSlot] ?? 0;
        const bottom = (endSlot < slotTops.length) ? slotTops[endSlot] : colBottom;
        const height = Math.max(0, bottom - top);

        // Store calculated position for this event
        rects[e.eventId] = { top, height };
      }
    }

    return rects;
  };

  // Recalculate event positions when component mounts or calendar configuration changes
  useLayoutEffect(() => {
    // Function to update all event positions based on current DOM measurements
    const update = () => {
      const r = computeEventRects();
      setEventRects(r);
    };

    update();

    // Set up ResizeObserver to recalculate when container size changes
    const ro = new ResizeObserver(() => update());
    if (columnContainerRef.current) ro.observe(columnContainerRef.current);

    window.addEventListener('resize', update);

    const id = window.setTimeout(update, 50);

    // Cleanup observers and listeners
    return () => {
      ro.disconnect();
      window.removeEventListener('resize', update);
      clearTimeout(id);
    };
  }, [events, weekStartISO, startHour, endHour, slotMinutes]);

  return (
    <div className="cal-grid">
      {/* Calendar header with day names and dates */}
      <div className="cal-grid__header">
        <div className="cal-grid__corner" />
        {/* Render day headers for the week */}
        {days.map((d) => {
          // Parse date string to create Date object
          const y = Number(d.slice(0, 4));
          const m = Number(d.slice(5, 7)) - 1;
          const day = Number(d.slice(8, 10));
          const dateObj = new Date(y, m, day);
          // Format weekday name and date number
          const weekday = new Intl.DateTimeFormat('en-GB', { weekday: 'long' }).format(dateObj);
          const dayLabel = String(dateObj.getDate()).padStart(2, '0');
          return (
            <div className={`cal-grid__day${d === todayISO ? ' cal-grid__day--today' : ''}`} key={d}>
              <div className="cal-grid__dayname">{weekday} {dayLabel}</div>
            </div>
          );
        })}
      </div>

      {/* Calendar body containing time labels and day columns */}
      <div className="cal-grid__body">
        <div className="cal-grid__times">
          {timeLabels.map((m, i) => (
            <div
              className="cal-grid__time"
              key={m}
              ref={i === 0 ? firstSlotRef : undefined}
            >
              {formatTimeLabel(m)}
            </div>
          ))}
        </div>

        {/* Container for all day columns */}
        <div className="cal-grid__days" ref={columnContainerRef}>
          {days.map((d, idx) => {
            const evs = eventsByDay[idx];
            const colClasses = `cal-grid__col${d === todayISO ? ' cal-grid__col--today' : ''}`;
            return (
              <div className={colClasses} key={d}>
                {/* Render time slot backgrounds with availability styling */}
                {timeLabels.map((m) => {
                  const slotStart = m;
                  const slotEnd = m + slotMinutes;
                  // Check if worker is available during this slot
                  const isAvailable = isSlotAvailable(d, slotStart, slotEnd);
                  const slotClasses = `cal-grid__slot${!isAvailable ? ' cal-grid__slot--unavailable' : ''}`;
                  return (
                    <div className={slotClasses} key={m + d} />
                  );
                })}
                {/* Render events positioned absolutely over slots */}
                {evs.map(e => {
                  // Get calculated position for this event (or default to 0)
                  const rect = eventRects[e.eventId];
                  const safeTop = rect ? rect.top : 0;
                  const safeHeight = rect ? Math.max(0, rect.height - 5) : 0;

                  return (
                    <div
                      key={e.eventId}
                      className="cal-grid__event"
                      style={{ top: `${safeTop}px`, height: `${safeHeight}px` }}
                      onClick={() => onEdit?.(e)}
                      title={`${e.title} @ ${e.location}\n${e.startTime} - ${e.endTime}`}
                    >
                      <div className="cal-grid__event-title">{e.title}</div>
                      <div className="cal-grid__event-undertitle">{e.location}</div>
                      {/* Edit button */}
                      <button className="cal-grid__event-edit" aria-label="Edit event" onClick={(ev) => { ev.stopPropagation(); onEdit?.(e) }}>
                        <img src="/images/edit.png" alt="Edit" />
                      </button>
                    </div>
                  );
                })}
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
