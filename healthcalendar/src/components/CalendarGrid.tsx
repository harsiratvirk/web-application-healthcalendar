import React, { useRef, useState, useEffect, useLayoutEffect } from 'react';
import type { Event } from '../types/event';
import '../styles/CalendarGrid.css';

export type CalendarGridProps = {
  events: Event[]
  weekStartISO: string // Monday of the week in YYYY-MM-DD
  startHour?: number // default 8
  endHour?: number // default 20
  slotMinutes?: number // default 30
  onEdit?: (e: Event) => void
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
  weekStartISO,
  startHour = 8,
  endHour = 20,
  slotMinutes = 30,
  onEdit
}: CalendarGridProps) {
  const startMins = startHour * 60;
  const endMins = endHour * 60;
  const totalSlots = Math.floor((endMins - startMins) / slotMinutes);

  // time labels (slot top boundaries)
  const timeLabels = Array.from({ length: totalSlots + 1 }, (_, i) => startMins + i * slotMinutes);

  const days = Array.from({ length: 7 }, (_, i) => addDays(weekStartISO, i));
  const now = new Date();
  const todayISO = toLocalISO(new Date(now.getFullYear(), now.getMonth(), now.getDate()));

  // refs
  const columnContainerRef = useRef<HTMLDivElement | null>(null);
  const firstSlotRef = useRef<HTMLDivElement | null>(null);

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

        <div className="cal-grid__days" ref={columnContainerRef}>
          {days.map((d, idx) => {
            const evs = eventsByDay[idx];
            return (
              <div className={`cal-grid__col${d === todayISO ? ' cal-grid__col--today' : ''}`} key={d}>
                {/* slots background */}
                {timeLabels.map((m, si) => (
                  <div className="cal-grid__slot" key={m + d} />
                ))}
                {/* events */}
                {evs.map(e => {
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
                      <div className="cal-grid__event-location">{e.location}</div>
                      <div className="cal-grid__event-meta">{e.startTime} - {e.endTime}</div>
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
