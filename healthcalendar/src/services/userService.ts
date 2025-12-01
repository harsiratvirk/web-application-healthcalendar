// API service layer for User Management (Admin operations)

// Imports functions shared with other services
import { API_BASE_URL, getHeaders, handleResponse } from './sharedService.ts'

// Represents a user (Patient, Worker, or Admin) in the system
export interface UserDTO {
  Id: string;             
  UserName: string;       
  Name: string;            
  Role: string;            // User role: "Patient", "Worker", or "Admin"
  WorkerId?: string;       // ID of assigned worker (only for patients)
}

// Data required to register a new healthcare worker
export interface RegisterWorkerDto {
  Name: string;            
  Email: string;           
  Password: string;       
}

// Public API surface for User Management
export const userService = {
  // Register a new healthcare worker
  async registerWorker(userData: RegisterWorkerDto): Promise<any> {
    const response = await fetch(`${API_BASE_URL}/Auth/registerWorker`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(userData),
    });
    return handleResponse(response);
  },

  // Get all healthcare workers, used in Admin dashboard for worker management
  async getAllWorkers(): Promise<UserDTO[]> {
    const response = await fetch(`${API_BASE_URL}/User/getAllWorkers`, {
      method: 'GET',
      headers: getHeaders(),
    });
    return handleResponse<UserDTO[]>(response);
  },

  // Get all patients, used in Admin dashboard for patient overview
  async getAllPatients(): Promise<UserDTO[]> {
    const response = await fetch(`${API_BASE_URL}/User/getAllPatients`, {
      method: 'GET',
      headers: getHeaders(),
    });
    return handleResponse<UserDTO[]>(response);
  },

  // Get unassigned patients (patients without a worker)
  async getUnassignedPatients(): Promise<UserDTO[]> {
    const response = await fetch(`${API_BASE_URL}/User/getUnassignedPatients`, {
      method: 'GET',
      headers: getHeaders(),
    });
    return handleResponse<UserDTO[]>(response);
  },

  // Get patients assigned to a specific worker
  async getUsersByWorkerId(workerId: string): Promise<UserDTO[]> {
    const response = await fetch(`${API_BASE_URL}/User/getUsersByWorkerId?workerId=${encodeURIComponent(workerId)}`, {
      method: 'GET',
      headers: getHeaders(),
    });
    return handleResponse<UserDTO[]>(response);
  },

  // Assign multiple patients to a worker
  async assignPatientsToWorker(patientIds: string[], workerUsername: string): Promise<any> {
    const queryParams = patientIds.map(id => `userIds=${encodeURIComponent(id)}`).join('&');
    const response = await fetch(
      `${API_BASE_URL}/User/assignPatientsToWorker?${queryParams}&username=${encodeURIComponent(workerUsername)}`,
      {
        method: 'PUT',
        headers: getHeaders(),
      }
    );
    return handleResponse(response);
  },

  // Unassign a patient from their worker
  async unassignPatientFromWorker(patientId: string): Promise<any> {
    const response = await fetch(`${API_BASE_URL}/User/unassignPatientFromWorker/${encodeURIComponent(patientId)}`, {
      method: 'PUT',
      headers: getHeaders(),
    });
    return handleResponse(response);
  },

  // Delete a user (worker or patient)
  async deleteUser(userId: string): Promise<any> {
    const response = await fetch(`${API_BASE_URL}/User/deleteUser/${encodeURIComponent(userId)}`, {
      method: 'DELETE',
      headers: getHeaders(),
    });
    return handleResponse(response);
  },
};