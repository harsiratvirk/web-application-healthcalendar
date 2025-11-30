//import { useState } from 'react'
//import reactLogo from './assets/react.svg'
//import viteLogo from '/vite.svg'
import HomePage from './home/HomePage'
import EventCalendarPage from './patientComponents/EventCalendarPage'
import WorkerCalendarPage from './workerComponents/WorkerCalendarPage'
import PatientLoginPage from './auth/PatientLoginPage'
import PatientRegistrationPage from './auth/PatientRegistrationPage'
import WorkerLoginPage from './auth/WorkerLoginPage'
import WorkerRegistrationPage from './admin/WorkerRegistrationPage'
import UserManagePage from './admin/UserManagePage'
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
                <EventCalendarPage />
              </ProtectedRoute>
            }
          />
          <Route
            path='/patient/EventCalendar'
            element={
              <ProtectedRoute allowedRoles={['Patient']} redirectPrefix='/patient'>
                <EventCalendarPage />
              </ProtectedRoute>
            }
          />
          {/* Back-compat alias for older path references */}
          <Route
            path='/patient/events'
            element={
              <ProtectedRoute allowedRoles={['Patient']} redirectPrefix='/patient'>
                <EventCalendarPage />
              </ProtectedRoute>
            }
          />
          <Route path='/patient/login' element={<PatientLoginPage />} />
          <Route path='/register' element={<PatientRegistrationPage />} />
          <Route path='/worker/login' element={<WorkerLoginPage />} />
          <Route
            path='/worker/WorkerCalendar'
            element={
              <ProtectedRoute allowedRoles={['Worker']} redirectPrefix='/worker'>
                <WorkerCalendar />
              </ProtectedRoute>
            }
          />
          {/* Alias to support existing links to /worker */}
          <Route
            path='/worker'
            element={
              <ProtectedRoute allowedRoles={['Worker']} redirectPrefix='/worker'>
                <WorkerCalendar />
              </ProtectedRoute>
            }
          />
          {/* Admin route: redirect to WorkerCalendar for admin */}
          <Route
            path='/admin/Dashboard'
            element={
              <ProtectedRoute allowedRoles={['Admin']} redirectPrefix='/worker'>
                <WorkerCalendar />
              </ProtectedRoute>
            }
          />
          {/* Admin routes for user management */}
          <Route
            path='/admin/register-worker'
            element={
              <ProtectedRoute allowedRoles={['Admin']} redirectPrefix='/worker'>
                <WorkerRegistrationPage />
              </ProtectedRoute>
            }
          />
          <Route
            path='/admin/manage'
            element={
              <ProtectedRoute allowedRoles={['Admin']} redirectPrefix='/worker'>
                <UserManagePage />
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
