import { Link } from 'react-router-dom'
import '../styles/NavBar.css'

const NavBar: React.FC = () => {
  return (
    <header className="navbar">
      <div className="navbar__left">
        <Link to="/" className="navbar__brand">
          <img src="/logo/logo.png" alt="HelseKalenderen logo" className="navbar__logo" />
          <span className="navbar__title">HelseKalenderen</span>
        </Link>
      </div>
      <nav className="navbar__right">
        <Link to="/login" className="navbar__link">Private Person</Link>
        <Link to="/worker/login" className="navbar__link navbar__link--primary">Health Personnel</Link>
      </nav>
    </header>
  )
}

export default NavBar
