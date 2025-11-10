import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import '../styles/RegistrationPage.css'
import NavBar from '../shared/NavBar'
import { useAuth } from './AuthContext'
import { registerPatient as apiRegister } from './AuthService'

const RegistrationPage: React.FC = () => {
  const navigate = useNavigate()
  const { login } = useAuth()
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [nameError, setNameError] = useState<string | null>(null)
  const [emailError, setEmailError] = useState<string | null>(null)
  const [passwordError, setPasswordError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setNameError(null)
    setEmailError(null)
    setPasswordError(null)
    let hasError = false
    if (!name) { setNameError('Name is required.'); hasError = true }
    if (!email) { setEmailError('Email is required.'); hasError = true }
  else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) { setEmailError('Enter a valid email address (e.g., name@example.com).'); hasError = true }
    if (!password) { setPasswordError('Password is required.'); hasError = true }
  else if (password.length < 6) { setPasswordError('Password must be at least 6 characters.'); hasError = true }
    if (hasError) return
    try {
      setLoading(true)
  await apiRegister({ name, email, password })
      // Optionally auto-login after registration
      await login({ email, password })
      navigate('/patient/events')
    } catch (err) {
      console.debug('Registration failed', err)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="auth-page">
      <NavBar />
      <main className="auth-main">
        <section className="auth-left">
          <img src="/images/register_login.png" alt="Register" className="auth-image" />
        </section>
        <section className="auth-right">
          <h1 className="auth-title auth-title--nowrap">Create your account</h1>
          <form className="auth-form" onSubmit={onSubmit} noValidate>
            <label>
              Name
              <input
                type="text"
                placeholder="Your name here…"
                value={name}
                onChange={e => {
                  const v = e.target.value
                  setName(v)
                  if (nameError && v.trim()) setNameError(null)
                }}
                className="auth-input"
                aria-invalid={!!nameError}
                required
              />
              {nameError && <small className="field-error">{nameError}</small>}
            </label>
            <label>
              Email
              <input
                type="email"
                placeholder="Your email here…"
                value={email}
                onChange={e => {
                  const v = e.target.value
                  setEmail(v)
                  const patternOk = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v)
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
            <label>
              Password
              <input
                type="password"
                placeholder="Your password here…"
                value={password}
                onChange={e => {
                  const v = e.target.value
                  setPassword(v)
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
            <button className="auth-btn" type="submit" disabled={loading}>Sign Up</button>
          </form>
          <p className="auth-alt">
            Have an account? <Link to="/login">Log in here</Link>
          </p>
        </section>
      </main>
    </div>
  )
}

export default RegistrationPage
