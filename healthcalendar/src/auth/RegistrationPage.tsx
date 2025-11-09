import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import '../styles/RegistrationPage.css'
import NavBar from '../shared/NavBar'

const RegistrationPage: React.FC = () => {
  const navigate = useNavigate()
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    if (!name || !email || !password) {
      setError('All fields are required.')
      return
    }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
      setError('Please enter a valid email address.')
      return
    }
    if (password.length < 6) {
      setError('Password must be at least 6 characters.')
      return
    }
    try {
      setLoading(true)
      // Mock async sign up
      await new Promise(res => setTimeout(res, 500))
      navigate('/patient/events')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Registration failed, please try again.')
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
          {error && <div className="auth-banner auth-banner--error">{error}</div>}
          <form className="auth-form" onSubmit={onSubmit} noValidate>
            <label>
              Name
              <input
                type="text"
                placeholder="Your name here…"
                value={name}
                onChange={e => setName(e.target.value)}
                className="auth-input"
                required
              />
            </label>
            <label>
              Email
              <input
                type="email"
                placeholder="Your email here…"
                value={email}
                onChange={e => setEmail(e.target.value)}
                className="auth-input"
                required
              />
            </label>
            <label>
              Password
              <input
                type="password"
                placeholder="Your password here…"
                value={password}
                onChange={e => setPassword(e.target.value)}
                className="auth-input"
                required
                minLength={6}
              />
            </label>
            <button className="auth-btn" type="submit" disabled={loading}>
              {loading ? 'Signing up…' : 'Sign Up'}
            </button>
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
