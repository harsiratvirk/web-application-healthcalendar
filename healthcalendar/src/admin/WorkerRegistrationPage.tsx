import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { userService } from '../services/userService'
import { useToast } from '../shared/toastContext'
import { useAuth } from '../auth/AuthContext'
import '../styles/PatientRegistrationPage.css'
import '../styles/UserManagement.css'
import '../styles/EventCalendarPage.css'

// Admin page for registering new healthcare workers

const WorkerRegistrationPage: React.FC = () => {
  const navigate = useNavigate()
  const { showSuccess, showError } = useToast()
  const { logout } = useAuth()
  
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  
  // Validation error state for each field
  const [nameError, setNameError] = useState<string | null>(null)
  const [emailError, setEmailError] = useState<string | null>(null)
  const [passwordError, setPasswordError] = useState<string | null>(null)
  
  // UI state
  const [loading, setLoading] = useState(false)
  const [showLogoutConfirm, setShowLogoutConfirm] = useState(false)

  // Handle form submission and worker registration
  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    setNameError(null)
    setEmailError(null)
    setPasswordError(null)
    let hasError = false
    
    // Client-side validation
    if (!name) { setNameError('Name is required.'); hasError = true }
    if (!email) { setEmailError('Email is required.'); hasError = true }
    else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) { 
      setEmailError('Enter a valid email address (e.g., name@example.com).'); 
      hasError = true 
    }
    if (!password) { setPasswordError('Password is required.'); hasError = true }
    else if (password.length < 6) { 
      setPasswordError('Password must be at least 6 characters long.'); 
      hasError = true 
    }
    
    // Attempt registration if email format is valid (backend will check for duplicates)
    const emailIsValid = email && /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)
    if (emailIsValid) {
      try {
        setLoading(true)
        await userService.registerWorker({ Name: name, Email: email, Password: password })
        if (!hasError) {
          showSuccess('Healthcare worker registered successfully!')
          setName('')
          setEmail('')
          setPassword('')
        }
      } catch (err: any) {
        console.debug('Worker registration failed', err)
        const errorMessage = err?.message || ''
        if (errorMessage.includes('DuplicateUserName') || errorMessage.includes('already taken')) {
          setEmailError('This email is already in use.')
        }
      } finally {
        setLoading(false)
      }
    }
  }

  return (
    <div className="auth-page">
      <div className="admin-logout-header">
        <button
          className="logout-btn"
          onClick={() => setShowLogoutConfirm(true)}
        >
          <img src="/images/logout.png" alt="Logout" />
          <span>Log Out</span>
        </button>
      </div>
      <main className="admin-form-container">
        <section className="admin-form-content">
          <h1 className="auth-title">Register Healthcare Worker</h1>
          <form className="auth-form" onSubmit={onSubmit} noValidate>
            {/* Name input field */}
            <label>
              Name
              <input
                type="text"
                placeholder="Worker name here…"
                value={name}
                onChange={e => {
                  const v = e.target.value
                  setName(v)
                  // Clear error when user starts typing a valid value
                  if (nameError && v.trim()) setNameError(null)
                }}
                className="auth-input"
                aria-invalid={!!nameError}
                required
              />
              {nameError && <small className="field-error">{nameError}</small>}
            </label>
            
            {/* Email input field with format validation */}
            <label>
              Email
              <input
                type="email"
                placeholder="Worker email here…"
                value={email}
                onChange={e => {
                  const v = e.target.value
                  setEmail(v)
                  const patternOk = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v)
                  // Clear error when user enters a valid email format
                  if (emailError) {
                    if (v.trim() && patternOk) setEmailError(null)
                  }
                }}
                className="auth-input"
                aria-invalid={!!emailError}
                required
              />
              {emailError && <small className="field-error">{emailError}</small>}
            </label>
            
            {/* Password input field with minimum length requirement */}
            <label>
              Password
              <input
                type="password"
                placeholder="Worker password here…"
                value={password}
                onChange={e => {
                  const v = e.target.value
                  setPassword(v)
                  // Clear error when password meets minimum length
                  if (passwordError) {
                    if (v.length >= 6) setPasswordError(null)
                  }
                }}
                className="auth-input"
                aria-invalid={!!passwordError}
                required
                minLength={6}
              />
              {passwordError && <small className="field-error">{passwordError}</small>}
            </label>
            <button className="auth-btn" type="submit" disabled={loading}>
              Register Worker
            </button>
          </form>
          <p className="auth-alt">
            <button 
              type="button" 
              onClick={() => navigate('/admin/manage')} 
              className="admin-link-button"
            >
              Go to Manage Workers & Patients
            </button>
          </p>
        </section>
      </main>

      {/* Logout confirmation modal */}
      {showLogoutConfirm && (
        <div className="overlay" role="dialog" aria-modal="true" aria-labelledby="logout-confirm-title" aria-describedby="logout-confirm-desc">
          <div className="modal confirm-modal">
            <header className="modal__header">
              <h2 id="logout-confirm-title">Confirm Logout</h2>
              <button className="icon-btn" onClick={() => setShowLogoutConfirm(false)} aria-label="Close confirmation">
                <img src="/images/exit.png" alt="Close" />
              </button>
            </header>
            <div id="logout-confirm-desc" className="confirm-body">
              Are you sure you want to log out?
            </div>
            <div className="confirm-actions">
              <button type="button" className="btn" onClick={() => setShowLogoutConfirm(false)}>Cancel</button>
              <button 
                type="button" 
                className="btn btn--primary" 
                onClick={() => {
                  logout();
                  window.location.href = '/';
                }}
              >
                Confirm
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

export default WorkerRegistrationPage
