import { useCallback, useMemo, useRef, useState } from 'react'
import '../styles/Toast.css'
import { ToastContext } from './toastContext'
import type { ToastContextType } from './toastContext'
// Keeping context + hook in same file for simplicity; react-refresh warning will be ignored for now.

type Toast = { id: number; type: 'success' | 'error' | 'info'; message: string }

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([])
  const idRef = useRef(1)

  const remove = useCallback((id: number) => {
    setToasts(ts => ts.filter(t => t.id !== id))
  }, [])

  const push = useCallback((type: Toast['type'], message: string) => {
    const id = idRef.current++
    setToasts(ts => [...ts, { id, type, message }])
    window.setTimeout(() => remove(id), 2500)
  }, [remove])

  const ctx = useMemo<ToastContextType>(() => ({
    showSuccess: (m: string) => push('success', m),
    showError: (m: string) => push('error', m),
    showInfo: (m: string) => push('info', m),
  }), [push])

  return (
    <ToastContext.Provider value={ctx}>
      {children}
      <div className="toast-region" role="status" aria-live="polite" aria-atomic="true">
        {toasts.map(t => (
          <div key={t.id} className={`toast toast--${t.type}`}>
            {t.message}
            <button className="toast__close" onClick={() => remove(t.id)} aria-label="Dismiss">×</button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  )
}

// useToast hook is exported from toastContext.ts
