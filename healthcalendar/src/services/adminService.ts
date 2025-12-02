// API service layer for User Management (Admin operations)

// imports DTOs shared with other services
import type { UserDTO } from './sharedService.ts';
// Imports constants and functions shared with other services
import { API_BASE_URL, getHeaders, handleResponse, normalizeError } from './sharedService.ts'

// Public API surface for User Management
export const adminService = {
  // Get all healthcare workers, used in Admin dashboard for worker management
  async getAllWorkers(): Promise<UserDTO[]> {
    try {
      const response = await fetch(`${API_BASE_URL}/User/getAllWorkers`, {
        method: 'GET',
        headers: getHeaders(),
      });
      return handleResponse<UserDTO[]>(response);
    } catch (err) {
      throw normalizeError(err);
    }
  },

  // Get all patients, used in Admin dashboard for patient overview
  async getAllPatients(): Promise<UserDTO[]> {
    try {
      const response = await fetch(`${API_BASE_URL}/User/getAllPatients`, {
        method: 'GET',
        headers: getHeaders(),
      });
      return handleResponse<UserDTO[]>(response);
    } catch (err) {
      throw normalizeError(err);
    }
  },

  // Get unassigned patients (patients without a worker)
  async getUnassignedPatients(): Promise<UserDTO[]> {
    try {
      const response = await fetch(`${API_BASE_URL}/User/getUnassignedPatients`, {
        method: 'GET',
        headers: getHeaders(),
      });
      return handleResponse<UserDTO[]>(response);
    } catch (err) {
      throw normalizeError(err);
    }
  },

  // Gets all EventIds from a patient's events
  async getEventIdsByUserId(userId: string): Promise<number[]> {
    try {
      const response = await fetch(`${API_BASE_URL}/Event/getEventIdsByUserId?userId=${encodeURIComponent(userId)}`, {
        method: 'GET',
        headers: getHeaders(),
      });
      return handleResponse<number[]>(response);
    } catch (err) {
      throw normalizeError(err);
    }
  },

  // Gets all EventIds from several patients events
  async getEventIdsByUserIds(userIds: string[]): Promise<number[]> {
    const queryParams = userIds.map(id => `userIds=${encodeURIComponent(id)}`).join('&');
    try {
      const response = await fetch(`${API_BASE_URL}/Event/getEventIdsByUserIds?${queryParams}`, {
        method: 'GET',
        headers: getHeaders(),
      });
      return handleResponse<number[]>(response);
    } catch (err) {
      throw normalizeError(err);
    }
  },

  // Assign multiple patients to a worker
  async assignPatientsToWorker(patientIds: string[], workerId: string): Promise<any> {
    try {
      const queryParams = patientIds.map(id => `userIds=${encodeURIComponent(id)}`).join('&');
      const response = await fetch(
        `${API_BASE_URL}/User/assignPatientsToWorker?${queryParams}&workerId=${encodeURIComponent(workerId)}`,
        {
          method: 'PUT',
          headers: getHeaders(),
        }
      );
      return handleResponse(response);
    } catch (err) {
      throw normalizeError(err);
    }
  },

  // Unassign a patient from their worker
  async unassignPatientFromWorker(patientId: string): Promise<any> {
    try {
      const response = await fetch(`${API_BASE_URL}/User/unassignPatientFromWorker/${encodeURIComponent(patientId)}`, {
        method: 'PUT',
        headers: getHeaders(),
      });
      return handleResponse(response);
    } catch (err) {
      throw normalizeError(err);
    }
  },

  // Unassign several patients from their worker
  async unassignPatientsFromWorker(patientIds: string[]): Promise<any> {
    try {
      const queryParams = patientIds.map(id => `userIds=${encodeURIComponent(id)}`).join('&');
      const response = await fetch(`${API_BASE_URL}/User/unassignPatientsFromWorker?${queryParams}`, {
        method: 'PUT',
        headers: getHeaders(),
      });
      return handleResponse(response);
    } catch (err) {
      throw normalizeError(err);
    }
  },

  // Delete a user (worker or patient)
  async deleteUser(userId: string): Promise<any> {
    try {
      const response = await fetch(`${API_BASE_URL}/User/deleteUser/${encodeURIComponent(userId)}`, {
        method: 'DELETE',
        headers: getHeaders(),
      });
      return handleResponse(response);
    } catch (err) {
      throw normalizeError(err);
    }
  },

  // Delete all of a worker's availability
  async deleteAvailabilityByUserId(userId: string): Promise<any> {
    try {
      const response = await fetch(`${API_BASE_URL}/Availability/deleteAvailabilityByUserId?userId=${encodeURIComponent(userId)}`, {
        method: 'DELETE',
        headers: getHeaders(),
      });
      return handleResponse(response);
    } catch (err) {
      throw normalizeError(err);
    }
  },
};