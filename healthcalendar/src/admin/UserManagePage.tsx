import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { sharedService, type UserDTO } from '../services/sharedService'
import { adminService } from '../services/adminService'
import { useToast } from '../shared/toastContext'
import '../styles/UserManagement.css'
import '../styles/EventCalendarPage.css'
import LogoutConfirmationModal from '../shared/LogoutConfirmationModal'

// Admin page for managing healthcare workers and patient assignments

const UserManagePage: React.FC = () => {
  const navigate = useNavigate()
  const { showSuccess, showError } = useToast()
  // State for managing workers and patients
  const [workers, setWorkers] = useState<UserDTO[]>([])
  const [selectedWorker, setSelectedWorker] = useState<UserDTO | null>(null)
  const [assignedPatients, setAssignedPatients] = useState<UserDTO[]>([])
  const [unassignedPatients, setUnassignedPatients] = useState<UserDTO[]>([])
  const [selectedPatientIds, setSelectedPatientIds] = useState<string[]>([])

  // UI state for loading and modals
  const [loading, setLoading] = useState(false)
  const [showLogoutConfirm, setShowLogoutConfirm] = useState(false)
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false)
  const [workerToDelete, setWorkerToDelete] = useState<UserDTO | null>(null)
  const [showDeletePatientConfirm, setShowDeletePatientConfirm] = useState(false)
  const [patientToDelete, setPatientToDelete] = useState<UserDTO | null>(null)

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

  // Fetch all healthcare workers from the backend
  const loadWorkers = async () => {
    try {
      const data = await adminService.getAllWorkers()
      setWorkers(data)
    } catch (err: any) {
      showError(err?.message || 'Failed to load healthcare workers')
    }
  }

  // Fetch all patients not currently assigned to any worker
  const loadUnassignedPatients = async () => {
    try {
      const data = await adminService.getUnassignedPatients()
      setUnassignedPatients(data)
    } catch (err: any) {
      showError(err?.message || 'Failed to load unassigned patients')
    }
  }

  // Fetch all patients assigned to a specific worker
  const loadAssignedPatients = async (workerId: string) => {
    try {
      const data = await sharedService.getUsersByWorkerId(workerId)
      setAssignedPatients(data)
    } catch (err: any) {
      showError(err?.message || 'Failed to load assigned patients')
    }
  }

  // Handle selection of a worker from the dropdown
  const handleWorkerChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const workerId = e.target.value
    const worker = workers.find(w => w.Id === workerId)
    setSelectedWorker(worker || null)
    setSelectedPatientIds([]) // Clear patient selections when changing worker
  }

  // Toggle patient checkbox selection for assignment
  const handlePatientToggle = (patientId: string) => {
    setSelectedPatientIds(prev => {
      if (prev.includes(patientId)) {
        return prev.filter(id => id !== patientId)
      } else {
        return [...prev, patientId]
      }
    })
  }

  // Assign selected patients to the currently selected worker
  const handleAssignPatients = async () => {
    if (!selectedWorker || selectedPatientIds.length === 0) {
      showError('Please select a worker and at least one patient')
      return
    }

    try {
      setLoading(true)
      await adminService.assignPatientsToWorker(selectedPatientIds, selectedWorker.Id)
      showSuccess(`Assigned ${selectedPatientIds.length} patient(s) to ${selectedWorker.Name}`)

      // Refresh lists to reflect the changes
      await loadAssignedPatients(selectedWorker.Id)
      await loadUnassignedPatients()
      setSelectedPatientIds([])
    } catch (err: any) {
      showError(err?.message || 'Failed to assign patients')
    } finally {
      setLoading(false)
    }
  }

  // Remove a patient's assignment from their current worker
  const handleUnassignPatient = async (patientId: string) => {
    try {
      setLoading(true)
      // step 1: retreive ids from all of patient's events
      const eventIds = await adminService.getEventIdsByUserId(patientId)
      // step 2: delete all patient's events and related schedules
      await sharedService.deleteSchedulesByEventIds(eventIds)
      await sharedService.deleteEventsByIds(eventIds)
      // step 3: unassign patient from their worker
      await adminService.unassignPatientFromWorker(patientId)
      showSuccess('Patient unassigned successfully')
      // Refresh lists to show updated assignments
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

  // Open confirmation modal for deleting a worker
  const handleDeleteWorkerClick = (worker: UserDTO) => {
    setWorkerToDelete(worker)
    setShowDeleteConfirm(true)
  }

  // Execute worker deletion after confirmation
  const handleDeleteWorkerConfirm = async () => {
    if (!workerToDelete) return

    try {
      setLoading(true)
      setShowDeleteConfirm(false)
      // step 1: retreive ids from all patients assigned to worker
      const patientIds = await sharedService.getIdsByWorkerId(workerToDelete.Id)
      // step 2: unassign all patients from worker
      await adminService.unassignPatientsFromWorker(patientIds)
      // step 3: retreive ids from all of patients events
      const eventIds = await adminService.getEventIdsByUserIds(patientIds)
      // step 4: delete all patients events and related schedules
      await sharedService.deleteSchedulesByEventIds(eventIds)
      await sharedService.deleteEventsByIds(eventIds)
      // step 5: delete all worker's availability
      await adminService.deleteAvailabilityByUserId(workerToDelete.Id)
      // step 6: delete the worker
      await adminService.deleteUser(workerToDelete.Id)
      showSuccess(`Worker ${workerToDelete.Name} has been removed`)

      // Clear selection if the deleted worker was currently selected
      if (selectedWorker?.Id === workerToDelete.Id) {
        setSelectedWorker(null)
        setAssignedPatients([])
      }

      // Refresh lists to show updated data
      await loadWorkers()
      await loadUnassignedPatients()

      setWorkerToDelete(null)
    } catch (err: any) {
      showError(err?.message || 'Failed to delete healthcare worker')
    } finally {
      setLoading(false)
    }
  }

  // Open confirmation modal for deleting a patient
  const handleDeletePatientClick = (patient: UserDTO) => {
    setPatientToDelete(patient)
    setShowDeletePatientConfirm(true)
  }

  // Execute patient deletion after confirmation
  const handleDeletePatientConfirm = async () => {
    if (!patientToDelete) return

    try {
      setLoading(true)
      setShowDeletePatientConfirm(false)

      // step 1: retreive ids from all of patient's events
      const eventIds = await adminService.getEventIdsByUserId(patientToDelete.Id)
      // step 2: delete all patient's events and related schedules
      await sharedService.deleteSchedulesByEventIds(eventIds)
      await sharedService.deleteEventsByIds(eventIds)
      // step 3: delete the patient
      await adminService.deleteUser(patientToDelete.Id)
      showSuccess(`Patient ${patientToDelete.Name} has been deleted`)

      // Clear patient selection if they were in the selected list
      setSelectedPatientIds(prev => prev.filter(id => id !== patientToDelete.Id))

      // Refresh lists to show updated data
      if (selectedWorker) {
        await loadAssignedPatients(selectedWorker.Id)
      }
      await loadUnassignedPatients()

      setPatientToDelete(null)
    } catch (err: any) {
      showError(err?.message || 'Failed to delete patient')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="manage-page">
      <main className="manage-main manage-main--no-top-padding">
        {/* Page header with title and action buttons */}
        <header className="manage-header">
          <h1 className="manage-title">Manage Healthcare Workers & Patients</h1>
          <div className="manage-actions manage-actions--vertical">
            {/* Logout button */}
            <button
              className="logout-btn"
              onClick={() => setShowLogoutConfirm(true)}
            >
              <img src="/images/logout.png" alt="Logout" />
              <span>Log Out</span>
            </button>
            <button
              className="btn btn--secondary"
              onClick={() => navigate('/admin/register-worker')}
            >
              + Add New Worker
            </button>
          </div>
        </header>

        {/* Two-column layout: worker selection & assignment on left, assigned patients on right */}
        <div className="manage-content">
          {/* Left Side: Worker Selector and Patient Assignment */}
          <section className="manage-section manage-section--left">
            {/* Worker selection dropdown with delete button */}
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
              </div>
            </div>

            {/* Patient assignment section (only visible when worker is selected) */}
            {selectedWorker && (
              <div className="manage-card">
                <h2 className="manage-card-title">Assign Patients</h2>
                {/* List of unassigned patients with checkboxes */}
                <div className="manage-checkbox-list">
                  {unassignedPatients.length === 0 ? (
                    <p className="manage-empty">No unassigned patients available</p>
                  ) : (
                    unassignedPatients.map(patient => (
                      <div key={patient.Id} className="manage-checkbox-item">
                        <label>
                          <input
                            type="checkbox"
                            checked={selectedPatientIds.includes(patient.Id)}
                            onChange={() => handlePatientToggle(patient.Id)}
                          />
                          <span>{patient.Name} ({patient.UserName})</span>
                        </label>
                        <button
                          className="btn btn--danger btn--small"
                          onClick={() => handleDeletePatientClick(patient)}
                          disabled={loading}
                          title="Delete this patient"
                        >
                          Delete
                        </button>
                      </div>
                    ))
                  )}
                </div>
                {/* Assign button (disabled when no patients selected) */}
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
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
                <h2 className="manage-card-title" style={{ margin: 0 }}>
                  {selectedWorker
                    ? `Patients Assigned to ${selectedWorker.Name}`
                    : 'Select a worker to view assigned patients'}
                </h2>
                {selectedWorker && (
                  <button
                    className="btn btn--danger btn--small"
                    onClick={() => handleDeleteWorkerClick(selectedWorker)}
                    disabled={loading}
                    title="Delete this healthcare worker"
                  >
                    Delete Worker
                  </button>
                )}
              </div>
              {/* Display assigned patients with unassign buttons */}
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
                        <div className="manage-button-actions">
                          <button
                            className="btn btn--grey btn--small"
                            onClick={() => handleUnassignPatient(patient.Id)}
                            disabled={loading}
                          >
                            Unassign
                          </button>
                          <button
                            className="btn btn--danger btn--small"
                            onClick={() => handleDeletePatientClick(patient)}
                            disabled={loading}
                            title="Delete this patient"
                          >
                            Delete
                          </button>
                        </div>
                      </div>
                    ))
                  )}
                </div>
              )}
            </div>
          </section>
        </div>
      </main>

      {/* Logout confirmation modal */}
      <LogoutConfirmationModal
        isOpen={showLogoutConfirm}
        onClose={() => setShowLogoutConfirm(false)}
      />

      {/* Worker deletion confirmation modal */}
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

      {/* Patient deletion confirmation modal */}
      {showDeletePatientConfirm && patientToDelete && (
        <div className="overlay" role="dialog" aria-modal="true" aria-labelledby="delete-patient-title" aria-describedby="delete-patient-desc">
          <div className="modal confirm-modal">
            <header className="modal__header">
              <h2 id="delete-patient-title">Confirm Delete</h2>
              <button className="icon-btn" onClick={() => {
                setShowDeletePatientConfirm(false)
                setPatientToDelete(null)
              }} aria-label="Close confirmation">
                <img src="/images/exit.png" alt="Close" />
              </button>
            </header>
            <div id="delete-patient-desc" className="confirm-body">
              Are you sure you want to delete this patient?
              <br /><br />
              <strong>{patientToDelete.Name}</strong> ({patientToDelete.UserName})
            </div>
            <div className="confirm-actions">
              <button type="button" className="btn" onClick={() => {
                setShowDeletePatientConfirm(false)
                setPatientToDelete(null)
              }}>Cancel</button>
              <button
                type="button"
                className="btn btn--danger"
                onClick={handleDeletePatientConfirm}
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
