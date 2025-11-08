import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import '../styles/LoginPage.css'
import NavBar from '../shared/NavBar'

const LoginPage: React.FC = () => {
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
			// Mock async login
			await new Promise(res => setTimeout(res, 400))
			// Navigate to patient events view for MVP
			navigate('/patient/events')
		} catch (err) {
			setError(err instanceof Error ? err.message : 'Login failed, please try again.')
		} finally {
			setLoading(false)
		}
	}

	return (
		<div className="auth-page">
			<NavBar />
			<main className="auth-main">
				<section className="auth-left">
					<img src="/images/register_login.png" alt="Login" className="auth-image" />
				</section>
				<section className="auth-right">
					<h1 className="auth-title">Welcome back</h1>
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
						Don’t have an account? <Link to="/register">Register here</Link>
					</p>
				</section>
			</main>
		</div>
	)
}

export default LoginPage
