import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from './AuthContext'
import '../styles/PatientLoginPage.css'
import NavBar from '../shared/NavBar'

// Patient login page for authenticating patients

const PatientLoginPage: React.FC = () => {
	const navigate = useNavigate()
	const { loginPatient } = useAuth()
	
	const [email, setEmail] = useState('')
	const [password, setPassword] = useState('')
	
	// Validation error state for each field
	const [emailError, setEmailError] = useState<string | null>(null)
	const [passwordError, setPasswordError] = useState<string | null>(null)
	const [formError, setFormError] = useState<string | null>(null)
	
	const [loading, setLoading] = useState(false)

	// Handle form submission and patient authentication
	const onSubmit = async (e: React.FormEvent) => {
		e.preventDefault()
		
		// Clear previous errors
		setEmailError(null)
		setPasswordError(null)
		let hasError = false
		
		// Client-side validation
		if (!email) { setEmailError('Email is required.'); hasError = true }
		if (!password) { setPasswordError('Password is required.'); hasError = true }
		if (hasError) return
			try {
				setLoading(true)
				setFormError(null)
				const decoded = await loginPatient({ email, password })
				// After login, route based on role (Patient -> patient/events; Worker/Admin -> worker calendar in case user misused form)
				const role = decoded?.role
				if (role === 'Patient') {
					navigate('/patient/EventCalendar', { replace: true })
				} else if (role === 'Admin') {
					navigate('/worker/WorkerCalendar', { replace: true })
				} else if (role === 'Worker') {
					navigate('/worker/WorkerCalendar', { replace: true })
				} else {
					// Fallback if role missing
					navigate('/patient/login', { replace: true })
				}
			} catch (err: any) {
				console.debug('Login failed', err)
				setFormError(err?.message || 'Invalid email or password.')
			} finally {
			setLoading(false)
		}
	}

	return (
		<div className="auth-page">
			<NavBar />
			<main className="auth-main">
				{/* Left side with decorative image */}
				<section className="auth-left">
					<img src="/images/register_login.png" alt="Login" className="auth-image" />
				</section>
				{/* Right side with login form */}
				<section className="auth-right">
					<h1 className="auth-title">Welcome back</h1>
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
								// Clear error when user starts typing valid input
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
								// Clear error when user starts typing
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
				{/* Link to registration page */}
				<p className="auth-alt">
					Don't have an account? <Link to="/register">Register here</Link>
				</p>
				</section>
			</main>
		</div>
	)
}

export default PatientLoginPage