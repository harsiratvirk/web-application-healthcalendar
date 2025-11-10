//import { useState } from 'react'
//import reactLogo from './assets/react.svg'
//import viteLogo from '/vite.svg'
import HomePage from './home/HomePage'
import EventCalendar from './patientComponents/EventCalendar'
import LoginPage from './auth/LoginPage'
import RegistrationPage from './auth/RegistrationPage.tsx'
import WorkerLoginPage from './auth/WorkerLoginPage.tsx'
import { AuthProvider, RequireAuth } from './auth/AuthContext.tsx'
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom'
import './App.css'

const App: React.FC = () => {
  return (
    <Router>
      <AuthProvider>
        <Routes>
          <Route path='/' element={<HomePage />} />
          <Route path='/patient/events' element={<RequireAuth roles={['Patient']} redirectTo='/login'><EventCalendar /></RequireAuth>} />
          <Route path='/login' element={<LoginPage />} />
          <Route path='/register' element={<RegistrationPage />} />
          <Route path='/worker/login' element={<WorkerLoginPage />} />
          {/* Worker dashboard placeholder protected route (future) */}
          <Route path='/worker' element={<RequireAuth roles={['Worker']} redirectTo='/worker/login'><div>Worker Dashboard (TODO)</div></RequireAuth>} />
          <Route path='*' element={<HomePage />} />
        </Routes>
      </AuthProvider>
    </Router>
  )
}

export default App
