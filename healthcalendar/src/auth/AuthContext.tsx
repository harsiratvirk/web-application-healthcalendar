/*
  AuthContext
  ---------------------------------------
  This file handles "Am I logged in?" logic for the app

  What it does:
  1. Stores a JWT token in localStorage under the key 'token'.
  2. Decodes that token to get simple user info (email, role, etc.).
  3. Automatically logs the user out if the token is missing or expired.
  4. Gives you easy functions: login() and logout().
  5. Lets you wrap pages in <RequireAuth> so only logged-in users (optionally with a role) can see them..

  If something breaks:
  - Open dev tools > Application > Local Storage and clear the 'token' key.
  - Then log in again.
*/

/* eslint-disable react-refresh/only-export-components */
import React, { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { login as apiLogin, logout as apiLogout } from './AuthService'
import type { LoginInput } from '../types/auth'
import type { User } from '../types/user'
import { jwtDecode } from 'jwt-decode'

// Single place we define the key name used in localStorage.
const TOKEN_KEY = 'token'

// Everything components can read/use from useAuth().
type AuthContextValue = {
  token: string | null
  user: User | null
  isAuthenticated: boolean
  isLoading: boolean
  login: (input: LoginInput) => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

// Turn a raw JWT string into a User object.
// Returns null if: token is invalid JSON, can't decode, or has passed exp time.
function decodeTokenToUser(token: string): User | null {
  try {
    const dec: any = jwtDecode(token)
    const expSec: number | undefined = dec['exp']
    if (typeof expSec === 'number' && expSec * 1000 <= Date.now()) return null
    return dec as User
  } catch {
    return null
  }
}

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  // At startup, try to grab an existing token (user may already be logged in).
  const [token, setToken] = useState<string | null>(() => (typeof localStorage !== 'undefined' ? localStorage.getItem(TOKEN_KEY) : null))
  const [user, setUser] = useState<User | null>(() => (token ? decodeTokenToUser(token) : null))
  const [isLoading, setIsLoading] = useState<boolean>(true)

  useEffect(() => {
  // Whenever the token value changes we decode it again.
    setIsLoading(true)
    const u = token ? decodeTokenToUser(token) : null
    if (!u) {
  // Token invalid or expired: treat user as logged out.
      localStorage.removeItem(TOKEN_KEY)
      setToken(null)
      setUser(null)
    } else {
      setUser(u)
    }
    setIsLoading(false)
  }, [token])

  const isAuthenticated = !!token && !!user

  const login = useCallback(async (input: LoginInput) => {
  // 1. Ask backend for a token.
  // 2. Save it to localStorage.
  // 3. Setting token triggers the effect above which decodes it.
    const { token } = await apiLogin(input)
    localStorage.setItem(TOKEN_KEY, token)
    setToken(token)
  }, [])

  const logout = useCallback(async () => {
  // Tell backend we want to log out (if it cares) then clear all local auth state.
    await apiLogout(token ?? undefined)
    localStorage.removeItem(TOKEN_KEY)
    setToken(null)
    setUser(null)
  }, [token])

  const value = useMemo<AuthContextValue>(() => ({
    token,
    user,
    isAuthenticated,
    isLoading,
    login,
    logout
  }), [token, user, isAuthenticated, isLoading, login, logout])

  return <AuthContext.Provider value={value}>{!isLoading && children}</AuthContext.Provider>
}

export const useAuth = (): AuthContextValue => {
  // Small helper so components can just call useAuth().
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}

export const RequireAuth: React.FC<{ roles?: string[]; redirectTo?: string; children: React.ReactNode }>
  = ({ roles, redirectTo = '/login', children }) => {
    const { isAuthenticated, user } = useAuth()
    const location = useLocation()
    // Not logged in? Redirect to login and remember where the user tried to go.
    if (!isAuthenticated) return <Navigate to={redirectTo} replace state={{ from: location }} />
    if (roles && user) {
      // If roles were specified, make sure the user has one of them.
      const role = (user as any).role
      if (!role || !roles.includes(role)) return <Navigate to={redirectTo} replace state={{ from: location }} />
    }
    return <>{children}</>
}
