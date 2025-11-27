//import { useState } from 'react'
//import reactLogo from './assets/react.svg'
//import viteLogo from '/vite.svg'
import HomePage from './home/HomePage'
import EventCalendar from './patientComponents/EventCalendar'
import WorkerCalendar from './workerComponents/WorkerCalendar'
import LoginPage from './auth/LoginPage'
import RegistrationPage from './auth/RegistrationPage'
import WorkerLoginPage from './auth/WorkerLoginPage'
import ProtectedRoute from './auth/ProtectedRoute'
import { AuthProvider } from './auth/AuthContext'
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom'
import './App.css'

const App: React.FC = () => {
  return (
    <Router>
      <AuthProvider>
        <Routes>
          <Route path='/' element={<HomePage />} />
          <Route
            path='/patient'
            element={
              <ProtectedRoute allowedRoles={['Patient']} redirectPrefix='/patient'>
                <EventCalendar />
              </ProtectedRoute>
            }
          />
          <Route
            path='/patient/EventCalendar'
            element={
              <ProtectedRoute allowedRoles={['Patient']} redirectPrefix='/patient'>
                <EventCalendar />
              </ProtectedRoute>
            }
          />
          {/* Back-compat alias for older path references */}
          <Route
            path='/patient/events'
            element={
              <ProtectedRoute allowedRoles={['Patient']} redirectPrefix='/patient'>
                <EventCalendar />
              </ProtectedRoute>
            }
          />
          <Route path='/patient/login' element={<LoginPage />} />
          <Route path='/register' element={<RegistrationPage />} />
          <Route path='/worker/login' element={<WorkerLoginPage />} />
          <Route
            path='/worker/WorkerCalendar'
            element={
              <ProtectedRoute allowedRoles={['Worker', 'Usermanager']} redirectPrefix='/worker'>
                <WorkerCalendar />
              </ProtectedRoute>
            }
          />
          {/* Alias to support existing links to /worker */}
          <Route
            path='/worker'
            element={
              <ProtectedRoute allowedRoles={['Worker', 'Usermanager']} redirectPrefix='/worker'>
                <WorkerCalendar />
              </ProtectedRoute>
            }
          />
          {/* Usermanager route: redirect to WorkerCalendar for usermanager */}
          <Route
            path='/usermanager/Dashboard'
            element={
              <ProtectedRoute allowedRoles={['Usermanager']} redirectPrefix='/worker'>
                <WorkerCalendar />
              </ProtectedRoute>
            }
          />
          <Route path='*' element={<HomePage />} />
        </Routes>
      </AuthProvider>
    </Router>
  )
}

export default App
