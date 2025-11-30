// API service layer for User Management (Admin/Usermanager operations)

export interface UserDTO {
  Id: string;
  UserName: string;
  Name: string;
  Role: string;
  WorkerId?: string;
}

export interface RegisterWorkerDto {
  Name: string;
  Email: string;
  Password: string;
}

// Base API URL
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5080/api';

// Helper to get auth token from localStorage
function getAuthToken(): string | null {
  return localStorage.getItem('hc_token');
}

// Helper to create headers with auth token
function getHeaders(): HeadersInit {
  const token = getAuthToken();
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
  };
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  return headers;
}

// Helper to handle API responses
async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const errorText = await response.text();
    if (response.status === 406) {
      throw new Error('Request not acceptable: ' + errorText);
    } else if (response.status === 500) {
      throw new Error('Server error: ' + errorText);
    } else if (response.status === 401) {
      throw new Error('Unauthorized. Please log in again.');
    } else {
      throw new Error(`HTTP ${response.status}: ${errorText}`);
    }
  }
  const contentType = response.headers.get('content-type');
  if (contentType && contentType.includes('application/json')) {
    return await response.json();
  }
  return {} as T;
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

  // Get all healthcare workers
  async getAllWorkers(): Promise<UserDTO[]> {
    const response = await fetch(`${API_BASE_URL}/User/getAllWorkers`, {
      method: 'GET',
      headers: getHeaders(),
    });
    return handleResponse<UserDTO[]>(response);
  },

  // Get all patients
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

  // Assign multiple patients to a worker (by worker username)
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
