import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from './AuthContext'
import '../styles/WorkerLoginPage.css'
import NavBar from '../shared/NavBar'

// Worker/Usermanager login page for authenticating healthcare personnel

const WorkerLoginPage: React.FC = () => {
  const navigate = useNavigate()
  const { loginWorker } = useAuth()
  
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  
  // Validation error state for each field
  const [emailError, setEmailError] = useState<string | null>(null)
  const [passwordError, setPasswordError] = useState<string | null>(null)
  
  const [loading, setLoading] = useState(false)
  
  // Form-level error state for authentication failures
  const [formError, setFormError] = useState<string | null>(null)

  // Handle form submission and worker/usermanager authentication
  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    // Clear previous validation errors
    setEmailError(null)
    setPasswordError(null)
    let hasError = false
    
    // Client-side validation
    if (!email) { setEmailError('Email is required.'); hasError = true }
    if (!password) { setPasswordError('Password is required.'); hasError = true }
    
    // Stop submission if validation errors exist
    if (hasError) return
    
    try {
      setLoading(true)
      setFormError(null)
      
      // Authenticate using worker login endpoint
      const decoded = await loginWorker({ email, password })
      
      // Route user based on their role (handles Worker, Usermanager, or wrong login form)
      const role = decoded?.role
  if (role === 'Admin') navigate('/admin/manage', { replace: true })
  else if (role === 'Worker') navigate('/worker/WorkerCalendar', { replace: true })
      else if (role === 'Patient') navigate('/patient/EventCalendar', { replace: true })
      else navigate('/worker/login', { replace: true })
    } catch (err: any) {
      console.debug('Worker login failed', err)
      setFormError(err?.message || 'Invalid email or password.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="auth-page">
      <NavBar />
      <main className="auth-main">
        {/* Left side with image */}
        <section className="auth-left">
          <img src="/images/register_login.png" alt="Worker Login" className="auth-image" />
        </section>
        {/* Right side with login form */}
        <section className="auth-right">
          <h1 className="auth-title">Personnel Login</h1>
          {/* Display form-level errors (authentication failures) */}
          {formError && <div role="alert" className="form-error-banner">{formError}</div>}
          <form className="auth-form" onSubmit={onSubmit} noValidate>
            {/* Email input field */}
            <label>
              Email
              <input
                type="email"
                placeholder="Your email here…"
                value={email}
                onChange={e => {
                  const v = e.target.value
                  setEmail(v)
                  if (emailError && v.trim()) setEmailError(null)
                }}
                className="auth-input"
                aria-invalid={!!emailError}
                required
              />
              {emailError && <small className="field-error">{emailError}</small>}
            </label>
            {/* Password input field */}
            <label>
              Password
              <input
                type="password"
                placeholder="Your password here…"
                value={password}
                onChange={e => {
                  const v = e.target.value
                  setPassword(v)
                  if (passwordError && v.trim()) setPasswordError(null)
                }}
                className="auth-input"
                aria-invalid={!!passwordError}
                required
                minLength={6}
              />
              {passwordError && <small className="field-error">{passwordError}</small>}
            </label>
            {/* Submit button for login */}
            <button className="auth-btn" type="submit" disabled={loading}>Log In</button>
          </form>
          {/* Link to patient login page */}
          <p className="auth-alt">
            Are you a private person? <Link to="/patient/login">Log in here</Link>
          </p>
        </section>
      </main>
    </div>
  )
}

export default WorkerLoginPage