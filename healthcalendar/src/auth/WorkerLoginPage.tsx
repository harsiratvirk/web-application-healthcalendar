import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import '../styles/WorkerLoginPage.css'
import NavBar from '../shared/NavBar'

const WorkerLoginPage: React.FC = () => {
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    if (!email || !password) {
      setError('Email and password are required.')
      return
    }
    try {
      setLoading(true)
      await new Promise(res => setTimeout(res, 400))
      navigate('/worker')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login failed, please retry.')
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
          {error && <div className="auth-banner auth-banner--error">{error}</div>}
          <form className="auth-form" onSubmit={onSubmit} noValidate>
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
              {loading ? 'Logging in…' : 'Log In'}
            </button>
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
