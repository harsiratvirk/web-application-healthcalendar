import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { userService, type UserDTO } from '../services/userService'
import { useToast } from '../shared/toastContext'
import { useAuth } from '../auth/AuthContext'
import '../styles/ManageHealthcareWorkers.css'
import '../styles/EventCalendar.css'

const UserManagePage: React.FC = () => {
  const navigate = useNavigate()
  const { showSuccess, showError } = useToast()
  const { user, logout } = useAuth()
  
  const [workers, setWorkers] = useState<UserDTO[]>([])
  const [selectedWorker, setSelectedWorker] = useState<UserDTO | null>(null)
  const [assignedPatients, setAssignedPatients] = useState<UserDTO[]>([])
  const [unassignedPatients, setUnassignedPatients] = useState<UserDTO[]>([])
  const [selectedPatientIds, setSelectedPatientIds] = useState<string[]>([])
  const [loading, setLoading] = useState(false)
  const [showLogoutConfirm, setShowLogoutConfirm] = useState(false)
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false)
  const [workerToDelete, setWorkerToDelete] = useState<UserDTO | null>(null)

  // Load all workers on mount
  useEffect(() => {
    loadWorkers()
    loadUnassignedPatients()
  }, [])

  // Load assigned patients when worker changes
  useEffect(() => {
    if (selectedWorker) {
      loadAssignedPatients(selectedWorker.Id)
    } else {
      setAssignedPatients([])
    }
  }, [selectedWorker])

  const loadWorkers = async () => {
    try {
      const data = await userService.getAllWorkers()
      setWorkers(data)
    } catch (err: any) {
      showError(err?.message || 'Failed to load healthcare workers')
    }
  }

  const loadUnassignedPatients = async () => {
    try {
      const data = await userService.getUnassignedPatients()
      setUnassignedPatients(data)
    } catch (err: any) {
      showError(err?.message || 'Failed to load unassigned patients')
    }
  }

  const loadAssignedPatients = async (workerId: string) => {
    try {
      const data = await userService.getUsersByWorkerId(workerId)
      setAssignedPatients(data)
    } catch (err: any) {
      showError(err?.message || 'Failed to load assigned patients')
    }
  }

  const handleWorkerChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const workerId = e.target.value
    const worker = workers.find(w => w.Id === workerId)
    setSelectedWorker(worker || null)
    setSelectedPatientIds([])
  }

  const handlePatientToggle = (patientId: string) => {
    setSelectedPatientIds(prev => {
      if (prev.includes(patientId)) {
        return prev.filter(id => id !== patientId)
      } else {
        return [...prev, patientId]
      }
    })
  }

  const handleAssignPatients = async () => {
    if (!selectedWorker || selectedPatientIds.length === 0) {
      showError('Please select a worker and at least one patient')
      return
    }

    try {
      setLoading(true)
      await userService.assignPatientsToWorker(selectedPatientIds, selectedWorker.UserName)
      showSuccess(`Assigned ${selectedPatientIds.length} patient(s) to ${selectedWorker.Name}`)
      
      // Refresh lists
      await loadAssignedPatients(selectedWorker.Id)
      await loadUnassignedPatients()
      setSelectedPatientIds([])
    } catch (err: any) {
      showError(err?.message || 'Failed to assign patients')
    } finally {
      setLoading(false)
    }
  }

  const handleUnassignPatient = async (patientId: string) => {
    try {
      setLoading(true)
      await userService.unassignPatientFromWorker(patientId)
      showSuccess('Patient unassigned successfully')
      
      // Refresh lists
      if (selectedWorker) {
        await loadAssignedPatients(selectedWorker.Id)
      }
      await loadUnassignedPatients()
    } catch (err: any) {
      showError(err?.message || 'Failed to unassign patient')
    } finally {
      setLoading(false)
    }
  }

  const handleDeleteWorkerClick = (worker: UserDTO) => {
    setWorkerToDelete(worker)
    setShowDeleteConfirm(true)
  }

  const handleDeleteWorkerConfirm = async () => {
    if (!workerToDelete) return

    try {
      setLoading(true)
      setShowDeleteConfirm(false)
      
      await userService.deleteUser(workerToDelete.Id)
      showSuccess(`Healthcare worker ${workerToDelete.Name} has been removed`)
      
      // Clear selection if the deleted worker was selected
      if (selectedWorker?.Id === workerToDelete.Id) {
        setSelectedWorker(null)
        setAssignedPatients([])
      }
      
      // Refresh lists
      await loadWorkers()
      await loadUnassignedPatients()
      
      setWorkerToDelete(null)
    } catch (err: any) {
      showError(err?.message || 'Failed to delete healthcare worker')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="manage-page">
      <div className="admin-logout-header">
        <button
          className="logout-btn"
          onClick={() => setShowLogoutConfirm(true)}
        >
          <img src="/images/logout.png" alt="Logout" />
          <span>Log Out</span>
        </button>
      </div>
      <main className="manage-main manage-main--no-top-padding">
        <header className="manage-header">
          <h1 className="manage-title">Manage Healthcare Workers & Patients</h1>
          <div className="manage-actions">
            <button 
              className="btn btn--secondary" 
              onClick={() => navigate('/admin/register-worker')}
            >
              + Add New Worker
            </button>
          </div>
        </header>

        <div className="manage-content">
          {/* Left Side: Worker Selector and Patient Assignment */}
          <section className="manage-section manage-section--left">
            <div className="manage-card">
              <h2 className="manage-card-title">Select Healthcare Worker</h2>
              <div className="manage-worker-selector">
                <select 
                  value={selectedWorker?.Id || ''} 
                  onChange={handleWorkerChange}
                  className="manage-select"
                >
                  <option value="">-- Select a worker --</option>
                  {workers.map(worker => (
                    <option key={worker.Id} value={worker.Id}>
                      {worker.Name} ({worker.UserName})
                    </option>
                  ))}
                </select>
                {selectedWorker && (
                  <button
                    className="btn btn--danger btn--small"
                    onClick={() => handleDeleteWorkerClick(selectedWorker)}
                    disabled={loading}
                    title="Remove this healthcare worker"
                  >
                    Delete
                  </button>
                )}
              </div>
            </div>

            {selectedWorker && (
              <div className="manage-card">
                <h2 className="manage-card-title">Assign Patients</h2>
                <label>
                  <div className="manage-checkbox-list">
                    {unassignedPatients.length === 0 ? (
                      <p className="manage-empty">No unassigned patients available</p>
                    ) : (
                      unassignedPatients.map(patient => (
                        <label key={patient.Id} className="manage-checkbox-item">
                          <input
                            type="checkbox"
                            checked={selectedPatientIds.includes(patient.Id)}
                            onChange={() => handlePatientToggle(patient.Id)}
                          />
                          <span>{patient.Name} ({patient.UserName})</span>
                        </label>
                      ))
                    )}
                  </div>
                </label>
                <button 
                  className="btn btn--primary" 
                  onClick={handleAssignPatients}
                  disabled={loading || selectedPatientIds.length === 0}
                >
                  Assign Patients
                </button>
              </div>
            )}
          </section>

          {/* Right Side: Assigned Patients List */}
          <section className="manage-section manage-section--right">
            <div className="manage-card">
              <h2 className="manage-card-title">
                {selectedWorker 
                  ? `Patients Assigned to ${selectedWorker.Name}` 
                  : 'Select a worker to view assigned patients'}
              </h2>
              {selectedWorker && (
                <div className="manage-patient-list">
                  {assignedPatients.length === 0 ? (
                    <p className="manage-empty">No patients assigned to this worker</p>
                  ) : (
                    assignedPatients.map(patient => (
                      <div key={patient.Id} className="manage-patient-item">
                        <div className="manage-patient-info">
                          <div className="manage-patient-name">{patient.Name}</div>
                          <div className="manage-patient-email">{patient.UserName}</div>
                        </div>
                        <button 
                          className="btn btn--danger btn--small" 
                          onClick={() => handleUnassignPatient(patient.Id)}
                          disabled={loading}
                        >
                          Unassign
                        </button>
                      </div>
                    ))
                  )}
                </div>
              )}
            </div>
          </section>
        </div>
      </main>

      {showLogoutConfirm && (
        <div className="overlay" role="dialog" aria-modal="true" aria-labelledby="logout-confirm-title" aria-describedby="logout-confirm-desc">
          <div className="modal confirm-modal">
            <header className="modal__header">
              <h2 id="logout-confirm-title">Confirm Logout</h2>
              <button className="icon-btn" onClick={() => setShowLogoutConfirm(false)} aria-label="Close confirmation">
                <img src="/images/exit.png" alt="Close" />
              </button>
            </header>
            <div id="logout-confirm-desc" className="confirm-body">
              Are you sure you want to log out?
            </div>
            <div className="confirm-actions">
              <button type="button" className="btn" onClick={() => setShowLogoutConfirm(false)}>Cancel</button>
              <button 
                type="button" 
                className="btn btn--primary" 
                onClick={() => {
                  logout();
                  window.location.href = '/';
                }}
              >
                Confirm
              </button>
            </div>
          </div>
        </div>
      )}

      {showDeleteConfirm && workerToDelete && (
        <div className="overlay" role="dialog" aria-modal="true" aria-labelledby="delete-confirm-title" aria-describedby="delete-confirm-desc">
          <div className="modal confirm-modal">
            <header className="modal__header">
              <h2 id="delete-confirm-title">Confirm Delete</h2>
              <button className="icon-btn" onClick={() => {
                setShowDeleteConfirm(false)
                setWorkerToDelete(null)
              }} aria-label="Close confirmation">
                <img src="/images/exit.png" alt="Close" />
              </button>
            </header>
            <div id="delete-confirm-desc" className="confirm-body">
              Are you sure you want to delete this healthcare worker?
              <br /><br />
              <strong>{workerToDelete.Name}</strong> ({workerToDelete.UserName})
            </div>
            <div className="confirm-actions">
              <button type="button" className="btn" onClick={() => {
                setShowDeleteConfirm(false)
                setWorkerToDelete(null)
              }}>Cancel</button>
              <button 
                type="button" 
                className="btn btn--danger" 
                onClick={handleDeleteWorkerConfirm}
                disabled={loading}
              >
                Delete
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

export default UserManagePage
