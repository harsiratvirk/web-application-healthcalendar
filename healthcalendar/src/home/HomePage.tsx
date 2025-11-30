import { Link } from 'react-router-dom'
import '../styles/HomePage.css'
import NavBar from '../shared/NavBar'

const HomePage: React.FC = () => {
  return (
    <div className="home-page">
      <NavBar />
      <main className="home-main">
        <section className="home-main__left">
          <h1 className="home-title">Book your next appointment with <span>HelseKalenderen</span></h1>
          <p className="home-subtitle">Your homecare made simple — book, track, and manage appointments with ease.</p>
          <Link to="/register" className="home-cta">Register now</Link>
        </section>
        <section className="home-main__right">
          <img src="/images/home.png" alt="Homecare illustration" className="home-image" />
        </section>
      </main>
    </div>
  )
}

export default HomePage
