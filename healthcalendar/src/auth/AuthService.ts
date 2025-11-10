/*
  AuthService (human friendly explanation)
  ---------------------------------------
  Tiny wrapper around fetch for auth endpoints.

  How URLs are chosen:
  - we read VITE_API_URL directly and use it.
    Example: const API_URL = import.meta.env.VITE_API_URL
    (We assume it's set; keep your .env.development in sync.)

  Contracts:
  - login({ email, password }) → resolves { token }
  - registerPatient({...}) → resolves void on success; throws Error on failure
  - logout(token?) → best-effort POST; errors are swallowed (UI state clears token anyway)

  If requests fail:
  - We throw simple Error objects with short messages. The caller (UI) decides
    how to show them.
*/

import type { LoginInput, RegisterInput } from '../types/auth'

// read API URL directly from env and compose paths
const API_URL = (import.meta as any).env?.VITE_API_URL as string
const API_BASE = `${API_URL}/api/auth`

export async function login(credentials: LoginInput): Promise<{ token: string }> {
  // POST /api/auth/login with JSON body and return { token }
  const response = await fetch(`${API_BASE}/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(credentials)
  })
  if (!response.ok) {
    throw new Error('Login failed')
  }
  return response.json() // expects { Token: string }
    .then(data => ({ token: (data as any).Token }))
}

export async function registerPatient(userData: RegisterInput): Promise<void> {
  // POST /api/auth/registerPatient; throw with readable message when possible
  const response = await fetch(`${API_BASE}/registerPatient`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(userData)
  })
  if (!response.ok) {
    // Format ASP.NET Identity style errors (array of { description })
    try {
      const errorData = await response.json()
      if (Array.isArray(errorData)) {
        const msg = errorData.map((e: any) => e?.description).filter(Boolean).join(', ')
        throw new Error(msg || 'Registration failed')
      }
      if (errorData?.message || errorData?.Message) {
        throw new Error(errorData.message || errorData.Message)
      }
    } catch {
      /* ignore parse errors */
    }
    throw new Error('Registration failed')
  }
  // Ignore body; backend returns simple success message
}

// Logout handled client-side by clearing token; endpoint call optional.
export async function logout(token?: string): Promise<void> {
  // If a token is provided, attempt to notify the backend. UI will clear token regardless.
  if (!token) return
  try {
    await fetch(`${API_BASE}/logout`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` }
    })
  } catch { /* network errors */ }
}
