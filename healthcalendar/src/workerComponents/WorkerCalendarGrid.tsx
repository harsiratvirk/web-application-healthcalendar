import { useRef, useState, useLayoutEffect } from 'react';
import type { Event } from '../types/event';
import type { Availability } from '../types/availability'

import '../styles/CalendarGrid.css';

export type CalendarGridProps = {
  events: Event[]
  availability?: Availability[] // Worker's availability for the week
  weekStartISO: string // Monday of the week in YYYY-MM-DD
  startHour?: number // default 8
  endHour?: number // default 20
  slotMinutes?: number // default 30
  onEdit?: (e: Event) => void
  onSlotClick?: (dateISO: string, timeMins: number, dayName: string) => void
  isAvailabilityMode?: boolean
}

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

export default function CalendarGrid({
  events,
  availability = [],
  weekStartISO,
  startHour = 8,
  endHour = 20,
  slotMinutes = 30,
  onEdit,
  onSlotClick,
  isAvailabilityMode = false
}: CalendarGridProps) {
  const startMins = startHour * 60;
  const endMins = endHour * 60;
  const totalSlots = Math.floor((endMins - startMins) / slotMinutes);

  // time labels (slot top boundaries)
  const timeLabels = Array.from({ length: totalSlots }, (_, i) => startMins + i * slotMinutes);

  const days = Array.from({ length: 7 }, (_, i) => addDays(weekStartISO, i));
  const now = new Date();
  const todayISO = toLocalISO(new Date(now.getFullYear(), now.getMonth(), now.getDate()));

  // Helper to check if a specific time slot is available for a given day
  const dayNamesMap = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
  const isSlotAvailable = (dayISO: string, slotStartMins: number, slotEndMins: number): boolean => {
    const dateObj = new Date(dayISO + 'T00:00:00');
    const dayName = dayNamesMap[dateObj.getDay()];

    // Check if any availability block covers this slot
    return availability.some(a => {
      if (a.day !== dayName) return false;
      const availStart = toMinutes(a.startTime);
      const availEnd = toMinutes(a.endTime);
      // Slot is available if it falls completely within an availability block
      return slotStartMins >= availStart && slotEndMins <= availEnd;
    });
  };

  // refs
  const columnContainerRef = useRef<HTMLDivElement | null>(null);

  // event positions map: eventId -> { topPx, heightPx }
  const [eventRects, setEventRects] = useState<Record<string, { top: number; height: number }>>({});

  // Convenience: group events by day to iterate
  const eventsByDay = days.map(d => events.filter(e => e.date === d));

  // Helper to compute positions using actual slot DOM nodes in each column
  const computeEventRects = () => {
    const cols = columnContainerRef.current?.querySelectorAll<HTMLDivElement>('.cal-grid__col');
    if (!cols) return {};

    const rects: Record<string, { top: number; height: number }> = {};

    for (let colIdx = 0; colIdx < cols.length; colIdx++) {
      const col = cols[colIdx];
      const colRect = col.getBoundingClientRect();
      // collect the slot elements for this column
      const slotEls = Array.from(col.querySelectorAll<HTMLDivElement>('.cal-grid__slot'));
      if (slotEls.length === 0) continue;

      // For convenience, build an array of slot top relative to column
      const slotTops = slotEls.map(s => {
        const r = s.getBoundingClientRect();
        return r.top - colRect.top;
      });

      // also compute bottom of last slot so events that end at the final slot can reference it
      const lastSlotRect = slotEls[slotEls.length - 1].getBoundingClientRect();
      const colBottom = lastSlotRect.bottom - colRect.top + (parseFloat(getComputedStyle(slotEls[0]).borderBottomWidth || '0') || 0);

      // events in this column
      const colEvents = eventsByDay[colIdx] || [];
      for (const e of colEvents) {
        const startSlotIdx = Math.floor((toMinutes(e.startTime) - startMins) / slotMinutes);
        const maybeEndSlot = Math.ceil((toMinutes(e.endTime) - startMins) / slotMinutes);

        const startSlot = Math.max(0, Math.min(slotEls.length - 1, startSlotIdx));
        const endSlot = Math.max(0, Math.min(slotEls.length, maybeEndSlot)); // endSlot can equal slotEls.length (meaning after last slot)

        // compute top using slotTops[startSlot]
        const top = slotTops[startSlot] ?? 0;
        // compute bottom: if endSlot is inside slots -> slotTops[endSlot], else colBottom
        const bottom = (endSlot < slotTops.length) ? slotTops[endSlot] : colBottom;
        const height = Math.max(0, bottom - top);

        rects[e.eventId] = { top, height };
      }
    }

    return rects;
  };

  // compute rects after mount and whenever relevant inputs change
  useLayoutEffect(() => {
    // initial compute
    const update = () => {
      const r = computeEventRects();
      setEventRects(r);
    };

    update();

    // recompute on resize or fonts loading
    const ro = new ResizeObserver(() => update());
    if (columnContainerRef.current) ro.observe(columnContainerRef.current);

    window.addEventListener('resize', update);
    // also observe images or font load changes by re-running after a short delay
    const id = window.setTimeout(update, 50);

    return () => {
      ro.disconnect();
      window.removeEventListener('resize', update);
      clearTimeout(id);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [events, weekStartISO, startHour, endHour, slotMinutes]);

  return (
    <div className="cal-grid">
      <div className="cal-grid__header">
        <div className="cal-grid__corner" />
        {days.map((d) => {
          const y = Number(d.slice(0, 4));
          const m = Number(d.slice(5, 7)) - 1;
          const day = Number(d.slice(8, 10));
          const dateObj = new Date(y, m, day);
          const weekday = new Intl.DateTimeFormat('en-GB', { weekday: 'long' }).format(dateObj);
          const dayLabel = String(dateObj.getDate()).padStart(2, '0');
          return (
            <div className={`cal-grid__day${d === todayISO ? ' cal-grid__day--today' : ''}`} key={d}>
              <div className="cal-grid__dayname">{weekday} {dayLabel}</div>
            </div>
          );
        })}
      </div>

      <div className="cal-grid__body">
        <div className="cal-grid__times">
          {timeLabels.map((m) => (
            <div
              className="cal-grid__time"
              key={m}
            >
              {formatTimeLabel(m)}
            </div>
          ))}
        </div>

        <div className="cal-grid__days" ref={columnContainerRef}>
          {days.map((d, idx) => {
            const evs = eventsByDay[idx];
            const colClasses = `cal-grid__col${d === todayISO ? ' cal-grid__col--today' : ''}`;
            const dateObj = new Date(d + 'T00:00:00');
            const dayName = dayNamesMap[dateObj.getDay()];

            return (
              <div className={colClasses} key={d}>
                {/* slots background */}
                {timeLabels.map((m) => {
                  const slotStart = m;
                  const slotEnd = m + slotMinutes;
                  const isAvailable = isSlotAvailable(d, slotStart, slotEnd);

                  let slotClasses = 'cal-grid__slot';
                  if (!isAvailable) {
                    // Use different class based on mode
                    slotClasses += isAvailabilityMode
                      ? ' cal-grid__slot-worker--unavailable'
                      : ' cal-grid__slot--unavailable';
                  }

                  // Add interactive class if in availability mode
                  if (isAvailabilityMode) {
                    slotClasses += ' cal-grid__slot--interactive';
                  }

                  return (
                    <div
                      className={slotClasses}
                      key={m + d}
                      onClick={() => onSlotClick?.(d, slotStart, dayName)}
                    />
                  );
                })}

                {evs.map(e => {
                  const rect = eventRects[e.eventId];
                  const safeTop = rect ? rect.top : 0;
                  const safeHeight = rect ? Math.max(0, rect.height - 5) : 0;

                  return (
                    <div
                      key={e.eventId}
                      className={`cal-grid__event${isAvailabilityMode ? ' cal-grid__event--passthrough' : ''}`}
                      style={{ top: `${safeTop}px`, height: `${safeHeight}px` }}
                      onClick={() => onEdit?.(e)}
                      title={`${e.title} @ ${e.location}\n${e.startTime} - ${e.endTime}`}
                    >
                      <div className="cal-grid__event-title">{e.title}</div>
                      <div className="cal-grid__event-location">{e.location}</div>
                      <div className="cal-grid__event-meta">{e.startTime} - {e.endTime}</div>
                      <button className="cal-grid__event-edit" aria-label="Open event" onClick={(ev) => { ev.stopPropagation(); onEdit?.(e) }}></button>
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
