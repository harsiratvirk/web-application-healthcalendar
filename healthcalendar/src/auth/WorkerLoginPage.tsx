import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import '../styles/WorkerLoginPage.css'
import NavBar from '../shared/NavBar'
import { useAuth } from './AuthContext'

const WorkerLoginPage: React.FC = () => {
  const navigate = useNavigate()
  const { login } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [emailError, setEmailError] = useState<string | null>(null)
  const [passwordError, setPasswordError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

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
  await login({ email, password })
  navigate('/worker')
    } catch (err) {
      console.debug('Invalid email or password', err)
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
            Are you a private person? <Link to="/login">Log in here</Link>
          </p>
        </section>
      </main>
    </div>
  )
}

export default WorkerLoginPage
