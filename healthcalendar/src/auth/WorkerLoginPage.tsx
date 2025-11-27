import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from './AuthContext'
import '../styles/WorkerLoginPage.css'
import NavBar from '../shared/NavBar'

const WorkerLoginPage: React.FC = () => {
  const navigate = useNavigate()
  const { loginWorker } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [emailError, setEmailError] = useState<string | null>(null)
  const [passwordError, setPasswordError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setEmailError(null)
    setPasswordError(null)
    let hasError = false
    if (!email) { setEmailError('Email is required.'); hasError = true }
    if (!password) { setPasswordError('Password is required.'); hasError = true }
    if (hasError) return
    try {
      setLoading(true)
      setFormError(null)
      const decoded = await loginWorker({ email, password })
      const role = decoded?.role
  if (role === 'Usermanager') navigate('/worker/WorkerCalendar', { replace: true })
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
        <section className="auth-left">
          <img src="/images/register_login.png" alt="Worker Login" className="auth-image" />
        </section>
        <section className="auth-right">
          <h1 className="auth-title">Personnel Login</h1>
          {formError && <div role="alert" className="form-error-banner">{formError}</div>}
          <form className="auth-form" onSubmit={onSubmit} noValidate>
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
            <button className="auth-btn" type="submit" disabled={loading}>Log In</button>
          </form>
          <p className="auth-alt">
            Are you a private person? <Link to="/patient/login">Log in here</Link>
          </p>
        </section>
      </main>
    </div>
  )
}

export default WorkerLoginPage