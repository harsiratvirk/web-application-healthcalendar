import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import '../styles/LoginPage.css'
import NavBar from '../shared/NavBar'

const LoginPage: React.FC = () => {
	const navigate = useNavigate()
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
			// Mock async login
			await new Promise(res => setTimeout(res, 400))
			// Navigate to patient events view
			navigate('/patient/events')
		} catch (err) {
			// Keep silent (no top banner); could show a generic small message near button if desired
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
									// Clear error immediately once valid
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
						Don’t have an account? <Link to="/register">Register here</Link>
					</p>
				</section>
			</main>
		</div>
	)
}

export default LoginPage
