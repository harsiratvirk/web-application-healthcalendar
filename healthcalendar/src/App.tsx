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
          {/* EventCalendar path for patient calendar */}
          <Route
            path='/patient/EventCalendar'
            element={
              <ProtectedRoute allowedRoles={['Patient']} redirectPrefix='/patient'>
                <EventCalendarPage />
              </ProtectedRoute>
            }
          />
          
          {/* Public authentication routes - no login required */}
          {/* Patient login page */}
          <Route path='/patient/login' element={<PatientLoginPage />} />
          {/* Patient self-registration page */}
          <Route path='/register' element={<PatientRegistrationPage />} />
          {/* Worker and Admin login page */}
          <Route path='/worker/login' element={<WorkerLoginPage />} />
          
          {/* Worker routes - require Worker or Admin role */}
          {/* Worker calendar shows availability and patient events */}
          <Route
            path='/worker/WorkerCalendar'
            element={
              <ProtectedRoute allowedRoles={['Worker']} redirectPrefix='/worker'>
                <WorkerCalendarPage />
              </ProtectedRoute>
            }
          />
          {/* Alias to support existing links to /worker */}
          <Route
            path='/worker'
            element={
              <ProtectedRoute allowedRoles={['Worker']} redirectPrefix='/worker'>
                <WorkerCalendarPage />
              </ProtectedRoute>
            }
          />
          {/* Admin route: redirect to WorkerCalendar for admin */}
          <Route
            path='/admin/Dashboard'
            element={
              <ProtectedRoute allowedRoles={['Admin']} redirectPrefix='/worker'>
                <WorkerCalendarPage />
              </ProtectedRoute>
            }
          />
          
          {/* Admin routes for user management - require Admin role */}
          {/* Register new healthcare workers */}
          <Route
            path='/admin/register-worker'
            element={
              <ProtectedRoute allowedRoles={['Admin']} redirectPrefix='/worker'>
                <WorkerRegistrationPage />
              </ProtectedRoute>
            }
          />
          {/* Manage users, assign patients to workers */}
          <Route
            path='/admin/manage'
            element={
              <ProtectedRoute allowedRoles={['Admin']} redirectPrefix='/worker'>
                <UserManagePage />
              </ProtectedRoute>
            }
          />
          
          {/* Fallback route - any unmatched paths redirect to home page */}
          <Route path='*' element={<HomePage />} />
        </Routes>
      </AuthProvider>
    </Router>
  )
}

export default App
